using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapReplayService : IFairValueGapReplayService
{
    private readonly IFairValueGapDetectionService _detectionService;

    private readonly IFairValueGapMitigationService
        _mitigationService;

    public FairValueGapReplayService(IFairValueGapDetectionService detectionService, IFairValueGapMitigationService mitigationService)
    {
        _detectionService = detectionService;
        _mitigationService = mitigationService;
    }

    public IReadOnlyCollection<FairValueGapSnapshot> Replay(IReadOnlyCollection<Candle> candles)
    {
        var closedCandles = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTime)
            .ToList();

        if (closedCandles.Count < 3)
        {
            return [];
        }

        var snapshots =
            new List<FairValueGapSnapshot>();

        var states = new Dictionary<Domain.ValueObjects.FairValueGapKey, FairValueGapState>();

        for (var i = 2; i < closedCandles.Count; i++)
        {
            var currentCandle = closedCandles[i];

            // 1. Update FVGs that already existed before
            //    the current candle closed.
            foreach (var state in states.Values)
            {
                _mitigationService.Update(state, currentCandle);
            }

            // 2. Only give the detector the current
            //    three-candle window.
            var window = new[]
            {
                closedCandles[i - 2],
                closedCandles[i - 1],
                currentCandle
            };

            var detectedGaps =
                _detectionService.Detect(window);

            // 3. Add FVGs newly confirmed by this candle.
            foreach (var gap in detectedGaps)
            {
                if (!states.ContainsKey(gap.Key))
                {
                    states.Add(gap.Key, new FairValueGapState(gap));
                }
            }

            // 4. Copy current state so future mitigation
            //    cannot mutate historical snapshots.
            var snapshotStates = states
                .Values
                .Select(x => x.CreateSnapshot())
                .ToList();

            snapshots.Add(
                new FairValueGapSnapshot
                {
                    Time = currentCandle.CloseTime,
                    Gaps = snapshotStates
                });
        }

        return snapshots;
    }
}