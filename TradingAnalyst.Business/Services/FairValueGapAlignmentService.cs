using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapAlignmentService : IFairValueGapAlignmentService
{
    public FairValueGapAlignment Evaluate(FairValueGapState state, MarketTrend marketTrend)
    {
        ArgumentNullException.ThrowIfNull(state);

        var isAligned =
            (state.Gap.Type == FairValueGapType.Bullish &&
             marketTrend == MarketTrend.Bullish)
            ||
            (state.Gap.Type == FairValueGapType.Bearish &&
             marketTrend == MarketTrend.Bearish);

        var reason =
            (state.Gap.Type, marketTrend) switch
            {
                (FairValueGapType.Bullish, MarketTrend.Bullish) =>
                    "Bullish FVG is aligned with bullish market structure.",

                (FairValueGapType.Bearish, MarketTrend.Bearish) =>
                    "Bearish FVG is aligned with bearish market structure.",

                (_, MarketTrend.Transition) =>
                    "Market structure is in transition.",

                (_, MarketTrend.Unknown) =>
                    "Market structure direction is unknown.",

                _ =>
                    "FVG direction is not aligned with market structure."
            };

        return new FairValueGapAlignment
        {
            State = state,
            MarketTrend = marketTrend,
            IsAligned = isAligned,
            Reason = reason
        };
    }
}