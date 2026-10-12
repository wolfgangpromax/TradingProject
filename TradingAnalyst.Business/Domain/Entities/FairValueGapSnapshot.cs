namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapSnapshot
{
    public required DateTime Time { get; init; }
    public required IReadOnlyCollection<FairValueGapState> Gaps { get; init; }
}