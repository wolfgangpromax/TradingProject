using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class AverageTrueRangeServiceTests
{
    private readonly AverageTrueRangeService _service = new();

    [Fact]
    public void Calculate_WithValidCandles_ReturnsExpectedAtr()
    {
        // Arrange
        var startTime = new DateTime(
            2026, 9, 7, 10, 0, 0,
            DateTimeKind.Utc);

        var candles = new List<Candle>
        {
            CreateCandle(
                startTime,
                high: 100m,
                low: 90m,
                close: 95m),

            CreateCandle(
                startTime.AddMinutes(5),
                high: 105m,
                low: 94m,
                close: 100m),

            CreateCandle(
                startTime.AddMinutes(10),
                high: 108m,
                low: 98m,
                close: 102m),

            CreateCandle(
                startTime.AddMinutes(15),
                high: 110m,
                low: 100m,
                close: 105m)
        };

        // True ranges for the last 3 candles:
        //
        // Candle 2:
        // max(105 - 94, |105 - 95|, |94 - 95|)
        // = max(11, 10, 1)
        // = 11
        //
        // Candle 3:
        // max(108 - 98, |108 - 100|, |98 - 100|)
        // = 10
        //
        // Candle 4:
        // max(110 - 100, |110 - 102|, |100 - 102|)
        // = 10
        //
        // ATR = (11 + 10 + 10) / 3
        //     = 10.333333...

        // Act
        var result = _service.Calculate(
            candles,
            period: 3);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            31m / 3m,
            result.Value);
    }

    [Fact]
    public void Calculate_WithInsufficientCandles_ReturnsNull()
    {
        // Arrange
        var startTime = new DateTime(
            2026, 9, 7, 10, 0, 0,
            DateTimeKind.Utc);

        var candles = new List<Candle>
        {
            CreateCandle(
                startTime,
                100m,
                90m,
                95m),

            CreateCandle(
                startTime.AddMinutes(5),
                105m,
                94m,
                100m),

            CreateCandle(
                startTime.AddMinutes(10),
                108m,
                98m,
                102m)
        };

        // period 3 requires 4 closed candles
        // because each True Range requires
        // the previous candle's close.

        // Act
        var result = _service.Calculate(
            candles,
            period: 3);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Calculate_IgnoresUnclosedCandles()
    {
        // Arrange
        var startTime = new DateTime(
            2026, 9, 7, 10, 0, 0,
            DateTimeKind.Utc);

        var candles = new List<Candle>
        {
            CreateCandle(
                startTime,
                100m,
                90m,
                95m),

            CreateCandle(
                startTime.AddMinutes(5),
                105m,
                94m,
                100m),

            CreateCandle(
                startTime.AddMinutes(10),
                108m,
                98m,
                102m),

            CreateCandle(
                startTime.AddMinutes(15),
                500m,
                1m,
                250m,
                isClosed: false)
        };

        // Only 3 candles are closed.
        // period 3 requires 4 closed candles.

        // Act
        var result = _service.Calculate(
            candles,
            period: 3);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Calculate_WithInvalidPeriod_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var candles = new List<Candle>();

        // Act
        Action action = () =>
        {
            _service.Calculate(candles, period: 0);
        };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }

    private static Candle CreateCandle(
        DateTime openTime,
        decimal high,
        decimal low,
        decimal close,
        bool isClosed = true)
    {
        return new Candle
        {
            OpenTime = openTime,
            CloseTime = openTime.AddMinutes(5),
            Open = close,
            High = high,
            Low = low,
            Close = close,
            Volume = 100m,
            IsClosed = isClosed
        };
    }
}