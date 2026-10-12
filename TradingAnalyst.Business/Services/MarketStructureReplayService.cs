using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Domain.ValueObjects;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class MarketStructureReplayService : IMarketStructureReplayService
{
    private readonly ISwingDetectionService _swingDetectionService;
    private readonly IMarketStructureService _marketStructureService;
    private readonly IMarketTrendService _marketTrendService;
    private readonly IStructureBreakDetectionService _structureBreakDetectionService;
    public MarketStructureReplayService(ISwingDetectionService swingDetectionService, IMarketStructureService marketStructureService,
        IMarketTrendService marketTrendService, IStructureBreakDetectionService structureBreakDetectionService)
    {
        _swingDetectionService = swingDetectionService;
        _marketStructureService = marketStructureService;
        _marketTrendService = marketTrendService;
        _structureBreakDetectionService = structureBreakDetectionService;
    }

    public IReadOnlyCollection<MarketStructureSnapshot> Replay(
    IReadOnlyCollection<Candle> candles,
    int swingStrength = 2)
    {
        if (swingStrength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(swingStrength), "Swing strength must be at least 1.");
        }

        var closedCandles = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTime)
            .ToList();

        if (closedCandles.Count == 0)
        {
            return [];
        }

        var snapshots = new List<MarketStructureSnapshot>();

        var state = new MarketStructureState();

        var minimumCandles = (swingStrength * 2) + 1;

        for (var i = minimumCandles - 1; i < closedCandles.Count; i++)
        {
            var currentCandle = closedCandles[i];

            var currentTime = currentCandle.CloseTime;

            // STEP 1:
            // Check the current candle against the structure
            // that existed BEFORE processing this candle.
            var structureBreak = _structureBreakDetectionService.DetectBreak(currentCandle, state);

            var structureBreaks =
                structureBreak is null
                    ? new List<StructureBreak>()
                    : new List<StructureBreak>
                    {
                    structureBreak
                    };

            // STEP 2:
            // Apply the detected break to our existing state.
            if (structureBreak is not null)
            {
                var brokenPoint =
                    structureBreak.Direction ==
                    BreakDirection.Bullish
                        ? state.ProtectedHigh
                        : state.ProtectedLow;

                if (brokenPoint is not null)
                {
                    state.ConsumeLevel(brokenPoint);
                }

                if (structureBreak.Type ==
                    StructureBreakType.ChangeOfCharacter)
                {
                    state.EnterTransition(structureBreak.Direction);
                }
                else
                {
                    state.SetTrend(structureBreak.TrendAfterBreak);
                }
            }

            // STEP 3:
            // Only AFTER checking for a break do we incorporate
            // information confirmed by the current candle.
            var availableCandles = closedCandles
                .Take(i + 1)
                .ToList();

            var swings =
                _swingDetectionService.DetectSwings(
                    availableCandles,
                    swingStrength);

            var availableSwings = swings
                .Where(x =>
                    x.ConfirmedAt <= currentTime)
                .ToList();

            var structurePoints =
                _marketStructureService.Classify(
                    availableSwings);

            var availableStructurePoints =
                structurePoints
                    .Where(x =>
                        x.ConfirmedAt <= currentTime)
                    .ToList();

            var calculatedTrend =
                _marketTrendService.DetermineTrend(
                    availableStructurePoints);

            // STEP 4:
            // Establish the initial trend.
            if (state.Trend == MarketTrend.Unknown &&
                calculatedTrend != MarketTrend.Unknown)
            {
                state.SetTrend(
                    calculatedTrend);
            }

            // STEP 5:
            // After CHoCH we are in Transition.
            // Confirmed structure can establish the new trend.
            else if (state.Trend == MarketTrend.Transition)
            {
                state.TryConfirmTransition(calculatedTrend);
            }

            // STEP 6:
            // These levels are updated AFTER the current candle
            // was checked for a break.
            // They will therefore be used by the NEXT candle.
            var latestHigh = availableStructurePoints
                .Where(x =>
                    x.SwingType == SwingType.High)
                .OrderByDescending(x =>
                    x.ConfirmedAt)
                .FirstOrDefault();

            var latestLow = availableStructurePoints
                .Where(x =>
                    x.SwingType == SwingType.Low)
                .OrderByDescending(x =>
                    x.ConfirmedAt)
                .FirstOrDefault();

            state.SetProtectedLevels(
                latestHigh,
                latestLow);

            // STEP 7:
            // Save the state as it exists at the end
            // of this candle.
            snapshots.Add(
                new MarketStructureSnapshot
                {
                    Time = currentTime,
                    Trend = state.Trend,
                    Swings = availableSwings,
                    StructurePoints =
                        availableStructurePoints,
                    StructureBreaks =
                        structureBreaks
                });
        }

        return snapshots;
    }
}