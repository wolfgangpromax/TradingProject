using Microsoft.AspNetCore.Mvc;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Api.Controllers;

[ApiController]
[Route("api/strategy")]
public sealed class StrategyController : ControllerBase
{
    private readonly IMarketSymbolRepository _marketSymbolRepository;
    private readonly ICandleRepository _candleRepository;
    private readonly IFairValueGapQualificationReplayService _replayService;
    private readonly IMarketStructureReplayService _marketStructureReplayService;

    public StrategyController(IMarketSymbolRepository marketSymbolRepository, ICandleRepository candleRepository,
        IFairValueGapQualificationReplayService replayService, IMarketStructureReplayService marketStructureReplayService)
    {
        _marketSymbolRepository = marketSymbolRepository;
        _candleRepository = candleRepository;
        _replayService = replayService;
        _marketStructureReplayService = marketStructureReplayService;
    }

    [HttpGet("backtest/{symbol}")]
    public async Task<IActionResult> Backtest(string symbol, [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes,
        [FromQuery] int limit = 500, [FromQuery] int atrPeriod = 14, [FromQuery] decimal startingCapital = 100m,
        [FromQuery] decimal riskPercentage = 1m, [FromQuery] decimal leverage = 40m, CancellationToken cancellationToken = default)
    {
        if (limit is < 50 or > 5000) return BadRequest("Limit must be between 50 and 5000.");
        if (atrPeriod is < 1 or > 200) return BadRequest("ATR period must be between 1 and 200.");
        if (startingCapital <= 0m) return BadRequest("Starting capital must be greater than zero.");
        if (riskPercentage <= 0m || riskPercentage > 100m) return BadRequest("Risk percentage must be greater than 0 and at most 100.");
        if (leverage < 1m || leverage > 125m) return BadRequest("Leverage must be between 1 and 125.");

        var data = await LoadReplay(symbol, timeframe, limit, atrPeriod, cancellationToken);
        if (data.Error is not null) return data.Error;
        var latest = data.Snapshots!.LastOrDefault();
        if (latest is null) return Ok(new { Symbol = symbol, TradeCount = 0 });

        var closed = latest.TradeOutcomes.Where(x => x.Status != TradeOutcomeStatus.Active && x.ClosedAt.HasValue)
            .OrderBy(x => x.ClosedAt).ToList();
        var equity = startingCapital;
        var peak = equity;
        var maxDrawdown = 0m;
        var curve = new List<object> { new { Time = data.Snapshots.First().Time, Equity = equity, RealizedR = 0m } };
        var wins = 0; var losses = 0; var totalR = 0m; var grossWinR = 0m; var grossLossR = 0m; var consecutiveLosses = 0; var maxConsecutiveLosses = 0;

        foreach (var outcome in closed)
        {
            var realizedR = outcome.RealizedRiskRewardRatio ?? 0m;
            var riskAmount = equity * (riskPercentage / 100m);
            equity += riskAmount * realizedR;
            totalR += realizedR;
            if (realizedR > 0m) { wins++; grossWinR += realizedR; consecutiveLosses = 0; }
            else if (realizedR < 0m) { losses++; grossLossR += Math.Abs(realizedR); consecutiveLosses++; maxConsecutiveLosses = Math.Max(maxConsecutiveLosses, consecutiveLosses); }
            peak = Math.Max(peak, equity);
            if (peak > 0m) maxDrawdown = Math.Max(maxDrawdown, (peak - equity) / peak * 100m);
            curve.Add(new { Time = outcome.ClosedAt, Equity = Math.Round(equity, 4), RealizedR = realizedR });
        }

        var approved = latest.TradeRiskAssessments.Count(x => x.IsApproved);
        var rejected = latest.TradeRiskAssessments.Count - approved;
        var executions = latest.TradeExecutions.Count(x => x.Status == OrderStatus.Filled);
        var active = latest.TradeOutcomes.Count(x => x.Status == TradeOutcomeStatus.Active);
        var tradeCount = wins + losses;
        var winRate = tradeCount == 0 ? 0m : (decimal)wins / tradeCount * 100m;
        var expectancy = tradeCount == 0 ? 0m : totalR / tradeCount;
        decimal? profitFactor = grossLossR == 0m ? null : grossWinR / grossLossR;
        var returnPct = (equity - startingCapital) / startingCapital * 100m;

        var marginPreview = latest.PositionSizes.OrderByDescending(x => x.Setup.CreatedAt).FirstOrDefault();
        return Ok(new
        {
            Exchange = "BINANCE", Symbol = symbol.ToUpperInvariant(), Timeframe = timeframe.ToString(), CandleCount = data.CandleCount,
            Parameters = new { StartingCapital = startingCapital, RiskPercentage = riskPercentage, Leverage = leverage, AtrPeriod = atrPeriod },
            Portfolio = new { StartingCapital = startingCapital, EndingEquity = Math.Round(equity, 4), NetProfit = Math.Round(equity - startingCapital, 4), ReturnPercentage = Math.Round(returnPct, 4), PeakEquity = Math.Round(peak, 4), MaxDrawdownPercentage = Math.Round(maxDrawdown, 4) },
            Metrics = new { TradeCount = tradeCount, Wins = wins, Losses = losses, Active = active, WinRate = Math.Round(winRate, 2), TotalR = totalR, ExpectancyR = Math.Round(expectancy, 4), ProfitFactor = profitFactor, MaxConsecutiveLosses = maxConsecutiveLosses, Approved = approved, Rejected = rejected, FilledExecutions = executions },
            LatestPositionPreview = marginPreview is null ? null : (object)new { marginPreview.PositionValue, Leverage = leverage, RequiredMargin = Math.Round(marginPreview.PositionValue / leverage, 4) },
            EquityCurve = curve,
            Note = "Leverage currently affects required-margin preview only. Liquidation, exchange fees, funding and slippage are not yet modeled."
        });
    }

    [HttpGet("watch")]
    public async Task<IActionResult> Watch([FromQuery] string symbols = "BTCUSDT,ETHUSDT,SOLUSDT", [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes,
        [FromQuery] int limit = 500, [FromQuery] int atrPeriod = 14, CancellationToken cancellationToken = default)
    {
        var requested = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList();
        var rows = new List<object>();
        foreach (var symbol in requested)
        {
            var data = await LoadReplay(symbol, timeframe, limit, atrPeriod, cancellationToken);
            if (data.Error is not null) { rows.Add(new { Symbol = symbol.ToUpperInvariant(), Status = "NO_DATA" }); continue; }
            var latest = data.Snapshots!.LastOrDefault();
            if (latest is null) { rows.Add(new { Symbol = symbol.ToUpperInvariant(), Status = "NO_SETUP" }); continue; }
            var setup = latest.TradeSetups.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var risk = latest.TradeRiskAssessments.OrderByDescending(x => x.PositionSize.Setup.CreatedAt).FirstOrDefault();
            var execution = latest.TradeExecutions.OrderByDescending(x => x.ExecutedAt).FirstOrDefault();
            var confirmation = latest.EntryConfirmations.OrderByDescending(x => x.ConfirmedAt ?? x.Interaction.FirstInteractionTime).FirstOrDefault();
            var candidate = latest.Candidates.Where(x => x.IsCandidate).OrderByDescending(x => x.State.Gap.ConfirmedAt).FirstOrDefault();
            var status = execution?.Status == OrderStatus.Filled ? "ACTIVE_OR_COMPLETED" : risk is not null && !risk.IsApproved ? "REJECTED" : setup is not null ? "ORDER_PENDING" : confirmation?.Status == EntryConfirmationStatus.Confirmed ? "ENTRY_CONFIRMED" : confirmation?.Interaction.HasInteracted == true ? "WAITING_FOR_ENTRY" : candidate is not null ? "WATCHING_SETUP" : "NO_SETUP";
            rows.Add(new { Symbol = symbol.ToUpperInvariant(), Status = status, Trend = latest.MarketTrend.ToString(), Direction = setup?.Direction.ToString(), TargetEntry = setup?.EntryPrice, StopLoss = setup?.StopLossPrice, TakeProfit = setup?.TakeProfitPrice, RiskRewardRatio = setup?.RiskRewardRatio, FvgLower = candidate?.State.Gap.LowerPrice, FvgUpper = candidate?.State.Gap.UpperPrice, RiskApproved = risk?.IsApproved, RiskReasons = risk?.RejectionReasons });
        }
        return Ok(rows);
    }

    [HttpGet("multi-timeframe/{symbol}")]
    public async Task<IActionResult> MultiTimeframe(string symbol, [FromQuery] int limit = 500, CancellationToken cancellationToken = default)
    {
        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync("BINANCE", symbol, cancellationToken);
        if (marketSymbol is null) return NotFound($"Symbol '{symbol}' was not found for BINANCE.");
        var timeframes = new[] { Timeframe.FiveMinutes, Timeframe.FifteenMinutes, Timeframe.OneHour, Timeframe.FourHours };
        var rows = new List<object>();
        foreach (var timeframe in timeframes)
        {
            var candles = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, timeframe, limit, cancellationToken);
            var latest = _marketStructureReplayService.Replay(candles).OrderBy(x => x.Time).LastOrDefault();
            rows.Add(new { Timeframe = timeframe.ToString(), CandleCount = candles.Count, Trend = latest?.Trend.ToString() ?? "Unknown", Time = latest?.Time });
        }
        return Ok(new { Exchange = "BINANCE", Symbol = symbol.ToUpperInvariant(), Timeframes = rows });
    }

    [HttpGet("integrations")]
    public IActionResult Integrations() => Ok(new
    {
        NewsFundamentals = new { Status = "NOT_CONFIGURED", Message = "Add a news provider/API key before enabling news or fundamental scoring." },
        AI = new { Status = "NOT_CONFIGURED", Message = "Connect an IAIProvider implementation (for example local Ollama or a cloud provider). AI must not bypass deterministic risk rules." },
        PaperTrading = new { Status = "HISTORICAL_REPLAY_AVAILABLE", Message = "Use the backtest endpoint for historical paper simulation. Forward paper broker/exchange execution is not connected yet." },
        LiveTrading = new { Status = "DISABLED", Message = "No live order provider is enabled. Add an authenticated exchange execution adapter only after backtest and forward-paper validation." }
    });

    private async Task<(IReadOnlyCollection<FairValueGapQualificationSnapshot>? Snapshots, int CandleCount, IActionResult? Error)> LoadReplay(string symbol, Timeframe timeframe, int limit, int atrPeriod, CancellationToken cancellationToken)
    {
        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync("BINANCE", symbol, cancellationToken);
        if (marketSymbol is null) return (null, 0, NotFound($"Symbol '{symbol}' was not found for BINANCE."));
        var candles = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, timeframe, limit, cancellationToken);
        if (candles.Count == 0) return (null, 0, NotFound($"No candles found for {symbol} / {timeframe}."));
        return (_replayService.Replay(candles, atrPeriod).OrderBy(x => x.Time).ToList(), candles.Count, null);
    }
}
