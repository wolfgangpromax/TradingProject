using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class StructureBreakDetectionServiceTests
{
    [Fact]
    public void DetectBreaks_BullishTrendCloseAboveHigh_ReturnsBullishBos()
    {
        var service = new StructureBreakDetectionService();

        var structure = CreateBullishStructure();

        var candles = new List<Candle>
        {
            CreateCandle(10, 30, 111, 104, 108),
            CreateCandle(10, 35, 116, 107, 115)
        };

        var result = service.DetectBreaks(candles, structure, MarketTrend.Bullish);

        var structureBreak = Assert.Single(result);

        Assert.Equal(StructureBreakType.BreakOfStructure, structureBreak.Type);

        Assert.Equal( BreakDirection.Bullish, structureBreak.Direction);

        Assert.Equal(110m, structureBreak.BrokenPrice);
        Assert.Equal(115m, structureBreak.BreakClosePrice);
    }

    [Fact]
    public void DetectBreaks_BullishTrendCloseBelowLow_ReturnsBearishChoch()
    {
        var service = new StructureBreakDetectionService();

        var structure = CreateBullishStructure();

        var candles = new List<Candle>
        {
            CreateCandle(10, 30, 108, 96, 100),
            CreateCandle(10, 35, 101, 90, 94)
        };

        var result = service.DetectBreaks(candles, structure, MarketTrend.Bullish);

        var structureBreak = Assert.Single(result);

        Assert.Equal(StructureBreakType.ChangeOfCharacter, structureBreak.Type);
        Assert.Equal(BreakDirection.Bearish, structureBreak.Direction);

        Assert.Equal(95m, structureBreak.BrokenPrice);
        Assert.Equal(94m, structureBreak.BreakClosePrice);
    }

    [Fact]
    public void DetectBreaks_BearishTrendCloseBelowLow_ReturnsBearishBos()
    {
        var service = new StructureBreakDetectionService();

        var structure = CreateBearishStructure();

        var candles = new List<Candle>
        {
            CreateCandle(10, 30, 104, 91, 95),
            CreateCandle(10, 35, 96, 84, 85)
        };

        var result = service.DetectBreaks(candles, structure, MarketTrend.Bearish);

        var structureBreak = Assert.Single(result);

        Assert.Equal(StructureBreakType.BreakOfStructure, structureBreak.Type);
        Assert.Equal(BreakDirection.Bearish, structureBreak.Direction);
        Assert.Equal(90m, structureBreak.BrokenPrice);
    }

    [Fact]
    public void DetectBreaks_WhenOnlyWickCrossesLevel_ReturnsNoBreak()
    {
        var service = new StructureBreakDetectionService();

        var structure = CreateBullishStructure();

        var candles = new List<Candle>
        {
            // High wicks above 110,
            // but candle closes below 110.
            CreateCandle(10, 30, high: 115, low: 100, close: 108)
        };

        var result = service.DetectBreaks(
            candles,
            structure,
            MarketTrend.Bullish);

        Assert.Empty(result);
    }

    private static List<MarketStructurePoint>
        CreateBullishStructure()
    {
        return
        [
            new MarketStructurePoint
            {
                OpenTime = Time(10, 10),
                ConfirmedAt = Time(10, 20),
                Price = 110,
                SwingType = SwingType.High,
                StructureType =
                    MarketStructureType.HigherHigh
            },

            new MarketStructurePoint
            {
                OpenTime = Time(10, 20),
                ConfirmedAt = Time(10, 30),
                Price = 95,
                SwingType = SwingType.Low,
                StructureType =
                    MarketStructureType.HigherLow
            }
        ];
    }

    private static List<MarketStructurePoint>
        CreateBearishStructure()
    {
        return
        [
            new MarketStructurePoint
            {
                OpenTime = Time(10, 10),
                ConfirmedAt = Time(10, 20),
                Price = 105,
                SwingType = SwingType.High,
                StructureType =
                    MarketStructureType.LowerHigh
            },

            new MarketStructurePoint
            {
                OpenTime = Time(10, 20),
                ConfirmedAt = Time(10, 30),
                Price = 90,
                SwingType = SwingType.Low,
                StructureType =
                    MarketStructureType.LowerLow
            }
        ];
    }

    private static Candle CreateCandle(int hour, int minute, decimal high, decimal low, decimal close)
    {
        var openTime = Time(hour, minute);

        return new Candle
        {
            OpenTime = openTime,
            CloseTime = openTime.AddMinutes(5),
            Open = close,
            High = high,
            Low = low,
            Close = close,
            Volume = 1,
            IsClosed = true
        };
    }

    private static DateTime Time(int hour, int minute)
    {
        return new DateTime(2026, 9, 6, hour, minute, 0);
    }

    [Fact]
    public void DetectBreaks_WhenBreakOccursBeforeStructureConfirmation_ReturnsNoBreak()
    {
        var service = new StructureBreakDetectionService();
        var structure = new List<MarketStructurePoint>
    {
        new()
        {
            OpenTime = Time(10, 10),
            ConfirmedAt = Time(10, 30),
            Price = 110,
            SwingType = SwingType.High,
            StructureType = MarketStructureType.HigherHigh
        },

        new()
        {
            OpenTime = Time(10, 15),
            ConfirmedAt = Time(10, 30),
            Price = 95,
            SwingType = SwingType.Low,
            StructureType = MarketStructureType.HigherLow
        }
    };

        var candles = new List<Candle>
    {
        // Closes at 10:25.
        // Structure is not confirmed until 10:30.
        CreateCandle(10, 20, high: 115, low: 100, close: 114)
    };

        var result = service.DetectBreaks(candles, structure, MarketTrend.Bullish);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectBreaks_WhenBreakOccursAtStructureConfirmation_CanDetectBreak()
    {
        var service = new StructureBreakDetectionService();
        var structure = new List<MarketStructurePoint>
    {
        new()
        {
            OpenTime = Time(10, 10),
            ConfirmedAt = Time(10, 30),
            Price = 110,
            SwingType = SwingType.High,
            StructureType = MarketStructureType.HigherHigh
        },

        new()
        {
            OpenTime = Time(10, 15),
            ConfirmedAt = Time(10, 30),
            Price = 95,
            SwingType = SwingType.Low,
            StructureType = MarketStructureType.HigherLow
        }
    };

        var candles = new List<Candle>
    {
        // Opens 10:25 and closes exactly 10:30.
        CreateCandle(10, 25, high: 115, low: 100, close: 114)
    };

        var result = service.DetectBreaks(candles, structure, MarketTrend.Bullish);

        var structureBreak = Assert.Single(result);

        Assert.Equal(StructureBreakType.BreakOfStructure, structureBreak.Type);
        Assert.Equal(BreakDirection.Bullish, structureBreak.Direction);
    }

    [Fact]
    public void DetectBreak_BullishState_CloseBelowProtectedLow_ReturnsBearishChoch()
    {
        var service = new StructureBreakDetectionService();
        var state = new MarketStructureState();

        var high = new MarketStructurePoint
        {
            OpenTime = Time(10, 0),
            ConfirmedAt = Time(10, 10),
            Price = 110m,
            SwingType = SwingType.High,
            StructureType = MarketStructureType.HigherHigh
        };

        var low = new MarketStructurePoint
        {
            OpenTime = Time(10, 5),
            ConfirmedAt = Time(10, 15),
            Price = 100m,
            SwingType = SwingType.Low,
            StructureType = MarketStructureType.HigherLow
        };

        state.SetTrend(MarketTrend.Bullish);
        state.SetProtectedLevels(high, low);

        var candle = new Candle
        {
            OpenTime = Time(10, 20),
            CloseTime = Time(10, 25),
            Open = 102m,
            High = 103m,
            Low = 97m,
            Close = 98m,
            IsClosed = true
        };

        var result = service.DetectBreak(candle, state);

        Assert.NotNull(result);

        Assert.Equal(StructureBreakType.ChangeOfCharacter, result.Type);
        Assert.Equal(BreakDirection.Bearish, result.Direction);
        Assert.Equal(MarketTrend.Bullish, result.TrendBeforeBreak);
        Assert.Equal(MarketTrend.Transition, result.TrendAfterBreak);
        Assert.Equal(100m, result.BrokenPrice);
    }

    [Fact]
    public void DetectBreak_BearishState_CloseAboveProtectedHigh_ReturnsBullishChoch()
    {
        var service = new StructureBreakDetectionService();
        var state = new MarketStructureState();

        var high = new MarketStructurePoint
        {
            OpenTime = Time(10, 0),
            ConfirmedAt = Time(10, 10),
            Price = 110m,
            SwingType = SwingType.High,
            StructureType = MarketStructureType.LowerHigh
        };

        var low = new MarketStructurePoint
        {
            OpenTime = Time(10, 5),
            ConfirmedAt = Time(10, 15),
            Price = 100m,
            SwingType = SwingType.Low,
            StructureType = MarketStructureType.LowerLow
        };

        state.SetTrend(MarketTrend.Bearish);
        state.SetProtectedLevels(high, low);

        var candle = new Candle
        {
            OpenTime = Time(10, 20),
            CloseTime = Time(10, 25),
            Open = 108m,
            High = 113m,
            Low = 107m,
            Close = 112m,
            IsClosed = true
        };

        var result = service.DetectBreak(candle, state);

        Assert.NotNull(result);

        Assert.Equal(StructureBreakType.ChangeOfCharacter, result.Type);
        Assert.Equal(BreakDirection.Bullish, result.Direction);
        Assert.Equal(MarketTrend.Bearish, result.TrendBeforeBreak);
        Assert.Equal(MarketTrend.Transition, result.TrendAfterBreak);
        Assert.Equal(110m, result.BrokenPrice);
    }

}