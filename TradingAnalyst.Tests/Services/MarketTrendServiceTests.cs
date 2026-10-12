using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class MarketTrendServiceTests
{
    [Fact]
    public void DetermineTrend_WhenHHAndHL_ReturnsBullish()
    {
        var service = new MarketTrendService();

        var points = new List<MarketStructurePoint>
        {
            CreatePoint(10, 0, SwingType.High, MarketStructureType.HigherHigh),
            CreatePoint(10, 20, SwingType.Low, MarketStructureType.HigherLow)
        };

        var result = service.DetermineTrend(points);

        Assert.Equal(MarketTrend.Bullish, result);
    }

    [Fact]
    public void DetermineTrend_WhenLHAndLL_ReturnsBearish()
    {
        var service = new MarketTrendService();

        var points = new List<MarketStructurePoint>
        {
            CreatePoint(10, 0, SwingType.High, MarketStructureType.LowerHigh),
            CreatePoint(10, 20, SwingType.Low, MarketStructureType.LowerLow)
        };

        var result = service.DetermineTrend(points);

        Assert.Equal(MarketTrend.Bearish, result);
    }

    [Fact]
    public void DetermineTrend_WhenStructureIsMixed_ReturnsUnknown()
    {
        var service = new MarketTrendService();

        var points = new List<MarketStructurePoint>
        {
            CreatePoint(10, 0, SwingType.High, MarketStructureType.HigherHigh),
            CreatePoint(10, 20, SwingType.Low, MarketStructureType.LowerLow)
        };

        var result = service.DetermineTrend(points);

        Assert.Equal(MarketTrend.Unknown, result);
    }

    [Fact]
    public void DetermineTrend_WhenInsufficientStructure_ReturnsUnknown()
    {
        var service = new MarketTrendService();

        var points = new List<MarketStructurePoint>
        {
            CreatePoint(10, 0, SwingType.High, MarketStructureType.HigherHigh)
        };

        var result = service.DetermineTrend(points);

        Assert.Equal(MarketTrend.Unknown, result);
    }

    private static MarketStructurePoint CreatePoint(int hour, int minute, SwingType swingType, MarketStructureType structureType)
    {
        var time = new DateTime(2026, 9, 6, hour, minute, 0);

        return new MarketStructurePoint
        {
            OpenTime = time,
            ConfirmedAt = time.AddMinutes(5),
            SwingType = swingType,
            StructureType = structureType
        };
    }
}