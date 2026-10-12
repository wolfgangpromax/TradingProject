using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapEntryConfirmationService : IFairValueGapEntryConfirmationService
{
    public FairValueGapEntryConfirmation Evaluate(FairValueGapInteractionState interaction, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(candle);

        if (!interaction.HasInteracted ||
            !interaction.FirstInteractionTime.HasValue)
        {
            return Waiting(interaction, "FVG has not been interacted with yet.");
        }

        if (!candle.IsClosed)
        {
            return Waiting(interaction, "Confirmation candle is not closed.");
        }

        // The candle that first interacts with the FVG
        // cannot also confirm or invalidate the entry.
        if (candle.CloseTime <= interaction.FirstInteractionTime.Value)
        {
            return Waiting(interaction, "Waiting for a candle after the first FVG interaction.");
        }

        var gap = interaction.Candidate.State.Gap;

        return gap.Type switch
        {
            FairValueGapType.Bullish => EvaluateBullish(interaction, candle),
            FairValueGapType.Bearish => EvaluateBearish(interaction, candle),
            _ => Waiting(interaction, "Unsupported FVG direction.")
        };
    }

    private static FairValueGapEntryConfirmation EvaluateBullish(FairValueGapInteractionState interaction, Candle candle)
    {
        var gap = interaction.Candidate.State.Gap;

        // A close below the lower FVG boundary invalidates
        // the bullish entry setup.
        if (candle.Close < gap.LowerPrice)
        {
            return Invalidated(interaction, "Bullish FVG entry setup invalidated by a close below the lower boundary.");
        }

        var isBullishCandle = candle.Close > candle.Open;
        var closesAboveGap = candle.Close > gap.UpperPrice;

        if (!isBullishCandle || !closesAboveGap)
        {
            return Waiting(interaction, "Waiting for bullish rejection confirmation above the FVG.");
        }

        return Confirmed(interaction, candle.CloseTime, candle.Close, "Bullish rejection confirmed above the FVG.");
    }

    private static FairValueGapEntryConfirmation EvaluateBearish(FairValueGapInteractionState interaction, Candle candle)
    {
        var gap = interaction.Candidate.State.Gap;

        // A close above the upper FVG boundary invalidates
        // the bearish entry setup.
        if (candle.Close > gap.UpperPrice)
        {
            return Invalidated(interaction, "Bearish FVG entry setup invalidated by a close above the upper boundary.");
        }

        var isBearishCandle = candle.Close < candle.Open;
        var closesBelowGap = candle.Close < gap.LowerPrice;

        if (!isBearishCandle || !closesBelowGap)
        {
            return Waiting(interaction, "Waiting for bearish rejection confirmation below the FVG.");
        }

        return Confirmed(interaction, candle.CloseTime, candle.Close, "Bearish rejection confirmed below the FVG.");
    }

    private static FairValueGapEntryConfirmation Waiting(FairValueGapInteractionState interaction, string reason)
    {
        return new FairValueGapEntryConfirmation
        {
            Interaction = interaction,
            Status = EntryConfirmationStatus.Waiting,
            ConfirmedAt = null,
            ConfirmationPrice = null,
            Reasons = new[] { reason }
        };
    }

    private static FairValueGapEntryConfirmation Confirmed(FairValueGapInteractionState interaction, DateTime confirmedAt, decimal confirmationPrice, string reason)
    {
        return new FairValueGapEntryConfirmation
        {
            Interaction = interaction,
            Status = EntryConfirmationStatus.Confirmed,
            ConfirmedAt = confirmedAt,
            ConfirmationPrice = confirmationPrice,
            Reasons = new[] { reason }
        };
    }

    private static FairValueGapEntryConfirmation Invalidated(FairValueGapInteractionState interaction, string reason)
    {
        return new FairValueGapEntryConfirmation
        {
            Interaction = interaction,
            Status = EntryConfirmationStatus.Invalidated,
            ConfirmedAt = null,
            ConfirmationPrice = null,
            Reasons = new[] { reason }
        };
    }

    public void Update(FairValueGapEntryConfirmationState state, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(candle);

        if (state.Status != EntryConfirmationStatus.Waiting)
            return;

        var result = Evaluate(state.Interaction, candle);

        if (result.Status == EntryConfirmationStatus.Invalidated)
        {
            state.Invalidate(result.Reasons);
            return;
        }

        if (result.Status != EntryConfirmationStatus.Confirmed ||
            !result.ConfirmedAt.HasValue ||
            !result.ConfirmationPrice.HasValue)
        {
            return;
        }

        state.Confirm(result.ConfirmedAt.Value, result.ConfirmationPrice.Value, result.Reasons);
    }
}