using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapMitigationService : IFairValueGapMitigationService
{
    public void Update(FairValueGapState state, Candle candle)
    {
        if (!candle.IsClosed ||
            candle.CloseTime <= state.Gap.ConfirmedAt ||
            state.IsMitigated)
        {
            return;
        }

        var fillPercentage = CalculateFillPercentage( state.Gap, candle);

        state.UpdateFill(fillPercentage, candle.CloseTime);
    }

    private static decimal CalculateFillPercentage(FairValueGap gap, Candle candle)
    {
        if (gap.GapSize <= 0m)
        {
            return 0m;
        }

        return gap.Type switch
        {
            FairValueGapType.Bullish => CalculateBullishFill(gap, candle),
            FairValueGapType.Bearish => CalculateBearishFill(gap, candle),
            _ => 0m
        };
    }

    private static decimal CalculateBullishFill(FairValueGap gap, Candle candle)
    {
        if (candle.Low >= gap.UpperPrice)
        {
            return 0m;
        }

        var filledAmount =
            gap.UpperPrice -
            candle.Low;

        return filledAmount / gap.GapSize * 100m;
    }

    private static decimal CalculateBearishFill(FairValueGap gap, Candle candle)
    {
        if (candle.High <= gap.LowerPrice)
        {
            return 0m;
        }

        var filledAmount = candle.High - gap.LowerPrice;

        return filledAmount / gap.GapSize * 100m;
    }
}