using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class MarketStructureReplayServiceTests
{
    [Fact]
    public void Replay_FutureSwingDoesNotAppearInEarlierSnapshot()
    {
        var service = CreateService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),

            // Swing high candidate.
            CreateCandle(10, 10, 120, 95),

            CreateCandle(10, 15, 108, 93),
            CreateCandle(10, 20, 103, 91)
        };

        var result = service.Replay(candles, swingStrength: 2);

        var snapshots = result.ToList();

        Assert.Single(snapshots);

        var snapshot = snapshots[0];

        Assert.Equal(Time(10, 25), snapshot.Time);

        var swing = Assert.Single(snapshot.Swings);

        Assert.Equal(Time(10, 10), swing.OpenTime);
        Assert.Equal(Time(10, 25), swing.ConfirmedAt);
    }

    [Fact]
    public void Replay_SwingAppearsOnlyWhenConfirmed()
    {
        var service = CreateService();

        var candles = new List<Candle>
        {
            CreateCandle(10, 0, 100, 90),
            CreateCandle(10, 5, 105, 92),
            CreateCandle(10, 10, 120, 95),
            CreateCandle(10, 15, 108, 93),
            CreateCandle(10, 20, 103, 91),

            // Extra candle after confirmation.
            CreateCandle(10, 25, 101, 89)
        };

        var result = service.Replay(candles, swingStrength: 2);

        var snapshots = result.ToList();

        Assert.Equal(2, snapshots.Count);

        var confirmationSnapshot = snapshots[0];
        var nextSnapshot = snapshots[1];

        Assert.Equal(Time(10, 25), confirmationSnapshot.Time);
        Assert.Contains(confirmationSnapshot.Swings, x => x.OpenTime == Time(10, 10));
        Assert.Equal(Time(10, 30), nextSnapshot.Time);
        Assert.Contains(nextSnapshot.Swings, x => x.OpenTime == Time(10, 10));
    }

    [Fact]
    public void Replay_IgnoresOpenCandles()
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
                Open = 100,
                High = 999,
                Low = 1,
                Close = 500,
                Volume = 1,
                IsClosed = false
            }
        };

        var result = service.Replay(candles, swingStrength: 2);

        var snapshots = result.ToList();

        Assert.Single(snapshots);

        Assert.Equal(Time(10, 25), snapshots[0].Time);

        Assert.DoesNotContain(snapshots, x => x.Time == Time(10, 30));
    }

    private static MarketStructureReplayService
        CreateService()
    {
        return new MarketStructureReplayService(
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
        return new DateTime( 2026, 9, 6, hour, minute, 0);
    }

    [Fact]
    public void Replay_TrendBecomesBullishOnlyAfterHHAndHLAreConfirmed()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // First swing high = 110
        CreateCandle(10, 0, 100, 90),
        CreateCandle(10, 5, 110, 95),
        CreateCandle(10, 10, 105, 92),

        // First swing low = 90
        CreateCandle(10, 15, 103, 90),
        CreateCandle(10, 20, 108, 94),

        // Second swing high = 120
        // Compared with 110 => Higher High
        CreateCandle(10, 25, 120, 100),
        CreateCandle(10, 30, 112, 98),

        // Second swing low = 95
        // Compared with 90 => Higher Low
        CreateCandle(10, 35, 108, 95),
        CreateCandle(10, 40, 114, 99),

        CreateCandle(10, 45, 116, 102)
    };

        var result = service.Replay(candles, swingStrength: 1);

        var snapshots = result.OrderBy(x => x.Time).ToList();

        Assert.NotEmpty(snapshots);

        var bullishSnapshot = snapshots.FirstOrDefault(x => x.Trend == MarketTrend.Bullish);

        Assert.NotNull(bullishSnapshot);

        /*
            Second High:
                OpenTime    = 10:25
                ConfirmedAt = 10:35
                Structure   = HigherHigh

            Second Low:
                OpenTime    = 10:35
                ConfirmedAt = 10:45
                Structure   = HigherLow

            Therefore HH + HL first becomes available
            at 10:45.
        */

        Assert.Equal(Time(10, 45), bullishSnapshot.Time);

        Assert.All(snapshots.Where(x => x.Time < Time(10, 45)), x => Assert.NotEqual(MarketTrend.Bullish, x.Trend));
    }

    [Fact]
    public void Replay_TrendBecomesBearishOnlyAfterLHAndLLAreConfirmed()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // First swing low = 90
        CreateCandle(10, 0, 105, 95),
        CreateCandle(10, 5, 102, 90),
        CreateCandle(10, 10, 106, 94),

        // First swing high = 110
        CreateCandle(10, 15, 110, 96),
        CreateCandle(10, 20, 107, 93),

        // Second swing low = 80 -> Lower Low
        CreateCandle(10, 25, 100, 80),
        CreateCandle(10, 30, 103, 85),

        // Second swing high = 105 -> Lower High
        CreateCandle(10, 35, 105, 88),
        CreateCandle(10, 40, 101, 84),

        CreateCandle(10, 45, 98, 82)
    };

        var result = service.Replay(candles, swingStrength: 1);

        var snapshots = result.OrderBy(x => x.Time).ToList();

        Assert.NotEmpty(snapshots);

        var bearishSnapshot = snapshots.FirstOrDefault(x => x.Trend == MarketTrend.Bearish);

        Assert.NotNull(bearishSnapshot);

        // LL at 10:25 is confirmed at 10:35.
        // LH at 10:35 is confirmed at 10:45.
        //
        // Both are finally available at 10:45.
        Assert.Equal(Time(10, 45), bearishSnapshot.Time);

        Assert.All(snapshots.Where(x => x.Time < Time(10, 45)), x => Assert.NotEqual(MarketTrend.Bearish, x.Trend));
    }

    [Fact]
    public void Replay_BullishTrendThenCloseAboveHigh_DetectsBullishBos()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // First high = 110
        CreateCandle(10, 0, 100, 90),
        CreateCandle(10, 5, 110, 95),
        CreateCandle(10, 10, 105, 92),

        // First low = 90
        CreateCandle(10, 15, 103, 90),
        CreateCandle(10, 20, 108, 94),

        // Higher High = 120
        CreateCandle(10, 25, 120, 100),
        CreateCandle(10, 30, 112, 98),

        // Higher Low = 95
        CreateCandle(10, 35, 108, 95),
        CreateCandle(10, 40, 114, 99),

        // Bullish trend is now known at 10:45.
        CreateCandle(10, 45, 118, 105),

        // Closes above 120 at 10:55.
        CreateCandleWithClose(10, 50, high: 125, low: 115, close: 123)
    };

        var result = service.Replay(
            candles,
            swingStrength: 1);

        var breakSnapshot = result
            .FirstOrDefault(x =>
                x.StructureBreaks.Any(y =>
                    y.Type ==
                        StructureBreakType.BreakOfStructure &&
                    y.Direction ==
                        BreakDirection.Bullish));

        Assert.NotNull(breakSnapshot);

        Assert.Equal(Time(10, 55), breakSnapshot.Time);

        var structureBreak = Assert.Single(breakSnapshot.StructureBreaks);

        Assert.Equal(StructureBreakType.BreakOfStructure, structureBreak.Type);
        Assert.Equal(BreakDirection.Bullish, structureBreak.Direction);
        Assert.Equal(120m, structureBreak.BrokenPrice);
        Assert.Equal(123m, structureBreak.BreakClosePrice);
        Assert.Equal(Time(10, 55), structureBreak.BreakTime);
    }

    private static Candle CreateCandleWithClose(int hour, int minute, decimal high, decimal low, decimal close)
    {
        var time = Time(hour, minute);

        return new Candle
        {
            OpenTime = time,
            CloseTime = time.AddMinutes(5),
            Open = low,
            High = high,
            Low = low,
            Close = close,
            Volume = 1,
            IsClosed = true
        };
    }

    [Fact]
    public void Replay_BullishTrendThenCloseBelowLow_DetectsBearishChoch()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // First high = 110
        CreateCandle(10, 0, 100, 90),
        CreateCandle(10, 5, 110, 95),
        CreateCandle(10, 10, 105, 92),

        // First low = 90
        CreateCandle(10, 15, 103, 90),
        CreateCandle(10, 20, 108, 94),

        // Higher High = 120
        CreateCandle(10, 25, 120, 100),
        CreateCandle(10, 30, 112, 98),

        // Higher Low = 95
        CreateCandle(10, 35, 108, 95),
        CreateCandle(10, 40, 114, 99),

        // HH + HL are known after this candle closes.
        CreateCandle(10, 45, 116, 100),

        // Close below the structural low of 95.
        CreateCandleWithClose(10, 50, high: 118, low: 90, close: 92)
    };

        var result = service.Replay(candles, swingStrength: 1);

        var breakSnapshot = result
            .FirstOrDefault(x =>
                x.StructureBreaks.Any(y =>
                    y.Type ==
                        StructureBreakType.ChangeOfCharacter &&
                    y.Direction ==
                        BreakDirection.Bearish));

        Assert.NotNull(breakSnapshot);

        Assert.Equal(Time(10, 55), breakSnapshot.Time);

        var structureBreak = Assert.Single(breakSnapshot.StructureBreaks);

        Assert.Equal(StructureBreakType.ChangeOfCharacter, structureBreak.Type);
        Assert.Equal(BreakDirection.Bearish, structureBreak.Direction);
        Assert.Equal(95m, structureBreak.BrokenPrice);
        Assert.Equal(92m, structureBreak.BreakClosePrice);
        Assert.Equal(MarketTrend.Bullish, structureBreak.TrendBeforeBreak);
        Assert.Equal(MarketTrend.Transition, structureBreak.TrendAfterBreak);
    }

    [Fact]
    public void Replay_BearishTrendThenCloseBelowLow_DetectsBearishBos()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // First low = 90
        CreateCandle(10, 0, 105, 95),
        CreateCandle(10, 5, 102, 90),
        CreateCandle(10, 10, 106, 94),

        // First high = 110
        CreateCandle(10, 15, 110, 96),
        CreateCandle(10, 20, 107, 93),

        // Lower Low = 80
        CreateCandle(10, 25, 100, 80),
        CreateCandle(10, 30, 103, 85),

        // Lower High = 105
        CreateCandle(10, 35, 105, 88),
        CreateCandle(10, 40, 101, 84),

        // LH + LL are now known.
        CreateCandle(10, 45, 98, 82),

        // Close below structural low 80.
        CreateCandleWithClose(10, 50, high: 95, low: 75, close: 78)
    };

        var result = service.Replay(candles, swingStrength: 1);

        var breakSnapshot = result
            .FirstOrDefault(x =>
                x.StructureBreaks.Any(y =>
                    y.Type ==
                        StructureBreakType.BreakOfStructure &&
                    y.Direction ==
                        BreakDirection.Bearish));

        Assert.NotNull(breakSnapshot);
        Assert.Equal(Time(10, 55), breakSnapshot.Time);

        var structureBreak = Assert.Single(breakSnapshot.StructureBreaks);

        Assert.Equal(StructureBreakType.BreakOfStructure, structureBreak.Type);
        Assert.Equal(BreakDirection.Bearish, structureBreak.Direction);
        Assert.Equal(80m, structureBreak.BrokenPrice);
        Assert.Equal(78m, structureBreak.BreakClosePrice);
        Assert.Equal(MarketTrend.Bearish, structureBreak.TrendBeforeBreak);
    }

    [Fact]
    public void Replay_BearishTrendThenCloseAboveHigh_DetectsBullishChoch()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // First low = 90
        CreateCandle(10, 0, 105, 95),
        CreateCandle(10, 5, 102, 90),
        CreateCandle(10, 10, 106, 94),

        // First high = 110
        CreateCandle(10, 15, 110, 96),
        CreateCandle(10, 20, 107, 93),

        // Lower Low = 80
        CreateCandle(10, 25, 100, 80),
        CreateCandle(10, 30, 103, 85),

        // Lower High = 105
        CreateCandle(10, 35, 105, 88),
        CreateCandle(10, 40, 101, 84),

        // Bearish structure remains established.
        CreateCandle(10, 45, 98, 82),

        // Close above structural high 105.
        //
        // Low is deliberately below the previous
        // candle's low so 10:45 does not accidentally
        // become a new swing low.
        CreateCandleWithClose(10, 50, high: 112, low: 78, close: 108)
    };

        var result = service.Replay(candles, swingStrength: 1);

        var breakSnapshot = result
            .FirstOrDefault(x =>
                x.StructureBreaks.Any(y =>
                    y.Type ==
                        StructureBreakType.ChangeOfCharacter &&
                    y.Direction ==
                        BreakDirection.Bullish));

        Assert.NotNull(breakSnapshot);

        Assert.Equal(Time(10, 55), breakSnapshot.Time);

        var structureBreak =Assert.Single(breakSnapshot.StructureBreaks);

        Assert.Equal(StructureBreakType.ChangeOfCharacter, structureBreak.Type);
        Assert.Equal(BreakDirection.Bullish, structureBreak.Direction);
        Assert.Equal(105m, structureBreak.BrokenPrice);
        Assert.Equal(108m, structureBreak.BreakClosePrice);
        Assert.Equal(MarketTrend.Bearish, structureBreak.TrendBeforeBreak);
        Assert.Equal(MarketTrend.Transition, structureBreak.TrendAfterBreak);
    }

    [Fact]
    public void Replay_SameStructuralLevel_IsBrokenOnlyOnce()
    {
        var service = CreateService();

        var candles = new List<Candle>
    {
        // Establish bullish structure.
        CreateCandle(10, 0, 100, 90),
        CreateCandle(10, 5, 110, 95),
        CreateCandle(10, 10, 105, 92),

        CreateCandle(10, 15, 103, 90),
        CreateCandle(10, 20, 108, 94),

        CreateCandle(10, 25, 120, 100),
        CreateCandle(10, 30, 112, 98),

        CreateCandle(10, 35, 108, 95),
        CreateCandle(10, 40, 114, 99),

        CreateCandle(10, 45, 116, 100),

        // First close below 95 -> CHoCH.
        CreateCandleWithClose(10, 50, high: 118, low: 90, close: 92),

        // Still below 95.
        // Must NOT generate the same break again.
        CreateCandleWithClose(10, 55, high: 117, low: 89, close: 91)
    };

        var result = service.Replay(candles, swingStrength: 1);

        var breaks = result
            .SelectMany(x => x.StructureBreaks)
            .Where(x =>
                x.BrokenPrice == 95m)
            .ToList();

        var structureBreak = Assert.Single(breaks);

        Assert.Equal(Time(10, 55), structureBreak.BreakTime);
    }
}