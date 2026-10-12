namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapInteractionState
{
    public FairValueGapInteractionState(FairValueGapCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Candidate = candidate;
    }

    public FairValueGapCandidate Candidate { get; }
    public bool HasInteracted { get; private set; }
    public DateTime? FirstInteractionTime { get; private set; }
    public decimal? FirstInteractionPrice { get; private set; }
    public void RecordInteraction(DateTime interactionTime, decimal interactionPrice)
    {
        if (HasInteracted)
        {
            return;
        }

        HasInteracted = true;
        FirstInteractionTime = interactionTime;
        FirstInteractionPrice = interactionPrice;
    }

    public FairValueGapInteractionState CreateSnapshot()
    {
        var snapshot = new FairValueGapInteractionState(Candidate);

        if (HasInteracted && FirstInteractionTime.HasValue && FirstInteractionPrice.HasValue)
        {
            snapshot.RecordInteraction(FirstInteractionTime.Value, FirstInteractionPrice.Value);
        }

        return snapshot;
    }
}