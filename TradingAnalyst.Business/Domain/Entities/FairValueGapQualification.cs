namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapQualification
{
    public required FairValueGapState State { get; init; }
    public required bool IsQualified { get; init; }
    public required IReadOnlyCollection<string> RejectionReasons { get; init; }
}