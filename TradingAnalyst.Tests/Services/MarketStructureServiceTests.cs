using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class MarketStructureServiceTests
{
    [Fact]
    public void Classify_WhenHighIncreases_ReturnsHigherHigh()
    {
        var service = new MarketStructureService();

        var swings = new List<SwingPoint>
        {
            CreateSwing(10, 0, SwingType.High, 100),
            CreateSwing(10, 20, SwingType.High, 110)
        };

        var result = service.Classify(swings);

        var point = Assert.Single(result);

        Assert.Equal(MarketStructureType.HigherHigh, point.StructureType);

        Assert.Equal(110m, point.Price);
    }

    [Fact]
    public void Classify_WhenLowIncreases_ReturnsHigherLow()
    {
        var service = new MarketStructureService();

        var swings = new List<SwingPoint>
        {
            CreateSwing(10, 0, SwingType.Low, 90),
            CreateSwing(10, 20, SwingType.Low, 95)
        };

        var result = service.Classify(swings);

        var point = Assert.Single(result);

        Assert.Equal( MarketStructureType.HigherLow, point.StructureType);

        Assert.Equal(95m, point.Price);
    }

    [Fact]
    public void Classify_WhenHighDecreases_ReturnsLowerHigh()
    {
        var service = new MarketStructureService();

        var swings = new List<SwingPoint>
        {
            CreateSwing(10, 0, SwingType.High, 110),
            CreateSwing(10, 20, SwingType.High, 105)
        };

        var result = service.Classify(swings);

        var point = Assert.Single(result);

        Assert.Equal(MarketStructureType.LowerHigh, point.StructureType);
    }

    [Fact]
    public void Classify_WhenLowDecreases_ReturnsLowerLow()
    {
        var service = new MarketStructureService();

        var swings = new List<SwingPoint>
        {
            CreateSwing(10, 0, SwingType.Low, 90),
            CreateSwing(10, 20, SwingType.Low, 85)
        };

        var result = service.Classify(swings);

        var point = Assert.Single(result);

        Assert.Equal(MarketStructureType.LowerLow, point.StructureType);
    }

    [Fact]
    public void Classify_PreservesConfirmationTime()
    {
        var service = new MarketStructureService();

        var swings = new List<SwingPoint>
        {
            CreateSwing(10, 0, SwingType.High, 100),
            CreateSwing(10, 20, SwingType.High, 110)
        };

        var result = service.Classify(swings);

        var point = Assert.Single(result);

        Assert.Equal(new DateTime(2026, 9, 6, 10, 25, 0), point.ConfirmedAt);
    }

    private static SwingPoint CreateSwing(int hour, int minute, SwingType type, decimal price)
    {
        var openTime = new DateTime( 2026, 9, 6, hour, minute, 0);

        return new SwingPoint
        {
            OpenTime = openTime,
            ConfirmedAt = openTime.AddMinutes(5),
            Type = type,
            Price = price
        };
    }

    [Fact]
    public void Classify_WhenHighsAreEqual_ReturnsEqualHigh()
    {
        var service = new MarketStructureService();
        var swings = new List<SwingPoint>
    {
        CreateSwing(10, 0, SwingType.High, 100),
        CreateSwing(10, 20, SwingType.High, 100)
    };

        var result = service.Classify(swings);
        var point = Assert.Single(result);

        Assert.Equal(MarketStructureType.EqualHigh, point.StructureType);
        Assert.Equal(100m, point.Price);
    }

    [Fact]
    public void Classify_WhenLowsAreEqual_ReturnsEqualLow()
    {
        var service = new MarketStructureService();
        var swings = new List<SwingPoint>
    {
        CreateSwing(10, 0, SwingType.Low, 90),
        CreateSwing(10, 20, SwingType.Low, 90)
    };

        var result = service.Classify(swings);
        var point = Assert.Single(result);

        Assert.Equal(MarketStructureType.EqualLow, point.StructureType);
        Assert.Equal(90m, point.Price);
    }
}