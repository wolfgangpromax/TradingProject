using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapQualificationSnapshot
{
    public required DateTime Time { get; init; }
    public required decimal? AverageTrueRange { get; init; }
    public required MarketTrend MarketTrend { get; init; }
    public required IReadOnlyCollection<FairValueGapQualification> Qualifications { get; init; }
    public required IReadOnlyCollection<FairValueGapAlignment> Alignments { get; init; }
    public required IReadOnlyCollection<FairValueGapCandidate> Candidates { get; init; }
    public required IReadOnlyCollection<FairValueGapInteractionState> Interactions { get; init; }
    public required IReadOnlyCollection<FairValueGapEntryConfirmationState> EntryConfirmations { get; init; }
    public required IReadOnlyCollection<TradeSetup> TradeSetups { get; init; }
    public required IReadOnlyCollection<TradeOutcome> TradeOutcomes { get; init; }
    public required IReadOnlyCollection<PositionSize> PositionSizes { get; init; }
    public required IReadOnlyCollection<TradeRiskAssessment> TradeRiskAssessments { get; init; }
    public required IReadOnlyCollection<TradeOrder> TradeOrders { get; init; }
    public required IReadOnlyCollection<TradeExecution> TradeExecutions { get; init; }
}