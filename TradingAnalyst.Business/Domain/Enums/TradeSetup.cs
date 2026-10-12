using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeSetup
{
    public required FairValueGapEntryConfirmation EntryConfirmation { get; init; }
    public required TradeDirection Direction { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal StopLossPrice { get; init; }
    public required decimal TakeProfitPrice { get; init; }
    public decimal RiskPerUnit { get; init; }
    public decimal RewardPerUnit { get; init; }
    public required decimal RiskRewardRatio { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyCollection<string> Reasons { get; init; }
}