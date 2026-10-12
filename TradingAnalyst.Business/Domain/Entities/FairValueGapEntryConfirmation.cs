using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapEntryConfirmation
{
    public required FairValueGapInteractionState Interaction { get; init; }
    public required EntryConfirmationStatus Status { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public decimal? ConfirmationPrice { get; init; }
    public required IReadOnlyCollection<string> Reasons { get; init; }
}