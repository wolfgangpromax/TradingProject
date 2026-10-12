using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class CandleContinuityServiceTests
{
    [Fact]
    public void FindGaps_WhenOneCandleIsMissing_ReturnsGap()
    {
        // Arrange
        var service = new CandleContinuityService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0),
            CreateCandle(10, 5),
            CreateCandle(10, 10),

            // 10:15 intentionally missing

            CreateCandle(10, 20),
            CreateCandle(10, 25)
        };

        // Act
        var gaps = service.FindGaps(candles,Timeframe.FiveMinutes);

        // Assert
        var gap = Assert.Single(gaps);

        Assert.Equal(new DateTime(2026, 9, 6, 10, 15, 0), gap.ExpectedOpenTime);
        Assert.Equal(new DateTime(2026, 9, 6, 10, 20, 0), gap.NextAvailableOpenTime);
        Assert.Equal(1, gap.MissingCandleCount);
    }

    private static Candle CreateCandle(int hour, int minute)
    {
        return new Candle
        {
            OpenTime = new DateTime(2026, 9, 6, hour, minute, 0)
        };
    }

    [Fact]
    public void FindGaps_WhenCandlesAreContinuous_ReturnsNoGaps()
    {
        // Arrange
        var service = new CandleContinuityService();

        var candles = new List<Candle>
    {
        CreateCandle(10, 0),
        CreateCandle(10, 5),
        CreateCandle(10, 10),
        CreateCandle(10, 15),
        CreateCandle(10, 20)
    };

        // Act
        var gaps = service.FindGaps(candles, Timeframe.FiveMinutes);

        // Assert
        Assert.Empty(gaps);
    }

    [Fact]
    public void FindGaps_WhenMultipleConsecutiveCandlesAreMissing_ReturnsCorrectCount()
    {
        // Arrange
        var service = new CandleContinuityService();

        var candles = new List<Candle>
    {
        CreateCandle(10, 0),
        CreateCandle(10, 5),
        CreateCandle(10, 10),

        // 10:15, 10:20 and 10:25 intentionally missing

        CreateCandle(10, 30)
    };

        // Act
        var gaps = service.FindGaps(candles, Timeframe.FiveMinutes);

        // Assert
        var gap = Assert.Single(gaps);

        Assert.Equal(new DateTime(2026, 9, 6, 10, 15, 0), gap.ExpectedOpenTime);
        Assert.Equal( new DateTime(2026, 9, 6, 10, 30, 0), gap.NextAvailableOpenTime);
        Assert.Equal(3, gap.MissingCandleCount);
    }

    [Fact]
    public void FindGaps_WhenMultipleSeparateGapsExist_ReturnsAllGaps()
    {
        // Arrange
        var service = new CandleContinuityService();

        var candles = new List<Candle>
    {
        CreateCandle(10, 0),
        CreateCandle(10, 5),
        CreateCandle(10, 10),

        // 10:15 missing

        CreateCandle(10, 20),
        CreateCandle(10, 25),

        // 10:30 missing

        CreateCandle(10, 35)
    };

        // Act
        var gaps = service
            .FindGaps(
                candles,
                Timeframe.FiveMinutes)
            .OrderBy(x => x.ExpectedOpenTime)
            .ToList();

        // Assert
        Assert.Equal(2, gaps.Count);
        Assert.Equal( new DateTime(2026, 9, 6, 10, 15, 0), gaps[0].ExpectedOpenTime);
        Assert.Equal(1, gaps[0].MissingCandleCount);
        Assert.Equal( new DateTime(2026, 9, 6, 10, 30, 0), gaps[1].ExpectedOpenTime);
        Assert.Equal(1, gaps[1].MissingCandleCount);
    }
}