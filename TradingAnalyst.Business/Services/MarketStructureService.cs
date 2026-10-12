using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class MarketStructureService : IMarketStructureService
{
    public IReadOnlyCollection<MarketStructurePoint> Classify(IReadOnlyCollection<SwingPoint> swings)
    {
        if (swings.Count == 0)
        {
            return [];
        }

        var orderedSwings = swings
            .OrderBy(x => x.OpenTime)
            .ThenBy(x => x.Type)
            .ToList();

        var result =
            new List<MarketStructurePoint>();

        SwingPoint? previousHigh = null;
        SwingPoint? previousLow = null;

        foreach (var swing in orderedSwings)
        {
            if (swing.Type == SwingType.High)
            {
                if (previousHigh is not null)
                {
                    result.Add(CreateHighStructurePoint( swing, previousHigh));
                }

                previousHigh = swing;
            }
            else if (swing.Type == SwingType.Low)
            {
                if (previousLow is not null)
                {
                    result.Add(CreateLowStructurePoint(swing, previousLow));
                }

                previousLow = swing;
            }
        }

        return result;
    }

    private static MarketStructurePoint
        CreateHighStructurePoint(SwingPoint current,SwingPoint previous)
    {
        var structureType = current.Price > previous.Price
                                ? MarketStructureType.HigherHigh
                                : current.Price < previous.Price
                                    ? MarketStructureType.LowerHigh
                                    : MarketStructureType.EqualHigh;

        return CreateStructurePoint(current, structureType);
    }

    private static MarketStructurePoint
        CreateLowStructurePoint( SwingPoint current, SwingPoint previous)
    {
        var structureType = current.Price > previous.Price
                                ? MarketStructureType.HigherLow
                                : current.Price < previous.Price
                                    ? MarketStructureType.LowerLow
                                    : MarketStructureType.EqualLow;

        return CreateStructurePoint(current, structureType);
    }

    private static MarketStructurePoint
        CreateStructurePoint(SwingPoint swing, MarketStructureType structureType)
    {
        return new MarketStructurePoint
        {
            OpenTime = swing.OpenTime,
            ConfirmedAt = swing.ConfirmedAt,
            Price = swing.Price,
            SwingType = swing.Type,
            StructureType = structureType,
            CandleIndex = swing.CandleIndex
        };
    }
}