using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapDetectionServiceTests
{
    private readonly FairValueGapDetectionService _service = new();

    [Fact]
    public void Detect_BullishGap_ShouldReturnBullishFairValueGap()
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

        var result = _service.Detect(candles);

        var gap = Assert.Single(result);

        Assert.Equal(FairValueGapType.Bullish, gap.Type);
        Assert.Equal(100m, gap.LowerPrice);
        Assert.Equal(105m, gap.UpperPrice);
        Assert.Equal(5m, gap.GapSize);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 0, 0),
            gap.StartTime);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 15, 0),
            gap.ConfirmedAt);
    }

    [Fact]
    public void Detect_BearishGap_ShouldReturnBearishFairValueGap()
    {
        var candles = new[]
        {
            CreateCandle(
                new DateTime(2026, 9, 7, 10, 0, 0),
                108m, 110m, 105m, 106m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 5, 0),
                106m, 107m, 97m, 98m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 10, 0),
                98m, 100m, 95m, 96m)
        };

        var result = _service.Detect(candles);

        var gap = Assert.Single(result);

        Assert.Equal(FairValueGapType.Bearish, gap.Type);
        Assert.Equal(100m, gap.LowerPrice);
        Assert.Equal(105m, gap.UpperPrice);
        Assert.Equal(5m, gap.GapSize);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 0, 0),
            gap.StartTime);

        Assert.Equal(
            new DateTime(2026, 9, 7, 10, 15, 0),
            gap.ConfirmedAt);
    }

    [Fact]
    public void Detect_NoGap_ShouldReturnEmpty()
    {
        var candles = new[]
        {
            CreateCandle(
                new DateTime(2026, 9, 7, 10, 0, 0),
                95m, 100m, 94m, 98m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 5, 0),
                98m, 103m, 97m, 101m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 10, 0),
                101m, 104m, 99m, 102m)
        };

        var result = _service.Detect(candles);

        Assert.Empty(result);
    }

    [Fact]
    public void Detect_OnlyTwoCandles_ShouldReturnEmpty()
    {
        var candles = new[]
        {
            CreateCandle(
                new DateTime(2026, 9, 7, 10, 0, 0),
                95m, 100m, 94m, 98m),

            CreateCandle(
                new DateTime(2026, 9, 7, 10, 5, 0),
                98m, 108m, 97m, 106m)
        };

        var result = _service.Detect(candles);

        Assert.Empty(result);
    }

    private static Candle CreateCandle(DateTime openTime, decimal open, decimal high, decimal low, decimal close)
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
    public void Detect_UnclosedThirdCandle_ShouldReturnEmpty()
    {
        var candles = new[]
        {
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 0, 0),
            95m, 100m, 94m, 98m),

        CreateCandle(
            new DateTime(2026, 9, 7, 10, 5, 0),
            98m, 108m, 97m, 106m),

        new Candle
        {
            OpenTime = new DateTime(2026, 9, 7, 10, 10, 0),
            CloseTime = new DateTime(2026, 9, 7, 10, 15, 0),
            Open = 106m,
            High = 110m,
            Low = 105m,
            Close = 109m,
            Volume = 100m,
            IsClosed = false
        }
    };

        var result = _service.Detect(candles);

        Assert.Empty(result);
    }

    [Fact]
    public void Detect_UnorderedCandles_ShouldDetectGapChronologically()
    {
        var candle1 = CreateCandle(
            new DateTime(2026, 9, 7, 10, 0, 0),
            95m, 100m, 94m, 98m);

        var candle2 = CreateCandle(
            new DateTime(2026, 9, 7, 10, 5, 0),
            98m, 108m, 97m, 106m);

        var candle3 = CreateCandle(
            new DateTime(2026, 9, 7, 10, 10, 0),
            106m, 110m, 105m, 109m);

        var candles = new[]
        {
        candle3,
        candle1,
        candle2
    };

        var result = _service.Detect(candles);

        var gap = Assert.Single(result);

        Assert.Equal(
            FairValueGapType.Bullish,
            gap.Type);

        Assert.Equal(100m, gap.LowerPrice);
        Assert.Equal(105m, gap.UpperPrice);
    }

    [Fact]
    public void Detect_TouchingBoundary_ShouldNotCreateGap()
    {
        var candles = new[]
        {
        CreateCandle(
            new DateTime(2026, 9, 7, 10, 0, 0),
            95m, 100m, 94m, 98m),

        CreateCandle(
            new DateTime(2026, 9, 7, 10, 5, 0),
            98m, 105m, 97m, 103m),

        CreateCandle(
            new DateTime(2026, 9, 7, 10, 10, 0),
            103m, 106m, 100m, 105m)
    };

        var result = _service.Detect(candles);

        Assert.Empty(result);
    }

    [Fact]
    public void Detect_MultipleGaps_ShouldReturnAllGaps()
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
            106m, 110m, 105m, 109m),

        CreateCandle(
            new DateTime(2026, 9, 7, 10, 15, 0),
            109m, 115m, 112m, 114m)
    };

        var result = _service.Detect(candles);

        Assert.Equal(2, result.Count);

        var gaps = result.ToList();

        // Candle 1 → Candle 3
        Assert.Equal(
            FairValueGapType.Bullish,
            gaps[0].Type);

        Assert.Equal(100m, gaps[0].LowerPrice);
        Assert.Equal(105m, gaps[0].UpperPrice);
        Assert.Equal(5m, gaps[0].GapSize);

        // Candle 2 → Candle 4
        Assert.Equal(
            FairValueGapType.Bullish,
            gaps[1].Type);

        Assert.Equal(108m, gaps[1].LowerPrice);
        Assert.Equal(112m, gaps[1].UpperPrice);
        Assert.Equal(4m, gaps[1].GapSize);
    }
}