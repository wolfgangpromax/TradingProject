using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapReplayServiceTests
{
    private readonly FairValueGapReplayService _service =
        new(
            new FairValueGapDetectionService(),
            new FairValueGapMitigationService());

    [Fact]
    public void Replay_NewlyConfirmedGap_ShouldStartUnmitigated()
    {
        var candles = new[]
        {
            CreateCandle(
                new DateTime(2026, 9, 7, 10, 0, 0),
                95m, 100m, 94m, 98m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 5, 0),
                98m, 108m, 97m, 106m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 10, 0),
                106m, 110m, 105m, 109m)
        };

        var snapshots =
            _service.Replay(candles);

        var snapshot =
            Assert.Single(snapshots);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 15, 0),
            snapshot.Time);

        var state =
            Assert.Single(snapshot.Gaps);

        Assert.Equal(
            FairValueGapType.Bullish,
            state.Gap.Type);

        Assert.Equal(
            100m,
            state.Gap.LowerPrice);

        Assert.Equal(
            105m,
            state.Gap.UpperPrice);

        Assert.Equal(
            0m,
            state.FillPercentage);

        Assert.False(
            state.IsMitigated);

        Assert.Null(
            state.MitigatedAt);
    }

    private static Candle CreateCandle(
        DateTime openTime,
        decimal open,
        decimal high,
        decimal low,
        decimal close)
    {
        return new Candle
        {
            OpenTime = openTime,
            CloseTime = openTime.AddMinutes(5),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 100m,
            IsClosed = true
        };
    }

    [Fact]
    public void Replay_FutureCandleMitigatesGap_ShouldPreserveEarlierSnapshot()
    {
        var candles = new[]
        {
        // C1
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 0, 0),
            95m, 100m, 94m, 98m),

        // C2
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 5, 0),
            98m, 108m, 97m, 106m),

        // C3
        // Confirms bullish FVG:
        // 100 -> 105
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 10, 0),
            106m, 110m, 105m, 109m),

        // C4
        // Low reaches 103:
        // (105 - 103) / 5 = 40%
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 15, 0),
            109m, 111m, 103m, 106m)
    };

        var snapshots =
            _service.Replay(candles)
                .ToList();

        Assert.Equal(
            2,
            snapshots.Count);

        var confirmationSnapshot =
            snapshots[0];

        var mitigationSnapshot =
            snapshots[1];

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 15, 0),
            confirmationSnapshot.Time);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 20, 0),
            mitigationSnapshot.Time);

        var originalState =
            Assert.Single(
                confirmationSnapshot.Gaps);

        Assert.Equal(
            0m,
            originalState.FillPercentage);

        Assert.False(
            originalState.IsMitigated);

        var updatedGap =
            mitigationSnapshot.Gaps
                .Single(x =>
                    x.Gap.Key ==
                    originalState.Gap.Key);

        Assert.Equal(
            40m,
            updatedGap.FillPercentage);

        Assert.False(
            updatedGap.IsMitigated);

        // Most important regression check:
        // C4 must not mutate the historical C3 snapshot.
        Assert.Equal(
            0m,
            originalState.FillPercentage);

        Assert.False(
            originalState.IsMitigated);
    }

    [Fact]
    public void Replay_FutureCandleFullyMitigatesGap_ShouldSetMitigatedAt()
    {
        var candles = new[]
        {
        // C1
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 0, 0),
            95m, 100m, 94m, 98m),

        // C2
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 5, 0),
            98m, 108m, 97m, 106m),

        // C3
        // Confirms bullish FVG: 100 -> 105
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 10, 0),
            106m, 110m, 105m, 109m),

        // C4
        // Low = 103 -> 40% filled
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 15, 0),
            109m, 111m, 103m, 106m),

        // C5
        // Low reaches below LowerPrice (100)
        // -> 100% filled
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 20, 0),
            106m, 107m, 99m, 101m)
    };

        var snapshots =
            _service.Replay(candles)
                .ToList();

        Assert.Equal(
            3,
            snapshots.Count);

        var confirmationSnapshot =
            snapshots[0];

        var partialSnapshot =
            snapshots[1];

        var fullSnapshot =
            snapshots[2];

        var originalGap =
            Assert.Single(
                confirmationSnapshot.Gaps);

        var gapKey =
            originalGap.Gap.Key;

        var partialState =
            partialSnapshot.Gaps
                .Single(x =>
                    x.Gap.Key == gapKey);

        var fullState =
            fullSnapshot.Gaps
                .Single(x =>
                    x.Gap.Key == gapKey);

        Assert.Equal(
            0m,
            originalGap.FillPercentage);

        Assert.Equal(
            40m,
            partialState.FillPercentage);

        Assert.Equal(
            100m,
            fullState.FillPercentage);

        Assert.True(
            fullState.IsMitigated);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 25, 0),
            fullState.MitigatedAt);

        // Historical snapshots remain unchanged.
        Assert.False(
            originalGap.IsMitigated);

        Assert.Null(
            originalGap.MitigatedAt);

        Assert.False(
            partialState.IsMitigated);

        Assert.Null(
            partialState.MitigatedAt);
    }
}