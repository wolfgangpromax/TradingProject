using Microsoft.AspNetCore.Mvc;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Api.Controllers;

[ApiController]
[Route("api/live-analysis")]
public sealed class LiveAnalysisController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IMarketSymbolRepository _marketSymbolRepository;
    private readonly ICandleRepository _candleRepository;
    private readonly IMarketStructureReplayService _marketStructureReplayService;
    private readonly IFairValueGapQualificationReplayService _fvgReplayService;

    private sealed record LiquiditySweepResult(bool Swept, string Side, decimal? Level, DateTime? Time);

    public LiveAnalysisController(IMarketDataService marketDataService, IMarketSymbolRepository marketSymbolRepository,
        ICandleRepository candleRepository, IMarketStructureReplayService marketStructureReplayService,
        IFairValueGapQualificationReplayService fvgReplayService)
    {
        _marketDataService = marketDataService;
        _marketSymbolRepository = marketSymbolRepository;
        _candleRepository = candleRepository;
        _marketStructureReplayService = marketStructureReplayService;
        _fvgReplayService = fvgReplayService;
    }

    [HttpGet("symbols")]
    public async Task<IActionResult> Symbols([FromQuery] string search = "", [FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var symbols = await _marketSymbolRepository.GetActiveAsync(cancellationToken);
        var rows = symbols.Where(x => x.Exchange.Equals("BINANCE", StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(search) || x.Symbol.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Symbol).Take(Math.Clamp(limit, 1, 500))
            .Select(x => new { x.Symbol, x.BaseAsset, x.QuoteAsset }).ToList();
        return Ok(rows);
    }

    [HttpPost("{symbol}/refresh")]
    public async Task<IActionResult> Refresh(string symbol, CancellationToken cancellationToken = default)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync("BINANCE", symbol, cancellationToken);
        if (marketSymbol is null) return NotFound($"Symbol '{symbol}' was not found. Run POST /api/market-data/symbols/sync once first.");
        var timeframes = new[] { Timeframe.OneMinute, Timeframe.FiveMinutes, Timeframe.FifteenMinutes, Timeframe.OneHour, Timeframe.FourHours };
        var synchronized = new List<object>();
        foreach (var timeframe in timeframes)
        {
            var count = await _marketDataService.SynchronizeIncrementalAsync("BINANCE", symbol, timeframe, cancellationToken);
            synchronized.Add(new { Timeframe = timeframe.ToString(), Count = count });
        }
        return Ok(new { Symbol = symbol, Synchronized = synchronized });
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> Analyze(string symbol, [FromQuery] Timeframe chartTimeframe = Timeframe.FiveMinutes,
        [FromQuery] int limit = 500, [FromQuery] int atrPeriod = 14, CancellationToken cancellationToken = default)
    {
        if (limit is < 100 or > 1000) return BadRequest("Limit must be between 100 and 1000.");
        symbol = symbol.Trim().ToUpperInvariant();
        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync("BINANCE", symbol, cancellationToken);
        if (marketSymbol is null) return NotFound($"Symbol '{symbol}' was not found for BINANCE.");

        var oneMinute = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, Timeframe.OneMinute, limit, cancellationToken);
        var fiveMinute = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, Timeframe.FiveMinutes, limit, cancellationToken);
        var fifteenMinute = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, Timeframe.FifteenMinutes, limit, cancellationToken);
        var chartCandles = chartTimeframe switch
        {
            Timeframe.OneMinute => oneMinute,
            Timeframe.FiveMinutes => fiveMinute,
            Timeframe.FifteenMinutes => fifteenMinute,
            _ => await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, chartTimeframe, limit, cancellationToken)
        };
        if (oneMinute.Count < 20 || fiveMinute.Count < 20 || fifteenMinute.Count < 20)
            return BadRequest("Not enough 1M/5M/15M candles. Refresh this symbol first.");

        var context = _marketStructureReplayService.Replay(fifteenMinute).OrderBy(x => x.Time).LastOrDefault();
        var entryStructure = _marketStructureReplayService.Replay(oneMinute).OrderBy(x => x.Time).LastOrDefault();
        var chartStructure = _marketStructureReplayService.Replay(chartCandles).OrderBy(x => x.Time).LastOrDefault();
        var setupReplay = _fvgReplayService.Replay(fiveMinute, atrPeriod).OrderBy(x => x.Time).LastOrDefault();
        if (context is null || entryStructure is null || chartStructure is null || setupReplay is null) return BadRequest("Analysis replay did not produce a snapshot.");

        var contextTrend = context.Trend;
        var directionalCandidates = setupReplay.Candidates.Where(x => x.IsCandidate && !x.State.IsMitigated)
            .Where(x => IsDirectionAligned(x.State.Gap.Type, contextTrend)).OrderByDescending(x => x.State.Gap.ConfirmedAt).ToList();
        var candidate = directionalCandidates.FirstOrDefault();
        var interaction = candidate is null ? null : setupReplay.Interactions.FirstOrDefault(x => x.Candidate.State.Gap.Key == candidate.State.Gap.Key);
        var existingConfirmation = candidate is null ? null : setupReplay.EntryConfirmations.FirstOrDefault(x => x.Interaction.Candidate.State.Gap.Key == candidate.State.Gap.Key);

        var expectedDirection = candidate?.State.Gap.Type == FairValueGapType.Bullish ? BreakDirection.Bullish : BreakDirection.Bearish;
        var entryBreak = interaction?.FirstInteractionTime is null ? null : entryStructure.StructureBreaks
            .Where(x => x.BreakTime > interaction.FirstInteractionTime.Value && x.Direction == expectedDirection)
            .OrderBy(x => x.BreakTime).FirstOrDefault();
        var entryConfirmed = interaction?.HasInteracted == true && entryBreak is not null;

        var liquidity = DetectLiquiditySweep(fiveMinute, contextTrend);
        var displacement = DetectDisplacement(fiveMinute, setupReplay.AverageTrueRange, candidate?.State.Gap.Type);
        var status = GetStatus(contextTrend, candidate, interaction, entryConfirmed);
        var currentPrice = oneMinute.OrderBy(x => x.CloseTime).Last().Close;
        var direction = candidate?.State.Gap.Type == FairValueGapType.Bullish ? "LONG" : candidate?.State.Gap.Type == FairValueGapType.Bearish ? "SHORT" : null;
        var tradeReference = BuildTradeReference(oneMinute, fiveMinute, fifteenMinute, candidate, entryBreak, liquidity, direction);
        var missing = new List<string>();
        if (contextTrend is MarketTrend.Unknown or MarketTrend.Transition) missing.Add("Clear 15M directional structure");
        if (candidate is null) missing.Add("Qualified 5M FVG aligned with 15M structure");
        if (candidate is not null && interaction?.HasInteracted != true) missing.Add("Price retracement into the 5M FVG");
        if (interaction?.HasInteracted == true && !entryConfirmed) missing.Add("1M BOS/CHoCH confirmation after FVG interaction");

        var explanation = BuildExplanation(symbol, contextTrend, candidate, interaction, entryBreak, displacement, status);
        var latestBreaks = chartStructure.StructureBreaks.OrderByDescending(x => x.BreakTime).Take(20).OrderBy(x => x.BreakTime).Select(x => new
        {
            Type = x.Type == StructureBreakType.ChangeOfCharacter ? "CHoCH" : "BOS", Direction = x.Direction.ToString(), x.BrokenPrice, x.BreakTime, x.BreakClosePrice
        }).ToList();
        var swings = chartStructure.Swings.OrderByDescending(x => x.ConfirmedAt).Take(30).OrderBy(x => x.ConfirmedAt)
            .Select(x => new { Type = x.Type.ToString(), x.Price, x.OpenTime, x.ConfirmedAt }).ToList();

        return Ok(new
        {
            Exchange = "BINANCE", Symbol = symbol, Strategy = new { Name = "ScalpingV1", ContextTimeframe = "15M", SetupTimeframe = "5M", EntryTimeframe = "1M" },
            ChartTimeframe = chartTimeframe.ToString(), CurrentPrice = currentPrice, UpdatedAt = DateTime.UtcNow,
            Status = status, Direction = direction, ContextTrend = contextTrend.ToString(), SetupTrend = setupReplay.MarketTrend.ToString(), EntryTrend = entryStructure.Trend.ToString(),
            Checklist = new
            {
                ContextAligned = contextTrend is MarketTrend.Bullish or MarketTrend.Bearish,
                LiquiditySweep = liquidity.Swept,
                Displacement = displacement,
                QualifiedFvg = candidate is not null,
                FvgInteraction = interaction?.HasInteracted == true,
                EntryStructureConfirmation = entryConfirmed
            },
            Liquidity = liquidity,
            Setup = candidate is null ? null : new
            {
                Type = candidate.State.Gap.Type.ToString(), candidate.State.Gap.LowerPrice, candidate.State.Gap.UpperPrice, candidate.State.Gap.ConfirmedAt,
                candidate.State.Gap.GapSize, candidate.State.FillPercentage, InteractionTime = interaction?.FirstInteractionTime,
                InteractionPrice = interaction?.FirstInteractionPrice, ExistingFiveMinuteConfirmation = existingConfirmation?.Status.ToString(),
                EntryConfirmationTime = entryBreak?.BreakTime, EntryConfirmationType = entryBreak?.Type.ToString(), EntryConfirmationPrice = entryBreak?.BreakClosePrice
            },
            TradeReference = tradeReference,
            MissingConditions = missing, Explanation = explanation,
            Chart = new
            {
                Candles = chartCandles.OrderBy(x => x.OpenTime).TakeLast(300).Select(x => new { x.OpenTime, x.CloseTime, x.Open, x.High, x.Low, x.Close, x.Volume, x.IsClosed }).ToList(),
                Swings = swings, StructureBreaks = latestBreaks,
                ActiveFvg = candidate is null ? null : new { candidate.State.Gap.LowerPrice, candidate.State.Gap.UpperPrice, Type = candidate.State.Gap.Type.ToString(), candidate.State.Gap.ConfirmedAt },
                EntryMarker = entryBreak is null ? null : new { Time = entryBreak.BreakTime, Price = entryBreak.BreakClosePrice, Type = entryBreak.Type == StructureBreakType.ChangeOfCharacter ? "CHoCH" : "BOS" }
            }
        });
    }

    private static bool IsDirectionAligned(FairValueGapType type, MarketTrend trend) =>
        type == FairValueGapType.Bullish && trend == MarketTrend.Bullish || type == FairValueGapType.Bearish && trend == MarketTrend.Bearish;

    private static LiquiditySweepResult DetectLiquiditySweep(IReadOnlyCollection<Candle> candles, MarketTrend trend)
    {
        var ordered = candles.Where(x => x.IsClosed).OrderBy(x => x.CloseTime).TakeLast(40).ToList();

        if (ordered.Count < 10)
            return new LiquiditySweepResult(false, "NONE", null, null);

        var recent = ordered.TakeLast(5).ToList();
        var reference = ordered.Take(ordered.Count - 5).TakeLast(20).ToList();

        if (trend == MarketTrend.Bullish)
        {
            var level = reference.Min(x => x.Low);
            var sweep = recent.FirstOrDefault(x => x.Low < level && x.Close > level);

            return new LiquiditySweepResult(sweep is not null, "SELL_SIDE", level, sweep?.CloseTime);
        }

        if (trend == MarketTrend.Bearish)
        {
            var level = reference.Max(x => x.High);
            var sweep = recent.FirstOrDefault(x => x.High > level && x.Close < level);

            return new LiquiditySweepResult(sweep is not null, "BUY_SIDE", level, sweep?.CloseTime);
        }

        return new LiquiditySweepResult(false, "NONE", null, null);
    }

    private static object? BuildTradeReference(IReadOnlyCollection<Candle> oneMinute, IReadOnlyCollection<Candle> fiveMinute, IReadOnlyCollection<Candle> fifteenMinute,
        FairValueGapCandidate? candidate, StructureBreak? entryBreak, LiquiditySweepResult liquidity, string? direction)
    {
        if (candidate is null || entryBreak is null || direction is null) return null;
        var entry = entryBreak.BreakClosePrice;
        var setupStart = candidate.State.Gap.ConfirmedAt;
        var setupCandles = fiveMinute.Where(x => x.IsClosed && x.CloseTime >= setupStart && x.CloseTime <= entryBreak.BreakTime).OrderBy(x => x.CloseTime).ToList();
        if (setupCandles.Count == 0) return null;

        decimal invalidation;
        if (direction == "LONG")
        {
            invalidation = setupCandles.Min(x => x.Low);
            if (liquidity.Swept && liquidity.Level.HasValue) invalidation = Math.Min(invalidation, liquidity.Level.Value);
            if (invalidation >= entry) return null;
        }
        else
        {
            invalidation = setupCandles.Max(x => x.High);
            if (liquidity.Swept && liquidity.Level.HasValue) invalidation = Math.Max(invalidation, liquidity.Level.Value);
            if (invalidation <= entry) return null;
        }

        var risk = Math.Abs(entry - invalidation);
        if (risk <= 0m) return null;
        var target1R = direction == "LONG" ? entry + risk : entry - risk;
        var target2R = direction == "LONG" ? entry + risk * 2m : entry - risk * 2m;
        var opposingLiquidity = FindOpposingLiquidity(fifteenMinute, entry, direction);
        var outcome = EvaluateOutcome(oneMinute, entryBreak.BreakTime, direction, invalidation, target1R, target2R);
        return new { Entry = entry, TechnicalInvalidation = invalidation, RiskDistance = risk, Target1R = target1R, Target2R = target2R, OpposingLiquidity = opposingLiquidity, Outcome = outcome };
    }

    private static decimal? FindOpposingLiquidity(IReadOnlyCollection<Candle> candles, decimal entry, string direction)
    {
        var recent = candles.Where(x => x.IsClosed).OrderByDescending(x => x.CloseTime).Take(80).ToList();
        if (direction == "LONG") return recent.Select(x => x.High).Where(x => x > entry).OrderBy(x => x).Cast<decimal?>().FirstOrDefault();
        return recent.Select(x => x.Low).Where(x => x < entry).OrderByDescending(x => x).Cast<decimal?>().FirstOrDefault();
    }

    private static string EvaluateOutcome(IReadOnlyCollection<Candle> candles, DateTime entryTime, string direction, decimal invalidation, decimal target1R, decimal target2R)
    {
        var after = candles.Where(x => x.IsClosed && x.CloseTime > entryTime).OrderBy(x => x.CloseTime);
        var hit1R = false;
        foreach (var candle in after)
        {
            var stopHit = direction == "LONG" ? candle.Low <= invalidation : candle.High >= invalidation;
            var target2Hit = direction == "LONG" ? candle.High >= target2R : candle.Low <= target2R;
            var target1Hit = direction == "LONG" ? candle.High >= target1R : candle.Low <= target1R;
            if (stopHit) return hit1R ? "TP1_THEN_SL" : "SL_HIT";
            if (target2Hit) return "TP2_HIT";
            if (target1Hit) hit1R = true;
        }
        return hit1R ? "TP1_HIT" : "ACTIVE";
    }

    private static bool DetectDisplacement(IReadOnlyCollection<Candle> candles, decimal? atr, FairValueGapType? direction)
    {
        if (atr is null or <= 0m || direction is null) return false;
        return candles.Where(x => x.IsClosed).OrderBy(x => x.CloseTime).TakeLast(5).Any(x =>
            Math.Abs(x.Close - x.Open) >= atr.Value * 0.75m &&
            (direction == FairValueGapType.Bullish ? x.Close > x.Open : x.Close < x.Open));
    }

    private static string GetStatus(MarketTrend trend, FairValueGapCandidate? candidate, FairValueGapInteractionState? interaction, bool entryConfirmed)
    {
        if (trend is MarketTrend.Unknown or MarketTrend.Transition) return "NO_SETUP";
        if (candidate is null) return "SCANNING";
        if (interaction?.HasInteracted != true) return "WATCHING_SETUP";
        if (!entryConfirmed) return "WAITING_FOR_ENTRY";
        return "ENTRY_CONFIRMED";
    }

    private static string BuildExplanation(string symbol, MarketTrend trend, FairValueGapCandidate? candidate, FairValueGapInteractionState? interaction,
        StructureBreak? entryBreak, bool displacement, string status)
    {
        var parts = new List<string> { $"{symbol} 15M structure is {trend.ToString().ToLowerInvariant()}." };
        if (candidate is null) parts.Add("No active qualified 5M FVG is currently aligned with that context, so the engine is still scanning.");
        else
        {
            parts.Add($"A {candidate.State.Gap.Type.ToString().ToLowerInvariant()} 5M FVG is active at {candidate.State.Gap.LowerPrice}–{candidate.State.Gap.UpperPrice}.");
            if (interaction?.HasInteracted == true) parts.Add("Price has retraced into the FVG."); else parts.Add("Price has not retraced into the FVG yet; do not chase the displacement.");
            if (entryBreak is not null) parts.Add($"The 1M chart confirmed the entry condition with a {entryBreak.Type} {entryBreak.Direction} structure break.");
            else if (interaction?.HasInteracted == true) parts.Add("The engine is waiting for a directional 1M BOS/CHoCH after the interaction.");
        }
        if (displacement) parts.Add("Recent 5M price action also meets the current displacement threshold.");
        parts.Add($"Current state: {status.Replace('_', ' ')}.");
        return string.Join(" ", parts);
    }
}
