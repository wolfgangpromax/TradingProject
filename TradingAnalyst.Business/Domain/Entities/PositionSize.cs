namespace TradingAnalyst.Business.Domain.Entities;

public sealed class PositionSize
{
    public required TradeSetup Setup { get; init; }
    public decimal AccountEquity { get; init; }
    public decimal RiskPercentage { get; init; }
    public decimal AccountRiskAmount { get; init; }
    public decimal RiskPerUnit { get; init; }
    public decimal Quantity { get; init; }
    public decimal PositionValue { get; init; }
    public required IReadOnlyCollection<string> Reasons { get; init; }
}