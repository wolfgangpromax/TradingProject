namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapInteraction
{
    public required FairValueGapCandidate Candidate { get; init; }
    public required bool HasInteracted { get; init; }
    public DateTime? FirstInteractionTime { get; init; }
    public decimal? InteractionPrice { get; init; }
}