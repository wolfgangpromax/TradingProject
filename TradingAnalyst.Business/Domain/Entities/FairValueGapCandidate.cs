namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapCandidate
{
    public required FairValueGapState State { get; init; }
    public required FairValueGapQualification Qualification { get; init; }
    public required FairValueGapAlignment Alignment { get; init; }
    public required bool IsCandidate { get; init; }
    public required IReadOnlyCollection<string> RejectionReasons { get; init; }
}