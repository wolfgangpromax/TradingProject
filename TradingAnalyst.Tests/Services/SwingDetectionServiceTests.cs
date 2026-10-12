using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class SwingDetectionServiceTests
{
    [Fact]
    public void DetectSwings_WhenSwingHighExists_ReturnsSwingHigh()
    {
        // Arrange
        var service = new SwingDetectionService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),

            // Candidate swing high
            CreateCandle(10, 10, 120, 95),

            CreateCandle(10, 15, 108, 93),
            CreateCandle(10, 20, 103, 91)
        };

        // Act
        var swings = service
            .DetectSwings(candles, strength: 2)
            .ToList();

        // Assert
        var swing = Assert.Single(swings);

        Assert.Equal(SwingType.High, swing.Type);
        Assert.Equal(120m, swing.Price);
        Assert.Equal(
            new DateTime(2026, 9, 6, 10, 10, 0),
            swing.OpenTime);
    }

    [Fact]
    public void DetectSwings_WhenSwingLowExists_ReturnsSwingLow()
    {
        // Arrange
        var service = new SwingDetectionService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 110, 100),
            CreateCandle(10, 5, 108, 95),

            // Candidate swing low
            CreateCandle(10, 10, 105, 80),

            CreateCandle(10, 15, 109, 94),
            CreateCandle(10, 20, 112, 98)
        };

        // Act
        var swings = service
            .DetectSwings(candles, strength: 2)
            .ToList();

        // Assert
        var swing = Assert.Single(swings);

        Assert.Equal(SwingType.Low, swing.Type);
        Assert.Equal(80m, swing.Price);
        Assert.Equal(
            new DateTime(2026, 9, 6, 10, 10, 0),
            swing.OpenTime);
    }

    [Fact]
    public void DetectSwings_WhenNoSwingExists_ReturnsEmpty()
    {
        // Arrange
        var service = new SwingDetectionService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),
            CreateCandle(10, 10, 110, 94),
            CreateCandle(10, 15, 115, 96),
            CreateCandle(10, 20, 120, 98)
        };

        // Act
        var swings = service.DetectSwings(
            candles,
            strength: 2);

        // Assert
        Assert.Empty(swings);
    }

    [Fact]
    public void DetectSwings_SetsCorrectConfirmationTime()
    {
        // Arrange
        var service = new SwingDetectionService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),

            // Swing occurs here.
            CreateCandle(10, 10, 120, 95),

            CreateCandle(10, 15, 108, 93),

            // Second confirmation candle.
            CreateCandle(10, 20, 103, 91)
        };

        // Act
        var swings = service
            .DetectSwings(candles, strength: 2)
            .ToList();

        // Assert
        var swing = Assert.Single(swings);

        Assert.Equal( new DateTime(2026, 9, 6, 10, 25, 0), swing.ConfirmedAt);
    }

    private static Candle CreateCandle(int hour, int minute, decimal high, decimal low)
    {
        var openTime = new DateTime(2026, 9, 6, hour, minute, 0);

        return new Candle
        {
            OpenTime = openTime,
            CloseTime = openTime.AddMinutes(5),

            High = high,
            Low = low,

            // These aren't relevant to swing detection yet,
            // but keep the candle valid.
            Open = low,
            Close = high,
            Volume = 1,
            IsClosed = true
        };
    }
}