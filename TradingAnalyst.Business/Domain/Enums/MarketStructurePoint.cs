using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class MarketStructurePoint
{
    public DateTime OpenTime { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public decimal Price { get; set; }
    public SwingType SwingType { get; set; }
    public MarketStructureType StructureType { get; set; }
    public int CandleIndex { get; set; }
}