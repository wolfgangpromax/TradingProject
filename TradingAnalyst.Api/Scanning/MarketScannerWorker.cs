using Microsoft.Extensions.Options;
using TradingAnalyst.Api.Notifications;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Api.Scanning;

public sealed class MarketScannerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScannerOptions _options;
    private readonly ScannerStateStore _store;
    private readonly IDiscordNotifier _discord;
    private readonly ILogger<MarketScannerWorker> _logger;
    private readonly Dictionary<string, string> _lastNotifiedState = new(StringComparer.OrdinalIgnoreCase);

    public MarketScannerWorker(IServiceScopeFactory scopeFactory, IOptions<ScannerOptions> options, ScannerStateStore store, IDiscordNotifier discord, ILogger<MarketScannerWorker> logger)
    {
        _scopeFactory = scopeFactory; _options = options.Value; _store = store; _discord = discord; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        await ScanAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(30, _options.IntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await ScanAsync(stoppingToken);
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var symbolsRepo = scope.ServiceProvider.GetRequiredService<IMarketSymbolRepository>();
        var candleRepo = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
        var marketData = scope.ServiceProvider.GetRequiredService<IMarketDataService>();
        var structureReplay = scope.ServiceProvider.GetRequiredService<IMarketStructureReplayService>();
        var fvgReplay = scope.ServiceProvider.GetRequiredService<IFairValueGapQualificationReplayService>();
        var active = await symbolsRepo.GetActiveAsync(cancellationToken);
        var configured = _options.Symbols.Select(x => x.Trim().ToUpperInvariant()).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var symbols = active.Where(x => x.Exchange.Equals("BINANCE", StringComparison.OrdinalIgnoreCase) && x.QuoteAsset.Equals("USDT", StringComparison.OrdinalIgnoreCase))
            .Where(x => configured.Count == 0 || configured.Contains(x.Symbol)).OrderBy(x => x.Symbol).ToList();

        foreach (var marketSymbol in symbols)
        {
            try
            {
                foreach (var tf in new[] { Timeframe.OneMinute, Timeframe.FiveMinutes, Timeframe.FifteenMinutes })
                    await marketData.SynchronizeIncrementalAsync("BINANCE", marketSymbol.Symbol, tf, cancellationToken);

                var one = await candleRepo.GetRecentAsync(marketSymbol.MarketSymbolId, Timeframe.OneMinute, _options.CandleLimit, cancellationToken);
                var five = await candleRepo.GetRecentAsync(marketSymbol.MarketSymbolId, Timeframe.FiveMinutes, _options.CandleLimit, cancellationToken);
                var fifteen = await candleRepo.GetRecentAsync(marketSymbol.MarketSymbolId, Timeframe.FifteenMinutes, _options.CandleLimit, cancellationToken);
                if (one.Count < 20 || five.Count < 20 || fifteen.Count < 20) continue;

                var context = structureReplay.Replay(fifteen).OrderBy(x => x.Time).LastOrDefault();
                var entry = structureReplay.Replay(one).OrderBy(x => x.Time).LastOrDefault();
                var setup = fvgReplay.Replay(five, 14).OrderBy(x => x.Time).LastOrDefault();
                if (context is null || entry is null || setup is null) continue;

                var candidate = setup.Candidates.Where(x => x.IsCandidate && !x.State.IsMitigated)
                    .Where(x => x.State.Gap.Type == FairValueGapType.Bullish && context.Trend == MarketTrend.Bullish || x.State.Gap.Type == FairValueGapType.Bearish && context.Trend == MarketTrend.Bearish)
                    .OrderByDescending(x => x.State.Gap.ConfirmedAt).FirstOrDefault();
                var interaction = candidate is null ? null : setup.Interactions.FirstOrDefault(x => x.Candidate.State.Gap.Key == candidate.State.Gap.Key);
                var expected = candidate?.State.Gap.Type == FairValueGapType.Bullish ? BreakDirection.Bullish : BreakDirection.Bearish;
                var entryBreak = interaction?.FirstInteractionTime is null ? null : entry.StructureBreaks.Where(x => x.BreakTime > interaction.FirstInteractionTime.Value && x.Direction == expected).OrderBy(x => x.BreakTime).FirstOrDefault();
                var confirmed = interaction?.HasInteracted == true && entryBreak is not null;
                var status = context.Trend is MarketTrend.Unknown or MarketTrend.Transition ? "NO_SETUP" : candidate is null ? "SCANNING" : interaction?.HasInteracted != true ? "WATCHING_SETUP" : !confirmed ? "WAITING_FOR_ENTRY" : "ENTRY_CONFIRMED";
                var direction = candidate?.State.Gap.Type == FairValueGapType.Bullish ? "LONG" : candidate?.State.Gap.Type == FairValueGapType.Bearish ? "SHORT" : null;
                var missing = context.Trend is MarketTrend.Unknown or MarketTrend.Transition ? "Clear 15M structure" : candidate is null ? "Aligned qualified 5M FVG" : interaction?.HasInteracted != true ? "5M FVG retracement" : !confirmed ? "1M BOS/CHoCH confirmation" : "All defined conditions confirmed";
                var price = one.OrderBy(x => x.CloseTime).Last().Close;
                var tradeLevels = confirmed && candidate is not null && entryBreak is not null ? CalculateTradeLevels(five, candidate.State.Gap.ConfirmedAt, entryBreak.BreakTime, entryBreak.BreakClosePrice, direction) : null;
                _store.Upsert(new ScannerRow(marketSymbol.Symbol, status, direction, context.Trend.ToString(), price, missing, DateTime.UtcNow));
                await NotifyOnTransitionAsync(marketSymbol.Symbol, status, direction, context.Trend.ToString(), price, missing, candidate?.State.Gap.LowerPrice, candidate?.State.Gap.UpperPrice, tradeLevels, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _logger.LogError(ex, "Scanner failed for {Symbol}.", marketSymbol.Symbol); }
        }
    }

    private sealed record TradeLevels(decimal Entry, decimal Invalidation, decimal Target1R, decimal Target2R);

    private static TradeLevels? CalculateTradeLevels(IReadOnlyCollection<TradingAnalyst.Business.Domain.Entities.Candle> candles, DateTime setupStart, DateTime entryTime, decimal entry, string? direction)
    {
        if (direction is null) return null;
        var setup = candles.Where(x => x.IsClosed && x.CloseTime >= setupStart && x.CloseTime <= entryTime).ToList();
        if (setup.Count == 0) return null;
        var invalidation = direction == "LONG" ? setup.Min(x => x.Low) : setup.Max(x => x.High);
        var risk = Math.Abs(entry - invalidation);
        if (risk <= 0m || direction == "LONG" && invalidation >= entry || direction == "SHORT" && invalidation <= entry) return null;
        return new TradeLevels(entry, invalidation, direction == "LONG" ? entry + risk : entry - risk, direction == "LONG" ? entry + risk * 2m : entry - risk * 2m);
    }

    private async Task NotifyOnTransitionAsync(string symbol, string status, string? direction, string trend, decimal price, string missing, decimal? fvgLow, decimal? fvgHigh, TradeLevels? tradeLevels, CancellationToken cancellationToken)
    {
        var noteworthy = status is "WATCHING_SETUP" or "WAITING_FOR_ENTRY" or "ENTRY_CONFIRMED";
        if (!noteworthy) { _lastNotifiedState[symbol] = status; return; }
        if (_lastNotifiedState.TryGetValue(symbol, out var previous) && previous == status) return;
        _lastNotifiedState[symbol] = status;
        var icon = status == "ENTRY_CONFIRMED" ? "🟢" : status == "WAITING_FOR_ENTRY" ? "🟠" : "🟡";
        var fvg = fvgLow.HasValue && fvgHigh.HasValue ? $"\n5M FVG: {fvgLow} – {fvgHigh}" : "";
        var levels = tradeLevels is null ? "" : $"\nEntry reference: {tradeLevels.Entry}\nTechnical invalidation: {tradeLevels.Invalidation}\n1R reference: {tradeLevels.Target1R}\n2R reference: {tradeLevels.Target2R}";
        await _discord.SendAsync($"{icon} **{symbol} — {direction ?? "WATCH"}**\nState: **{status.Replace('_', ' ')}**\n15M Context: {trend}\nCurrent Price: {price}{fvg}{levels}\nNext: {missing}\n\nReview the chart in TradingAnalyst before making a decision.", cancellationToken);
    }
}
