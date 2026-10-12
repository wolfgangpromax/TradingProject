namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeRiskContext
{
    public required PositionSize PositionSize { get; init; }
    public int TradesToday { get; init; }
    public int ActiveTrades { get; init; }
    public decimal DailyRealizedR { get; init; }
}