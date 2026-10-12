using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeOrder
{
    public required TradeRiskAssessment RiskAssessment { get; init; }
    public TradeDirection Direction { get; init; }
    public OrderType OrderType { get; init; }
    public decimal Quantity { get; init; }
    public decimal RequestedPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public required IReadOnlyCollection<string> Reasons { get; init; }
}