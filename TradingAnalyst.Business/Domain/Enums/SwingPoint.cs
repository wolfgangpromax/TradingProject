using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;
public sealed class SwingPoint
{
    public DateTime OpenTime { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public SwingType Type { get; set; }
    public decimal Price { get; set; }
    public int CandleIndex { get; set; }
}