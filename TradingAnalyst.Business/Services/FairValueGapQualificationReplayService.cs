using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Domain.ValueObjects;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapQualificationReplayService : IFairValueGapQualificationReplayService
{
    private readonly IFairValueGapReplayService _fairValueGapReplayService;
    private readonly IAverageTrueRangeService _averageTrueRangeService;
    private readonly IFairValueGapQualificationService _fairValueGapQualificationService;
    private readonly IMarketStructureReplayService _marketStructureReplayService;
    private readonly IFairValueGapAlignmentService _fairValueGapAlignmentService;
    private readonly IFairValueGapCandidateService _fairValueGapCandidateService;
    private readonly IFairValueGapInteractionService _fairValueGapInteractionService;
    private readonly IFairValueGapEntryConfirmationService _entryConfirmationService;
    private readonly ITradeSetupService _tradeSetupService;
    private readonly ITradeOutcomeService _tradeOutcomeService;
    private readonly IPositionSizingService _positionSizingService;
    private readonly IRiskManagementService _riskManagementService;
    private readonly RiskManagementOptions _riskManagementOptions;
    private readonly ITradeExecutionService _tradeExecutionService;

    public FairValueGapQualificationReplayService(
        IFairValueGapReplayService fairValueGapReplayService,
        IAverageTrueRangeService averageTrueRangeService,
        IFairValueGapQualificationService fairValueGapQualificationService,
        IMarketStructureReplayService marketStructureReplayService,
        IFairValueGapAlignmentService fairValueGapAlignmentService,
        IFairValueGapCandidateService fairValueGapCandidateService,
        IFairValueGapInteractionService fairValueGapInteractionService,
        IFairValueGapEntryConfirmationService entryConfirmationService,
        ITradeSetupService tradeSetupService,
        ITradeOutcomeService tradeOutcomeService,
        IPositionSizingService positionSizingService,
        IRiskManagementService riskManagementService,
        IOptions<RiskManagementOptions> riskManagementOptions,         
        ITradeExecutionService tradeExecutionService)
    {
        ArgumentNullException.ThrowIfNull(fairValueGapReplayService);
        ArgumentNullException.ThrowIfNull(averageTrueRangeService);
        ArgumentNullException.ThrowIfNull(fairValueGapQualificationService);
        ArgumentNullException.ThrowIfNull(marketStructureReplayService);
        ArgumentNullException.ThrowIfNull(fairValueGapAlignmentService);
        ArgumentNullException.ThrowIfNull(fairValueGapCandidateService);
        ArgumentNullException.ThrowIfNull(fairValueGapInteractionService);
        ArgumentNullException.ThrowIfNull(entryConfirmationService);
        ArgumentNullException.ThrowIfNull(tradeSetupService);
        ArgumentNullException.ThrowIfNull(tradeOutcomeService);
        ArgumentNullException.ThrowIfNull(positionSizingService);
        ArgumentNullException.ThrowIfNull(riskManagementService);
        ArgumentNullException.ThrowIfNull(riskManagementOptions);
        ArgumentNullException.ThrowIfNull(tradeExecutionService);

        _fairValueGapReplayService = fairValueGapReplayService;
        _averageTrueRangeService = averageTrueRangeService;
        _fairValueGapQualificationService = fairValueGapQualificationService;
        _marketStructureReplayService = marketStructureReplayService;
        _fairValueGapAlignmentService = fairValueGapAlignmentService;
        _fairValueGapCandidateService = fairValueGapCandidateService;
        _fairValueGapInteractionService = fairValueGapInteractionService;
        _entryConfirmationService = entryConfirmationService;
        _tradeSetupService = tradeSetupService;
        _tradeOutcomeService = tradeOutcomeService;
        _positionSizingService = positionSizingService;
        _riskManagementService = riskManagementService;
        _riskManagementOptions = riskManagementOptions.Value;
        _tradeExecutionService = tradeExecutionService;
    }

    public IReadOnlyCollection<FairValueGapQualificationSnapshot> Replay(
        IReadOnlyCollection<Candle> candles,
        int atrPeriod)
    {
        ArgumentNullException.ThrowIfNull(candles);

        if (atrPeriod < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(atrPeriod),
                "ATR period must be at least 1.");
        }

        // ---------------------------------------------------------
        // 1. Only use closed candles and keep chronological order
        // ---------------------------------------------------------

        var orderedCandles = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTime)
            .ToList();

        if (orderedCandles.Count < 3)
        {
            return [];
        }

        // ---------------------------------------------------------
        // 2. Replay FVG state through time
        // ---------------------------------------------------------

        var fvgSnapshots = _fairValueGapReplayService
            .Replay(orderedCandles)
            .OrderBy(x => x.Time)
            .ToList();

        // ---------------------------------------------------------
        // 3. Replay market structure through time
        // ---------------------------------------------------------

        var marketStructureSnapshots = _marketStructureReplayService
            .Replay(orderedCandles)
            .OrderBy(x => x.Time)
            .ToList();

        // ---------------------------------------------------------
        // 4. Persistent lifecycle states
        // ---------------------------------------------------------

        var results = new List<FairValueGapQualificationSnapshot>();

        var interactionStates =
            new Dictionary<FairValueGapKey, FairValueGapInteractionState>();

        var entryConfirmationStates =
            new Dictionary<FairValueGapKey, FairValueGapEntryConfirmationState>();

        var tradeSetups =
            new Dictionary<FairValueGapKey, TradeSetup>();

        var tradeOutcomeStates =
            new Dictionary<FairValueGapKey, TradeOutcomeState>();

        var positionSizes =
            new Dictionary<FairValueGapKey, PositionSize>();

        var tradeRiskAssessments =
            new Dictionary<FairValueGapKey, TradeRiskAssessment>();

        var tradeOrders =
            new Dictionary<FairValueGapKey, TradeOrder>();

        var tradeExecutions =
            new Dictionary<FairValueGapKey, TradeExecution>();

        foreach (var fvgSnapshot in fvgSnapshots)
        {
            // -----------------------------------------------------
            // 5. Get candles available at this point in time
            // -----------------------------------------------------

            var availableCandles = orderedCandles
                .Where(x => x.CloseTime <= fvgSnapshot.Time)
                .ToList();

            // -----------------------------------------------------
            // 6. Calculate historical ATR
            // -----------------------------------------------------

            var atr = _averageTrueRangeService.Calculate(
                availableCandles,
                atrPeriod);

            // -----------------------------------------------------
            // 7. Get market structure known at this point
            // -----------------------------------------------------

            var marketStructureSnapshot = marketStructureSnapshots
                .Where(x => x.Time <= fvgSnapshot.Time)
                .OrderByDescending(x => x.Time)
                .FirstOrDefault();

            var marketTrend =
                marketStructureSnapshot?.Trend ?? MarketTrend.Unknown;

            // -----------------------------------------------------
            // 8. Build qualification context
            // -----------------------------------------------------

            var context = new FairValueGapQualificationContext
            {
                CurrentTime = fvgSnapshot.Time,
                AverageTrueRange = atr,
                MarketTrend = marketTrend
            };

            // -----------------------------------------------------
            // 9. Qualify FVGs
            // -----------------------------------------------------

            var qualifications = fvgSnapshot.Gaps
                .Select(state =>
                    _fairValueGapQualificationService.Qualify(
                        state,
                        context))
                .ToList();

            // -----------------------------------------------------
            // 10. Evaluate alignment
            // -----------------------------------------------------

            var alignments = fvgSnapshot.Gaps
                .Select(state =>
                    _fairValueGapAlignmentService.Evaluate(
                        state,
                        marketTrend))
                .ToList();

            // -----------------------------------------------------
            // 11. Build candidates
            // -----------------------------------------------------

            var candidates = qualifications
                .Select(qualification =>
                {
                    var alignment = alignments.Single(x =>
                        x.State.Gap.Key ==
                        qualification.State.Gap.Key);

                    return _fairValueGapCandidateService.Evaluate(
                        qualification,
                        alignment);
                })
                .ToList();

            // -----------------------------------------------------
            // 12. Get current replay candle
            // -----------------------------------------------------

            var currentCandle = orderedCandles.LastOrDefault(x =>
                x.CloseTime == fvgSnapshot.Time);

            // Existing filled trades are updated before new orders are created.
            if (currentCandle is not null)
            {
                foreach (var outcomeState in tradeOutcomeStates.Values) _tradeOutcomeService.Update(outcomeState, currentCandle);

                // Pending orders may fill only on a candle AFTER order creation.
                foreach (var pair in tradeOrders.Where(x => !tradeExecutions.ContainsKey(x.Key)).ToList())
                {
                    var execution = _tradeExecutionService.Execute(pair.Value, currentCandle);
                    if (execution.Status != OrderStatus.Filled) continue;
                    tradeExecutions.Add(pair.Key, execution);
                    if (!tradeOutcomeStates.ContainsKey(pair.Key)) tradeOutcomeStates.Add(pair.Key, new TradeOutcomeState(pair.Value.RiskAssessment.PositionSize.Setup));
                }
            }

            // -----------------------------------------------------
            // 13. Update interaction states
            // -----------------------------------------------------

            foreach (var candidate in candidates)
            {
                if (!candidate.IsCandidate)
                {
                    continue;
                }

                var key = candidate.State.Gap.Key;

                if (!interactionStates.TryGetValue(
                        key,
                        out var interactionState))
                {
                    interactionState =
                        new FairValueGapInteractionState(candidate);

                    interactionStates.Add(
                        key,
                        interactionState);
                }

                if (currentCandle is not null)
                {
                    _fairValueGapInteractionService.Update(
                        interactionState,
                        currentCandle);
                }
            }

            // -----------------------------------------------------
            // 14. Update entry-confirmation states
            // -----------------------------------------------------

            if (currentCandle is not null)
            {
                foreach (var interactionState in interactionStates.Values)
                {
                    if (!interactionState.HasInteracted)
                    {
                        continue;
                    }

                    var key =
                        interactionState.Candidate.State.Gap.Key;

                    if (!entryConfirmationStates.TryGetValue(
                            key,
                            out var confirmationState))
                    {
                        confirmationState =
                            new FairValueGapEntryConfirmationState(
                                interactionState);

                        entryConfirmationStates.Add(
                            key,
                            confirmationState);
                    }

                    _entryConfirmationService.Update(
                        confirmationState,
                        currentCandle);
                }
            }

            // -----------------------------------------------------
            // 15. Create interaction snapshots
            // -----------------------------------------------------

            var interactions = interactionStates.Values
                .Select(x => x.CreateSnapshot())
                .ToList();

            // -----------------------------------------------------
            // 16. Create entry-confirmation snapshots
            // -----------------------------------------------------

            var entryConfirmations = entryConfirmationStates.Values
                .Select(x => x.CreateSnapshot())
                .ToList();

            // -----------------------------------------------------
            // 17. Create new trade setups
            // -----------------------------------------------------

            foreach (var confirmationState in entryConfirmationStates.Values)
            {
                if (confirmationState.Status !=
                        EntryConfirmationStatus.Confirmed ||
                    !confirmationState.ConfirmedAt.HasValue ||
                    !confirmationState.ConfirmationPrice.HasValue)
                {
                    continue;
                }

                var key =
                    confirmationState.Interaction.Candidate.State.Gap.Key;

                if (tradeSetups.ContainsKey(key))
                {
                    continue;
                }

                // Setup must be created only at the exact
                // confirmation snapshot.
                if (confirmationState.ConfirmedAt.Value != fvgSnapshot.Time)
                {
                    continue;
                }

                var entryConfirmation =
                    new FairValueGapEntryConfirmation
                    {
                        Interaction =
                            confirmationState.Interaction.CreateSnapshot(),
                        Status = confirmationState.Status,
                        ConfirmedAt = confirmationState.ConfirmedAt,
                        ConfirmationPrice =
                            confirmationState.ConfirmationPrice,
                        Reasons = confirmationState.Reasons.ToArray()
                    };

                var tradeSetup =
                    _tradeSetupService.Create(
                        new TradeSetupContext
                        {
                            EntryConfirmation = entryConfirmation,
                            AverageTrueRange = atr
                        });

                if (tradeSetup is null)
                {
                    continue;
                }

                tradeSetups.Add(key, tradeSetup);

                // -------------------------------------------------
                // 18. Calculate position size
                // -------------------------------------------------

                var positionSize =
                    _positionSizingService.Calculate(
                        tradeSetup,
                        _riskManagementOptions.InitialAccountEquity);

                positionSizes.Add(
                    key,
                    positionSize);

                // -------------------------------------------------
                // 19. Build historical risk context
                // -------------------------------------------------

                var setupDate =
                    tradeSetup.CreatedAt.Date;

                var tradesToday =
                    tradeRiskAssessments.Values.Count(x =>
                        x.IsApproved &&
                        x.PositionSize.Setup.CreatedAt.Date ==
                        setupDate);

                var activeTrades =
                    tradeOutcomeStates.Values.Count(x =>
                        x.Status == TradeOutcomeStatus.Active);

                var dailyRealizedR =
                    tradeOutcomeStates.Values
                        .Where(x =>
                            x.Status != TradeOutcomeStatus.Active &&
                            x.ClosedAt.HasValue &&
                            x.ClosedAt.Value.Date == setupDate)
                        .Sum(x =>
                            x.RealizedRiskRewardRatio ?? 0m);

                var riskContext =
                    new TradeRiskContext
                    {
                        PositionSize = positionSize,
                        TradesToday = tradesToday,
                        ActiveTrades = activeTrades,
                        DailyRealizedR = dailyRealizedR
                    };

                // -------------------------------------------------
                // 20. Evaluate risk rules
                // -------------------------------------------------

                var riskAssessment =
                    _riskManagementService.Evaluate(
                        riskContext);

                tradeRiskAssessments.Add(
                    key,
                    riskAssessment);

                // Approved risk creates an order. The outcome becomes active only after a future candle fills it.
                if (riskAssessment.IsApproved) tradeOrders.Add(key, _tradeExecutionService.CreateOrder(riskAssessment));
            }

            // -----------------------------------------------------
            // 21. Create isolated historical snapshots
            // -----------------------------------------------------

            var tradeOutcomes =
                tradeOutcomeStates.Values
                    .Select(x => x.CreateSnapshot())
                    .ToList();

            var positionSizeSnapshots =
                positionSizes.Values.ToList();

            var tradeRiskAssessmentSnapshots =
                tradeRiskAssessments.Values.ToList();

            // -----------------------------------------------------
            // 22. Save replay snapshot
            // -----------------------------------------------------

            results.Add(
                new FairValueGapQualificationSnapshot
                {
                    Time = fvgSnapshot.Time,
                    AverageTrueRange = atr,
                    MarketTrend = marketTrend,
                    Qualifications = qualifications,
                    Alignments = alignments,
                    Candidates = candidates,
                    Interactions = interactions,
                    EntryConfirmations = entryConfirmations,
                    TradeSetups = tradeSetups.Values.ToList(),
                    TradeOutcomes = tradeOutcomes,
                    PositionSizes = positionSizeSnapshots,
                    TradeRiskAssessments = tradeRiskAssessmentSnapshots,
                    TradeOrders = tradeOrders.Values.ToList(),
                    TradeExecutions = tradeExecutions.Values.ToList()
                });
        }

        return results;
    }
}