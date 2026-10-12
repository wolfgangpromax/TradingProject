using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class MarketStructureAnalysisServiceTests
{
    [Fact]
    public void Analyze_WithValidClosedCandles_ReturnsAnalysis()
    {
        var service = CreateService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),
            CreateCandle(10, 10, 120, 95),
            CreateCandle(10, 15, 108, 93),
            CreateCandle(10, 20, 103, 91)
        };

        var result = service.Analyze(candles, swingStrength: 2);

        Assert.NotNull(result);
        Assert.Single(result.Swings);

        var swing = result.Swings.Single();

        Assert.Equal(SwingType.High, swing.Type);
        Assert.Equal(120m, swing.Price);
        Assert.Equal( Time(10, 25), result.AnalysisTime);
    }

    [Fact]
    public void Analyze_IgnoresOpenCandles()
    {
        var service = CreateService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),
            CreateCandle(10, 10, 120, 95),
            CreateCandle(10, 15, 108, 93),
            CreateCandle(10, 20, 103, 91),

            new Candle
            {
                OpenTime = Time(10, 25),
                CloseTime = Time(10, 30),
                High = 999,
                Low = 1,
                Close = 500,
                IsClosed = false
            }
        };

        var result = service.Analyze(candles, swingStrength: 2);

        Assert.Equal(Time(10, 25), result.AnalysisTime);
    }

    private static MarketStructureAnalysisService
        CreateService()
    {
        return new MarketStructureAnalysisService(
            new SwingDetectionService(),
            new MarketStructureService(),
            new MarketTrendService(),
            new StructureBreakDetectionService());
    }

    private static Candle CreateCandle(int hour, int minute, decimal high, decimal low)
    {
        var time = Time(hour, minute);

        return new Candle
        {
            OpenTime = time,
            CloseTime = time.AddMinutes(5),
            Open = low,
            High = high,
            Low = low,
            Close = high,
            Volume = 1,
            IsClosed = true
        };
    }

    private static DateTime Time(int hour, int minute)
    {
        return new DateTime(2026, 9, 6, hour, minute, 0);
    }
}