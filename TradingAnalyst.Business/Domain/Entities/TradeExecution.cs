using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeExecution
{
    public required TradeOrder Order { get; init; }
    public OrderStatus Status { get; init; }
    public decimal? FilledPrice { get; init; }
    public decimal FilledQuantity { get; init; }
    public DateTime? ExecutedAt { get; init; }
    public required IReadOnlyCollection<string> Reasons { get; init; }
}