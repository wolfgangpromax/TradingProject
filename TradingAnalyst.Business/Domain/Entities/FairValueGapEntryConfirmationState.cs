using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapEntryConfirmationState
{
    public FairValueGapEntryConfirmationState(FairValueGapInteractionState interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        Interaction = interaction;
    }

    public FairValueGapInteractionState Interaction { get; }
    public EntryConfirmationStatus Status { get; private set; } = EntryConfirmationStatus.Waiting;
    public DateTime? ConfirmedAt { get; private set; }
    public decimal? ConfirmationPrice { get; private set; }
    public IReadOnlyCollection<string> Reasons { get; private set; } = Array.Empty<string>();

    public void Confirm(DateTime confirmedAt, decimal confirmationPrice, IReadOnlyCollection<string> reasons)
    {
        if (Status != EntryConfirmationStatus.Waiting)
            return;

        Status = EntryConfirmationStatus.Confirmed;
        ConfirmedAt = confirmedAt;
        ConfirmationPrice = confirmationPrice;
        Reasons = reasons;
    }

    public void Invalidate(IReadOnlyCollection<string> reasons)
    {
        if (Status != EntryConfirmationStatus.Waiting)
            return;

        Status = EntryConfirmationStatus.Invalidated;
        ConfirmedAt = null;
        ConfirmationPrice = null;
        Reasons = reasons;
    }

    public FairValueGapEntryConfirmationState CreateSnapshot()
    {
        var snapshot = new FairValueGapEntryConfirmationState(Interaction.CreateSnapshot());

        if (Status == EntryConfirmationStatus.Confirmed &&
            ConfirmedAt.HasValue &&
            ConfirmationPrice.HasValue)
        {
            snapshot.Confirm(ConfirmedAt.Value, ConfirmationPrice.Value, Reasons.ToArray());
        }
        else if (Status == EntryConfirmationStatus.Invalidated)
        {
            snapshot.Invalidate(Reasons.ToArray());
        }

        return snapshot;
    }
}