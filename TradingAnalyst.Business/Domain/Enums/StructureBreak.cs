using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class StructureBreak
{
    public StructureBreakType Type { get; set; }
    public BreakDirection Direction { get; set; }
    public decimal BrokenPrice { get; set; }
    public DateTime BrokenStructureTime { get; set; }
    public DateTime BreakTime { get; set; }
    public decimal BreakClosePrice { get; set; }
    public MarketTrend TrendBeforeBreak { get; set; }
    public MarketTrend TrendAfterBreak { get; set; }
}