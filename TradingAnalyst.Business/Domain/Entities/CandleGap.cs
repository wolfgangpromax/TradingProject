namespace TradingAnalyst.Business.Domain.Entities;

public sealed class CandleGap
{
    public DateTime ExpectedOpenTime { get; set; }
    public DateTime NextAvailableOpenTime { get; set; }
    public int MissingCandleCount { get; set; }
}