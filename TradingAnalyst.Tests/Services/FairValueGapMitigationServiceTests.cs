using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapMitigationServiceTests
{
    private readonly FairValueGapMitigationService _service = new();

    [Fact]
    public void Update_BullishGapPartialRetracement_ShouldUpdateFill()
    {
        var state = CreateBullishState();

        var candle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 20, 0),
            106m,
            107m,
            103m,
            104m);

        _service.Update(state, candle);

        Assert.Equal(40m, state.FillPercentage);
        Assert.False(state.IsMitigated);
    }

    [Fact]
    public void Update_BullishGapFullRetracement_ShouldMarkMitigated()
    {
        var state = CreateBullishState();

        var candle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 20, 0),
            106m,
            107m,
            99m,
            101m);

        _service.Update(state, candle);

        Assert.Equal(100m, state.FillPercentage);
        Assert.True(state.IsMitigated);
        Assert.Equal(
            candle.CloseTime,
            state.MitigatedAt);
    }

    [Fact]
    public void Update_BearishGapPartialRetracement_ShouldUpdateFill()
    {
        var state = CreateBearishState();

        var candle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 20, 0),
            99m,
            102m,
            98m,
            101m);

        _service.Update(state, candle);

        Assert.Equal(40m, state.FillPercentage);
        Assert.False(state.IsMitigated);
    }

    [Fact]
    public void Update_BearishGapFullRetracement_ShouldMarkMitigated()
    {
        var state = CreateBearishState();

        var candle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 20, 0),
            99m,
            106m,
            98m,
            104m);

        _service.Update(state, candle);

        Assert.Equal(100m, state.FillPercentage);
        Assert.True(state.IsMitigated);
        Assert.Equal(
            candle.CloseTime,
            state.MitigatedAt);
    }

    private static FairValueGapState CreateBullishState()
    {
        return new FairValueGapState(
            new FairValueGap
            {
                Type = FairValueGapType.Bullish,
                StartTime =
                    new DateTime(2026, 9, 7, 10, 0, 0),
                ConfirmedAt =
                    new DateTime(2026, 9, 7, 10, 15, 0),
                LowerPrice = 100m,
                UpperPrice = 105m,
                GapSize = 5m
            });
    }

    private static FairValueGapState CreateBearishState()
    {
        return new FairValueGapState(
            new FairValueGap
            {
                Type = FairValueGapType.Bearish,
                StartTime =
                    new DateTime(2026, 9, 7, 10, 0, 0),
                ConfirmedAt =
                    new DateTime(2026, 9, 7, 10, 15, 0),
                LowerPrice = 100m,
                UpperPrice = 105m,
                GapSize = 5m
            });
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
    public void Update_ConfirmationCandle_ShouldNotMitigateGap()
    {
        var state = CreateBullishState();

        // Gap ConfirmedAt = 10:15.
        // Even though this candle penetrates the gap,
        // it cannot mitigate a gap that does not exist until this close.
        var candle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 10, 0),
            106m,
            110m,
            101m,
            109m);

        _service.Update(state, candle);

        Assert.Equal(0m, state.FillPercentage);
        Assert.False(state.IsMitigated);
        Assert.Null(state.MitigatedAt);
    }

    [Fact]
    public void Update_UnclosedCandle_ShouldNotMitigateGap()
    {
        var state = CreateBullishState();

        var candle = new Candle
        {
            OpenTime =
                new DateTime(2026, 9, 7, 10, 20, 0),

            CloseTime =
                new DateTime(2026, 9, 7, 10, 25, 0),

            Open = 106m,
            High = 107m,
            Low = 100m,
            Close = 101m,
            Volume = 100m,

            IsClosed = false
        };

        _service.Update(state, candle);

        Assert.Equal(0m, state.FillPercentage);
        Assert.False(state.IsMitigated);
        Assert.Null(state.MitigatedAt);
    }

    [Fact]
    public void Update_AlreadyMitigatedGap_ShouldPreserveOriginalMitigationTime()
    {
        var state = CreateBullishState();

        var firstCandle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 20, 0),
            106m,
            107m,
            99m,
            101m);

        var laterCandle = CreateCandle(
            new DateTime(2026, 9, 7, 10, 25, 0),
            101m,
            103m,
            98m,
            99m);

        _service.Update(
            state,
            firstCandle);

        _service.Update(
            state,
            laterCandle);

        Assert.True(state.IsMitigated);
        Assert.Equal(100m, state.FillPercentage);

        Assert.Equal(
            firstCandle.CloseTime,
            state.MitigatedAt);
    }
}