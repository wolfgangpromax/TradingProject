using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapInteractionService : IFairValueGapInteractionService
{
    public FairValueGapInteraction Evaluate(FairValueGapCandidate candidate, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(candle);

        var gap = candidate.State.Gap;

        if (!candidate.IsCandidate || !candle.IsClosed || candle.CloseTime <= gap.ConfirmedAt)
        {
            return NoInteraction(candidate);
        }

        return gap.Type switch
        {
            FairValueGapType.Bullish => EvaluateBullish(candidate, candle),
            FairValueGapType.Bearish => EvaluateBearish(candidate, candle),
            _ => NoInteraction(candidate)
        };
    }

    private static FairValueGapInteraction EvaluateBullish(FairValueGapCandidate candidate, Candle candle)
    {
        var gap = candidate.State.Gap;

        // Price is completely above the FVG.
        if (candle.Low > gap.UpperPrice)
        {
            return NoInteraction(candidate);
        }

        // Price is completely below the FVG.
        if (candle.High < gap.LowerPrice)
        {
            return NoInteraction(candidate);
        }

        // Clamp the interaction price inside the FVG.
        var interactionPrice = Math.Max(Math.Min(candle.Low, gap.UpperPrice), gap.LowerPrice);

        return Interaction(candidate, candle.CloseTime, interactionPrice);
    }

    private static FairValueGapInteraction EvaluateBearish(FairValueGapCandidate candidate, Candle candle)
    {
        var gap = candidate.State.Gap;

        // Price is completely below the FVG.
        if (candle.High < gap.LowerPrice)
        {
            return NoInteraction(candidate);
        }

        // Price is completely above the FVG.
        if (candle.Low > gap.UpperPrice)
        {
            return NoInteraction(candidate);
        }

        // Clamp the interaction price inside the FVG.
        var interactionPrice = Math.Min(Math.Max(candle.High, gap.LowerPrice), gap.UpperPrice);

        return Interaction(candidate, candle.CloseTime, interactionPrice);
    }

    private static FairValueGapInteraction Interaction(FairValueGapCandidate candidate, DateTime time, decimal price)
    {
        return new FairValueGapInteraction
        {
            Candidate = candidate,
            HasInteracted = true,
            FirstInteractionTime = time,
            InteractionPrice = price
        };
    }

    private static FairValueGapInteraction NoInteraction(FairValueGapCandidate candidate)
    {
        return new FairValueGapInteraction
        {
            Candidate = candidate,
            HasInteracted = false,
            FirstInteractionTime = null,
            InteractionPrice = null
        };
    }

    public void Update(FairValueGapInteractionState state, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(candle);

        if (state.HasInteracted)
        {
            return;
        }

        var interaction = Evaluate(state.Candidate, candle);

        if (!interaction.HasInteracted ||
            !interaction.FirstInteractionTime.HasValue ||
            !interaction.InteractionPrice.HasValue)
        {
            return;
        }

        state.RecordInteraction(interaction.FirstInteractionTime.Value, interaction.InteractionPrice.Value);
    }
}