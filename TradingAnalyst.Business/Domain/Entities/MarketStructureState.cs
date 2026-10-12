using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Domain.ValueObjects;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class MarketStructureState
{
    public MarketTrend Trend { get; private set; } = MarketTrend.Unknown;

    public MarketStructurePoint? ProtectedHigh { get; private set; }

    public MarketStructurePoint? ProtectedLow { get; private set; }

    public HashSet<StructureLevelKey> ConsumedLevels { get; } = [];

    public void SetTrend(MarketTrend trend)
    {
        Trend = trend;

        if (trend != MarketTrend.Transition)
        {
            TransitionDirection = null;
        }
    }

    public void SetProtectedHigh(MarketStructurePoint? structurePoint)
    {
        ProtectedHigh = structurePoint;
    }

    public void SetProtectedLow(MarketStructurePoint? structurePoint)
    {
        ProtectedLow = structurePoint;
    }

    public void SetProtectedLevels(MarketStructurePoint? high, MarketStructurePoint? low)
    {
        ProtectedHigh = high;
        ProtectedLow = low;
    }

    public void EnterTransition(BreakDirection direction)
    {
        Trend = MarketTrend.Transition;
        TransitionDirection = direction;
    }

    public void ConsumeLevel(MarketStructurePoint structurePoint)
    {
        ConsumedLevels.Add(
            new StructureLevelKey(
                structurePoint.OpenTime,
                structurePoint.SwingType,
                structurePoint.Price));
    }

    public bool IsConsumed(MarketStructurePoint structurePoint)
    {
        return ConsumedLevels.Contains(
            new StructureLevelKey(
                structurePoint.OpenTime,
                structurePoint.SwingType,
                structurePoint.Price));
    }

    public BreakDirection? TransitionDirection { get; private set; }

    public bool TryConfirmTransition(
    MarketTrend calculatedTrend)
    {
        if (Trend != MarketTrend.Transition || TransitionDirection is null)
        {
            return false;
        }

        var isConfirmed =(TransitionDirection == BreakDirection.Bullish && calculatedTrend == MarketTrend.Bullish) ||
            (TransitionDirection == BreakDirection.Bearish && calculatedTrend == MarketTrend.Bearish);

        if (!isConfirmed)
        {
            return false;
        }

        SetTrend(calculatedTrend);

        return true;
    }
}