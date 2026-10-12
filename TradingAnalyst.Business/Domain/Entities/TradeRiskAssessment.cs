namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeRiskAssessment
{
    public required PositionSize PositionSize { get; init; }
    public bool IsApproved { get; init; }
    public int TradesToday { get; init; }
    public int ActiveTrades { get; init; }
    public decimal DailyRealizedR { get; init; }
    public required IReadOnlyCollection<string> RejectionReasons { get; init; }
}