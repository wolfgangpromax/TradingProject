using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeOutcome
{
    public required TradeSetup Setup { get; init; }
    public required TradeOutcomeStatus Status { get; init; }
    public DateTime? ClosedAt { get; init; }
    public decimal? ExitPrice { get; init; }
    public decimal? RealizedRiskRewardRatio { get; init; }
    public required IReadOnlyCollection<string> Reasons { get; init; }
}