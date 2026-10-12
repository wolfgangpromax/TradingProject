using Microsoft.AspNetCore.Mvc;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Api.Controllers;

[ApiController]
[Route("api/market-data")]
public sealed class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IMarketStructureAnalysisService _marketStructureAnalysisService;
    private readonly ICandleRepository _candleRepository;
    private readonly IMarketSymbolRepository _marketSymbolRepository;
    private readonly IMarketStructureReplayService _marketStructureReplayService;
    private readonly IFairValueGapReplayService _fairValueGapReplayService;
    private readonly IAverageTrueRangeService _averageTrueRangeService;
    private readonly IFairValueGapQualificationService _fairValueGapQualificationService;
    private readonly IFairValueGapQualificationReplayService _fairValueGapQualificationReplayService;

    public MarketDataController(IMarketDataService marketDataService, IMarketStructureAnalysisService marketStructureAnalysisService,
        ICandleRepository candleRepository, IMarketSymbolRepository marketSymbolRepository, 
        IMarketStructureReplayService marketStructureReplayService, IFairValueGapReplayService fairValueGapReplayService,
        IAverageTrueRangeService averageTrueRangeService, IFairValueGapQualificationService fairValueGapQualificationService,
        IFairValueGapQualificationReplayService fairValueGapQualificationReplayService)
    {
        _marketDataService = marketDataService;
        _marketStructureAnalysisService = marketStructureAnalysisService;
        _candleRepository = candleRepository;
        _marketSymbolRepository = marketSymbolRepository;
        _marketStructureReplayService = marketStructureReplayService;
        _fairValueGapReplayService = fairValueGapReplayService;
        _averageTrueRangeService = averageTrueRangeService;
        _fairValueGapQualificationService = fairValueGapQualificationService;
        _fairValueGapQualificationReplayService = fairValueGapQualificationReplayService;
    }

    [HttpPost("symbols/sync")]
    public async Task<IActionResult> SynchronizeSymbols(CancellationToken cancellationToken)
    {
        var count = await _marketDataService.SynchronizeSymbolsAsync(cancellationToken);

        return Ok(new
        {
            SynchronizedSymbols = count
        });
    }

    [HttpPost("candles/sync")]
    public async Task<IActionResult> SynchronizeCandles([FromQuery] string symbol, [FromQuery] Timeframe timeframe, [FromQuery] int limit = 500, CancellationToken cancellationToken = default)
    {
        var candles = await _marketDataService.SynchronizeCandlesAsync("BINANCE", symbol, timeframe, limit, cancellationToken);

        return Ok(new
        {
            Symbol = symbol,
            Timeframe = timeframe.ToString(),
            SynchronizedCandles = candles.Count
        });
    }

    [HttpGet("analysis/{symbol}")]
    public async Task<IActionResult> GetAnalysisAsync(string symbol, [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes,
    [FromQuery] int limit = 500, [FromQuery] int swingStrength = 2, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000)
        {
            return BadRequest("Limit must be between 1 and 5000.");
        }

        if (swingStrength < 1)
        {
            return BadRequest("Swing strength must be at least 1.");
        }

        const string exchange = "BINANCE";

        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync(exchange, symbol, cancellationToken);

        if (marketSymbol is null)
        {
            return NotFound($"Symbol '{symbol}' was not found for {exchange}.");
        }

        var candles = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, timeframe, limit, cancellationToken);

        if (candles.Count == 0)
        {
            return NotFound($"No candles found for {symbol} / {timeframe}.");
        }

        var analysis = _marketStructureAnalysisService.Analyze(candles, swingStrength);

        return Ok(new
        {
            marketSymbol.Exchange,
            marketSymbol.Symbol,

            Timeframe = timeframe.ToString(),

            CandleCount = candles.Count,

            analysis.AnalysisTime,

            Trend = analysis.Trend.ToString(),

            SwingCount = analysis.Swings.Count,

            StructurePointCount = analysis.StructurePoints.Count,
            StructureBreakCount = analysis.StructureBreaks.Count,

            LatestSwing = analysis.Swings
                .OrderByDescending(x => x.ConfirmedAt)
                .FirstOrDefault(),

            LatestStructure = analysis.StructurePoints
                .OrderByDescending(x => x.ConfirmedAt)
                .FirstOrDefault(),

            LatestHighStructure = analysis.StructurePoints
                .Where(x => x.SwingType == SwingType.High)
                .OrderByDescending(x => x.ConfirmedAt)
                .FirstOrDefault(),

            LatestLowStructure = analysis.StructurePoints
                .Where(x => x.SwingType == SwingType.Low)
                .OrderByDescending(x => x.ConfirmedAt)
                .FirstOrDefault(),

            LatestStructureBreak = analysis.StructureBreaks
                .OrderByDescending(x => x.BreakTime)
                .FirstOrDefault()
        });
    }

    [HttpGet("analysis/{symbol}/replay")]
    public async Task<IActionResult> GetReplayAsync(string symbol, [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes,
    [FromQuery] int limit = 500, [FromQuery] int swingStrength = 2, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000)
        {
            return BadRequest("Limit must be between 1 and 5000.");
        }

        if (swingStrength < 1)
        {
            return BadRequest("Swing strength must be at least 1.");
        }

        const string exchange = "BINANCE";

        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync(
                exchange,
                symbol,
                cancellationToken);

        if (marketSymbol is null)
        {
            return NotFound($"Symbol '{symbol}' was not found for {exchange}.");
        }

        var candles =await _candleRepository.GetRecentAsync(
                marketSymbol.MarketSymbolId,
                timeframe,
                limit,
                cancellationToken);

        if (candles.Count == 0)
        {
            return NotFound($"No candles found for {symbol} / {timeframe}.");
        }

        var snapshots =
            _marketStructureReplayService
                .Replay(
                    candles,
                    swingStrength)
                .OrderBy(x => x.Time)
                .ToList();

        var latestSnapshot =
            snapshots.LastOrDefault();

        var structureBreaks = snapshots
            .SelectMany(x => x.StructureBreaks)
            .OrderBy(x => x.BreakTime)
            .ToList();

        var bullishBosCount = structureBreaks.Count(x =>
            x.Type == StructureBreakType.BreakOfStructure &&
            x.Direction == BreakDirection.Bullish);

        var bearishBosCount = structureBreaks.Count(x =>
            x.Type == StructureBreakType.BreakOfStructure &&
            x.Direction == BreakDirection.Bearish);

        var bullishChochCount = structureBreaks.Count(x =>
            x.Type == StructureBreakType.ChangeOfCharacter &&
            x.Direction == BreakDirection.Bullish);

        var bearishChochCount = structureBreaks.Count(x =>
            x.Type == StructureBreakType.ChangeOfCharacter &&
            x.Direction == BreakDirection.Bearish);

        return Ok(new
        {
            marketSymbol.Exchange,
            marketSymbol.Symbol,

            Timeframe = timeframe.ToString(),
            CandleCount = candles.Count,
            SnapshotCount = snapshots.Count,
            FirstSnapshotTime = snapshots.FirstOrDefault()?.Time,
            LastSnapshotTime = latestSnapshot?.Time,
            CurrentTrend = latestSnapshot?.Trend.ToString(),
            StructureBreakCount = structureBreaks.Count,
            BullishBosCount = bullishBosCount,
            BearishBosCount = bearishBosCount,
            BullishChochCount = bullishChochCount,
            BearishChochCount = bearishChochCount,
            LatestStructureBreak = structureBreaks.LastOrDefault(),

            RecentStructureBreaks = structureBreaks.TakeLast(10).ToList()
        });
    }

    [HttpGet("analysis/{symbol}/fvg-replay")]
    public async Task<IActionResult> GetFairValueGapReplayAsync(string symbol, [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes,
    [FromQuery] int limit = 500, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000)
        {
            return BadRequest("Limit must be between 1 and 5000.");
        }

        const string exchange = "BINANCE";
        const int atrPeriod = 14;

        // ---------------------------------------------------------
        // 1. Get market symbol
        // ---------------------------------------------------------

        var marketSymbol =
            await _marketSymbolRepository.GetBySymbolAsync(
                exchange,
                symbol,
                cancellationToken);

        if (marketSymbol is null)
        {
            return NotFound(
                $"Symbol '{symbol}' was not found for {exchange}.");
        }

        // ---------------------------------------------------------
        // 2. Load recent candles
        // ---------------------------------------------------------

        var candles =
            await _candleRepository.GetRecentAsync(
                marketSymbol.MarketSymbolId,
                timeframe,
                limit,
                cancellationToken);

        if (candles.Count == 0)
        {
            return NotFound(
                $"No candles found for {symbol} / {timeframe}.");
        }

        // ---------------------------------------------------------
        // 3. Replay Fair Value Gaps
        // ---------------------------------------------------------

        var snapshots =
            _fairValueGapReplayService
                .Replay(candles)
                .OrderBy(x => x.Time)
                .ToList();

        var latestSnapshot =
            snapshots.LastOrDefault();

        // ---------------------------------------------------------
        // 4. Calculate latest ATR
        // ---------------------------------------------------------

        var averageTrueRange =
            _averageTrueRangeService.Calculate(
                candles,
                atrPeriod);

        // ---------------------------------------------------------
        // 5. Get unique FVG states from latest snapshot
        // ---------------------------------------------------------

        var gaps =
            latestSnapshot?.Gaps
                .OrderBy(x => x.Gap.ConfirmedAt)
                .ToList()
            ?? [];

        // ---------------------------------------------------------
        // 6. Build qualification context
        // ---------------------------------------------------------

        var qualificationContext =
            new FairValueGapQualificationContext
            {
                CurrentTime =
                    latestSnapshot?.Time
                    ?? candles.Max(x => x.CloseTime),

                AverageTrueRange =
                    averageTrueRange
            };

        // ---------------------------------------------------------
        // 7. Qualify every FVG
        // ---------------------------------------------------------

        var qualifiedGaps =
            gaps
                .Select(state => new
                {
                    State = state,

                    Qualification =
                        _fairValueGapQualificationService
                            .Qualify(
                                state,
                                qualificationContext)
                })
                .ToList();

        // ---------------------------------------------------------
        // 8. Raw FVG statistics
        // ---------------------------------------------------------

        var bullishGapCount =
            gaps.Count(x =>
                x.Gap.Type ==
                FairValueGapType.Bullish);

        var bearishGapCount =
            gaps.Count(x =>
                x.Gap.Type ==
                FairValueGapType.Bearish);

        var mitigatedGapCount =
            gaps.Count(x =>
                x.IsMitigated);

        var activeGapCount =
            gaps.Count(x =>
                !x.IsMitigated);

        // ---------------------------------------------------------
        // 9. Qualification statistics
        // ---------------------------------------------------------

        var qualifiedGapCount =
            qualifiedGaps.Count(x =>
                x.Qualification.IsQualified);

        var rejectedGapCount =
            qualifiedGaps.Count(x =>
                !x.Qualification.IsQualified);

        // ---------------------------------------------------------
        // 10. Response
        // ---------------------------------------------------------

        return Ok(new
        {
            marketSymbol.Exchange,
            marketSymbol.Symbol,

            Timeframe = timeframe.ToString(),
            CandleCount = candles.Count,
            SnapshotCount = snapshots.Count,
            FirstSnapshotTime = snapshots.FirstOrDefault()?.Time,
            LastSnapshotTime = latestSnapshot?.Time,

            // ATR
            AtrPeriod = atrPeriod,
            AverageTrueRange = averageTrueRange,

            // Raw FVG statistics
            TotalGapCount = gaps.Count,
            BullishGapCount = bullishGapCount,
            BearishGapCount = bearishGapCount,
            MitigatedGapCount = mitigatedGapCount,
            ActiveGapCount = activeGapCount,

            // Qualification statistics
            QualifiedGapCount = qualifiedGapCount,
            RejectedGapCount = rejectedGapCount,

            // Most recently detected raw FVGs
            RecentGaps =
                gaps
                    .OrderByDescending(x =>
                        x.Gap.ConfirmedAt)
                    .Take(10)
                    .Select(x => new
                    {
                        Type =
                            x.Gap.Type.ToString(),

                        x.Gap.StartTime,
                        x.Gap.ConfirmedAt,
                        x.Gap.LowerPrice,
                        x.Gap.UpperPrice,
                        x.Gap.GapSize,

                        GapAtrRatio =
                            averageTrueRange is > 0m
                                ? x.Gap.GapSize /
                                  averageTrueRange.Value
                                : (decimal?)null,

                        x.FillPercentage,
                        x.IsMitigated,
                        x.MitigatedAt
                    })
                    .ToList(),

            // Most recently detected FVGs that passed qualification
            QualifiedGaps =
                qualifiedGaps
                    .Where(x =>
                        x.Qualification.IsQualified)
                    .OrderByDescending(x =>
                        x.State.Gap.ConfirmedAt)
                    .Take(10)
                    .Select(x => new
                    {
                        Type =
                            x.State.Gap.Type.ToString(),

                        x.State.Gap.StartTime,
                        x.State.Gap.ConfirmedAt,
                        x.State.Gap.LowerPrice,
                        x.State.Gap.UpperPrice,
                        x.State.Gap.GapSize,

                        GapAtrRatio =
                            averageTrueRange is > 0m
                                ? x.State.Gap.GapSize /
                                  averageTrueRange.Value
                                : (decimal?)null,

                        x.State.FillPercentage,
                        x.State.IsMitigated,
                        x.State.MitigatedAt
                    })
                    .ToList(),

            // Why FVGs failed qualification.
            // One FVG may have multiple rejection reasons.
            RejectionReasons =
                qualifiedGaps
                    .Where(x =>
                        !x.Qualification.IsQualified)
                    .SelectMany(x =>
                        x.Qualification.RejectionReasons)
                    .GroupBy(x => x)
                    .Select(x => new
                    {
                        Reason = x.Key,
                        Count = x.Count()
                    })
                    .OrderByDescending(x =>
                        x.Count)
                    .ToList()
        });
    }

    [HttpGet("analysis/{symbol}/fvg-qualification-replay")]
    public async Task<IActionResult> GetFairValueGapQualificationReplayAsync(string symbol, [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes,
    [FromQuery] int limit = 500, [FromQuery] int atrPeriod = 14, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000)
        {
            return BadRequest("Limit must be between 1 and 5000.");
        }

        if (atrPeriod is < 1 or > 200)
        {
            return BadRequest("ATR period must be between 1 and 200.");
        }

        const string exchange = "BINANCE";

        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync(exchange, symbol, cancellationToken);

        if (marketSymbol is null)
        {
            return NotFound($"Symbol '{symbol}' was not found for {exchange}.");
        }

        var candles = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, timeframe, limit, cancellationToken);

        if (candles.Count == 0)
        {
            return NotFound($"No candles found for {symbol} / {timeframe}.");
        }

        var snapshots =
            _fairValueGapQualificationReplayService
                .Replay(
                    candles,
                    atrPeriod)
                .OrderBy(x => x.Time)
                .ToList();

        var latestSnapshot = snapshots.LastOrDefault();

        if (latestSnapshot is null)
        {
            return Ok(new
            {
                marketSymbol.Exchange,
                marketSymbol.Symbol,
                Timeframe = timeframe.ToString(),
                CandleCount = candles.Count,
                SnapshotCount = 0
            });
        }

        // ---------------------------------------------------------
        // Existing FVG qualification/alignment results
        // ---------------------------------------------------------

        var results =
            latestSnapshot.Qualifications
                .Select(qualification =>
                {
                    var alignment =
                        latestSnapshot.Alignments
                            .FirstOrDefault(x =>
                                x.State.Gap.Key ==
                                qualification.State.Gap.Key);

                    return new
                    {
                        State = qualification.State,
                        Qualification = qualification,
                        Alignment = alignment
                    };
                })
                .ToList();

        // ---------------------------------------------------------
        // Active FVGs
        // ---------------------------------------------------------

        var active =
            results
                .Where(x =>
                    !x.State.IsMitigated)
                .ToList();

        // ---------------------------------------------------------
        // Qualified active FVGs
        // ---------------------------------------------------------

        var qualified =
            active
                .Where(x =>
                    x.Qualification.IsQualified)
                .ToList();

        // ---------------------------------------------------------
        // Qualified + market-structure aligned FVGs
        // ---------------------------------------------------------

        var aligned =
            qualified
                .Where(x =>
                    x.Alignment?.IsAligned == true)
                .ToList();

        // ---------------------------------------------------------
        // Entry confirmation counts
        // ---------------------------------------------------------

        var waitingEntryConfirmationCount = latestSnapshot.EntryConfirmations.Count(x => x.Status == EntryConfirmationStatus.Waiting);
        var confirmedEntryConfirmationCount = latestSnapshot.EntryConfirmations.Count(x => x.Status == EntryConfirmationStatus.Confirmed);
        var invalidatedEntryConfirmationCount = latestSnapshot.EntryConfirmations.Count(x => x.Status == EntryConfirmationStatus.Invalidated);
        var tradeSetupCount = latestSnapshot.TradeSetups.Count;
        var activeTradeCount = latestSnapshot.TradeOutcomes.Count(x => x.Status == TradeOutcomeStatus.Active);
        var wonTradeCount = latestSnapshot.TradeOutcomes.Count(x => x.Status == TradeOutcomeStatus.Won);
        var lostTradeCount = latestSnapshot.TradeOutcomes.Count(x => x.Status == TradeOutcomeStatus.Lost);
        var approvedTradeCount = latestSnapshot.TradeRiskAssessments.Count(x => x.IsApproved);
        var rejectedTradeCount = latestSnapshot.TradeRiskAssessments.Count(x => !x.IsApproved);
        var pendingOrderCount = latestSnapshot.TradeOrders.Count - latestSnapshot.TradeExecutions.Count;
        var filledExecutionCount = latestSnapshot.TradeExecutions.Count(x => x.Status == OrderStatus.Filled);
        // ---------------------------------------------------------
        // API response
        // ---------------------------------------------------------

        return Ok(new
        {
            marketSymbol.Exchange,
            marketSymbol.Symbol,

            Timeframe = timeframe.ToString(),
            CandleCount = candles.Count,
            SnapshotCount = snapshots.Count,
            FirstSnapshotTime = snapshots.First().Time,
            LastSnapshotTime = latestSnapshot.Time,
            AtrPeriod = atrPeriod,
            CurrentAverageTrueRange = latestSnapshot.AverageTrueRange,
            CurrentMarketTrend = latestSnapshot.MarketTrend.ToString(),
            TotalGapCount = results.Count,
            ActiveGapCount = active.Count,
            QualifiedActiveGapCount = qualified.Count,
            AlignedQualifiedActiveGapCount = aligned.Count, 

            // -----------------------------------------------------
            // Entry confirmation summary
            // -----------------------------------------------------

            WaitingEntryConfirmationCount = waitingEntryConfirmationCount,
            ConfirmedEntryConfirmationCount = confirmedEntryConfirmationCount,
            InvalidatedEntryConfirmationCount = invalidatedEntryConfirmationCount,
            TradeSetupCount = tradeSetupCount,
            ActiveTradeCount = activeTradeCount,
            WonTradeCount = wonTradeCount,
            LostTradeCount = lostTradeCount,
            ApprovedTradeCount = approvedTradeCount,
            RejectedTradeCount = rejectedTradeCount,
            PendingOrderCount = pendingOrderCount,
            FilledExecutionCount = filledExecutionCount,

            // -----------------------------------------------------
            // Existing FVG candidates
            // -----------------------------------------------------

            Candidates =
                aligned
                    .OrderByDescending(x =>
                        x.State.Gap.ConfirmedAt)
                    .Select(x => new
                    {
                        Type =
                            x.State.Gap.Type.ToString(),

                        x.State.Gap.StartTime,
                        x.State.Gap.ConfirmedAt,
                        x.State.Gap.LowerPrice,
                        x.State.Gap.UpperPrice,
                        x.State.Gap.GapSize,
                        x.State.FillPercentage,
                        QualificationReasons = x.Qualification.RejectionReasons,
                        MarketTrend = x.Alignment!.MarketTrend.ToString(),
                        x.Alignment.IsAligned,
                        AlignmentReason = x.Alignment.Reason
                    })
                    .ToList(),

            // -----------------------------------------------------
            // FVG entry-confirmation lifecycle
            // -----------------------------------------------------

            EntryConfirmations =
                latestSnapshot.EntryConfirmations
                    .OrderByDescending(x => x.Interaction.FirstInteractionTime)
                    .Select(x => new
                    {
                        Type = x.Interaction.Candidate.State.Gap.Type.ToString(),
                        StartTime = x.Interaction.Candidate.State.Gap.StartTime,
                        FvgConfirmedAt = x.Interaction.Candidate.State.Gap.ConfirmedAt,
                        LowerPrice = x.Interaction.Candidate.State.Gap.LowerPrice,
                        UpperPrice = x.Interaction.Candidate.State.Gap.UpperPrice,
                        GapSize = x.Interaction.Candidate.State.Gap.GapSize,
                        FirstInteractionTime = x.Interaction.FirstInteractionTime,
                        FirstInteractionPrice = x.Interaction.FirstInteractionPrice,
                        Status = x.Status.ToString(),
                        EntryConfirmedAt = x.ConfirmedAt,
                        ConfirmationPrice = x.ConfirmationPrice,
                        Reasons = x.Reasons
                    })
                    .ToList(),

            // -----------------------------------------------------
            // Trade setups
            // -----------------------------------------------------
            TradeSetups =
                latestSnapshot.TradeSetups
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        Direction = x.Direction.ToString(),
                        x.EntryPrice,
                        x.StopLossPrice,
                        x.TakeProfitPrice,
                        x.RiskPerUnit,
                        x.RewardPerUnit,
                        x.RiskRewardRatio,
                        x.CreatedAt,

                        Fvg = new
                        {
                            Type = x.EntryConfirmation.Interaction.Candidate.State.Gap.Type.ToString(),
                            x.EntryConfirmation.Interaction.Candidate.State.Gap.LowerPrice,
                            x.EntryConfirmation.Interaction.Candidate.State.Gap.UpperPrice,
                            x.EntryConfirmation.Interaction.Candidate.State.Gap.ConfirmedAt
                        },

                        EntryConfirmation = new
                        {
                            x.EntryConfirmation.Interaction.FirstInteractionTime,
                            x.EntryConfirmation.Interaction.FirstInteractionPrice,
                            x.EntryConfirmation.ConfirmedAt,
                            x.EntryConfirmation.ConfirmationPrice,
                            x.EntryConfirmation.Reasons
                        }
                    })
                    .ToList(),

                TradeOutcomes = latestSnapshot.TradeOutcomes.Select(x => new
                {
                    Direction = x.Setup.Direction.ToString(),
                    EntryPrice = x.Setup.EntryPrice,
                    StopLossPrice = x.Setup.StopLossPrice,
                    TakeProfitPrice = x.Setup.TakeProfitPrice,
                    Status = x.Status.ToString(),
                    ClosedAt = x.ClosedAt,
                    ExitPrice = x.ExitPrice,
                    RealizedRiskRewardRatio = x.RealizedRiskRewardRatio,
                    CreatedAt = x.Setup.CreatedAt,
                    Reasons = x.Reasons
                }),

                PositionSizes = latestSnapshot.PositionSizes.Select(x => new
                {
                    Direction = x.Setup.Direction.ToString(),
                    AccountEquity = x.AccountEquity,
                    RiskPercentage = x.RiskPercentage,
                    AccountRiskAmount = x.AccountRiskAmount,
                    RiskPerUnit = x.RiskPerUnit,
                    Quantity = x.Quantity,
                    PositionValue = x.PositionValue,
                    CreatedAt = x.Setup.CreatedAt,
                    Reasons = x.Reasons
                }),

                TradeRiskAssessments = latestSnapshot.TradeRiskAssessments.Select(x => new
                {
                    Direction = x.PositionSize.Setup.Direction.ToString(),
                    IsApproved = x.IsApproved,
                    TradesToday = x.TradesToday,
                    ActiveTrades = x.ActiveTrades,
                    DailyRealizedR = x.DailyRealizedR,
                    CreatedAt = x.PositionSize.Setup.CreatedAt,
                    RejectionReasons = x.RejectionReasons
                }),

                TradeOrders = latestSnapshot.TradeOrders.Select(x => new
                {
                    Direction = x.Direction.ToString(), OrderType = x.OrderType.ToString(), x.Quantity, x.RequestedPrice, x.CreatedAt, Status = x.Status.ToString(), x.Reasons
                }),

                TradeExecutions = latestSnapshot.TradeExecutions.Select(x => new
                {
                    Status = x.Status.ToString(), x.FilledPrice, x.FilledQuantity, x.ExecutedAt, x.Reasons
                }),

        });
    }
}