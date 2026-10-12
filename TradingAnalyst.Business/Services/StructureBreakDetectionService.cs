using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Domain.ValueObjects;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class StructureBreakDetectionService : IStructureBreakDetectionService
{
    public IReadOnlyCollection<StructureBreak> DetectBreaks(IReadOnlyCollection<Candle> candles,
        IReadOnlyCollection<MarketStructurePoint> structurePoints,MarketTrend trend,
        IReadOnlySet<StructureLevelKey>? consumedLevels = null)
    {
        if (trend == MarketTrend.Unknown ||
            candles.Count == 0 ||
            structurePoints.Count == 0)
        {
            return [];
        }

        var latestHigh = structurePoints
            .Where(x => x.SwingType == SwingType.High)
            .OrderByDescending(x => x.ConfirmedAt)
            .FirstOrDefault();

        var latestLow = structurePoints
            .Where(x => x.SwingType == SwingType.Low)
            .OrderByDescending(x => x.ConfirmedAt)
            .FirstOrDefault();

        if (latestHigh is null || latestLow is null)
        {
            return [];
        }

        var breaks = new List<StructureBreak>();

        var highKey = new StructureLevelKey(
            latestHigh.OpenTime,
            latestHigh.SwingType,
            latestHigh.Price);

        var lowKey = new StructureLevelKey(
            latestLow.OpenTime,
            latestLow.SwingType,
            latestLow.Price);

        foreach (var candle in candles.OrderBy(x => x.OpenTime))
        {
            // Critical for avoiding look-ahead:
            // don't use a structure level before it was confirmed.
            if (candle.CloseTime < latestHigh.ConfirmedAt ||
                candle.CloseTime < latestLow.ConfirmedAt)
            {
                continue;
            }

            if (trend == MarketTrend.Bullish)
            {
                if (candle.Close > latestHigh.Price &&
                    !(consumedLevels?.Contains(highKey) ?? false))
                {
                    breaks.Add(CreateBreak(
                        StructureBreakType.BreakOfStructure,
                        BreakDirection.Bullish,
                        latestHigh,
                        candle,
                        trend,
                        MarketTrend.Bullish));

                    break;
                }

                if (candle.Close < latestLow.Price &&
                    !(consumedLevels?.Contains(lowKey) ?? false))
                {
                    breaks.Add(CreateBreak(
                        StructureBreakType.ChangeOfCharacter,
                        BreakDirection.Bearish,
                        latestLow,
                        candle,
                        trend,
                        MarketTrend.Transition));

                    break;
                }
            }

            if (trend == MarketTrend.Bearish)
            {
                if (candle.Close < latestLow.Price &&
                    !(consumedLevels?.Contains(lowKey) ?? false))
                {
                    breaks.Add(CreateBreak(
                        StructureBreakType.BreakOfStructure,
                        BreakDirection.Bearish,
                        latestLow,
                        candle,
                        trend,
                        MarketTrend.Bearish));

                    break;
                }

                if (candle.Close > latestHigh.Price &&
                    !(consumedLevels?.Contains(highKey) ?? false))
                {
                    breaks.Add(CreateBreak(
                        StructureBreakType.ChangeOfCharacter,
                        BreakDirection.Bullish,
                        latestHigh,
                        candle,
                        trend,
                        MarketTrend.Transition));

                    break;
                }
            }
        }

        return breaks;
    }

    private static StructureBreak CreateBreak(StructureBreakType type, BreakDirection direction, MarketStructurePoint brokenPoint,
        Candle candle, MarketTrend trendBefore, MarketTrend trendAfter)
    {
        return new StructureBreak
        {
            Type = type,
            Direction = direction,
            BrokenPrice = brokenPoint.Price,
            BrokenStructureTime = brokenPoint.OpenTime,
            BreakTime = candle.CloseTime,
            BreakClosePrice = candle.Close,
            TrendBeforeBreak = trendBefore,
            TrendAfterBreak = trendAfter
        };
    }
    public StructureBreak? DetectBreak(Candle candle, MarketStructureState state)
    {
        if (state.Trend == MarketTrend.Unknown ||
            state.Trend == MarketTrend.Transition ||
            state.ProtectedHigh is null ||
            state.ProtectedLow is null)
        {
            return null;
        }

        var protectedHigh = state.ProtectedHigh;
        var protectedLow = state.ProtectedLow;

        if (candle.CloseTime < protectedHigh.ConfirmedAt ||
            candle.CloseTime < protectedLow.ConfirmedAt)
        {
            return null;
        }

        if (state.Trend == MarketTrend.Bullish)
        {
            if (candle.Close > protectedHigh.Price &&
                !state.IsConsumed(protectedHigh))
            {
                return CreateBreak(
                    StructureBreakType.BreakOfStructure,
                    BreakDirection.Bullish,
                    protectedHigh,
                    candle,
                    state.Trend,
                    MarketTrend.Bullish);
            }

            if (candle.Close < protectedLow.Price &&
                !state.IsConsumed(protectedLow))
            {
                return CreateBreak(
                    StructureBreakType.ChangeOfCharacter,
                    BreakDirection.Bearish,
                    protectedLow,
                    candle,
                    state.Trend,
                    MarketTrend.Transition);
            }
        }

        if (state.Trend == MarketTrend.Bearish)
        {
            if (candle.Close < protectedLow.Price &&
                !state.IsConsumed(protectedLow))
            {
                return CreateBreak(
                    StructureBreakType.BreakOfStructure,
                    BreakDirection.Bearish,
                    protectedLow,
                    candle,
                    state.Trend,
                    MarketTrend.Bearish);
            }

            if (candle.Close > protectedHigh.Price &&
                !state.IsConsumed(protectedHigh))
            {
                return CreateBreak(
                    StructureBreakType.ChangeOfCharacter,
                    BreakDirection.Bullish,
                    protectedHigh,
                    candle,
                    state.Trend,
                    MarketTrend.Transition);
            }
        }

        return null;
    }
}