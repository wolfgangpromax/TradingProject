using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class MarketTrendService : IMarketTrendService
{
    public MarketTrend DetermineTrend(IReadOnlyCollection<MarketStructurePoint> structurePoints)
    {
        if (structurePoints.Count < 2)
        {
            return MarketTrend.Unknown;
        }

        var ordered = structurePoints.OrderBy(x => x.ConfirmedAt).ToList();

        MarketStructurePoint? latestHigh = null;
        MarketStructurePoint? latestLow = null;

        foreach (var point in ordered)
        {
            if (point.SwingType == SwingType.High)
            {
                latestHigh = point;
            }
            else if (point.SwingType == SwingType.Low)
            {
                latestLow = point;
            }
        }

        if (latestHigh is null || latestLow is null)
        {
            return MarketTrend.Unknown;
        }

        if (latestHigh.StructureType == MarketStructureType.HigherHigh &&
            latestLow.StructureType == MarketStructureType.HigherLow)
        {
            return MarketTrend.Bullish;
        }

        if (latestHigh.StructureType == MarketStructureType.LowerHigh &&
            latestLow.StructureType == MarketStructureType.LowerLow)
        {
            return MarketTrend.Bearish;
        }

        return MarketTrend.Unknown;
    }
}