using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class MarketStructureSnapshot
{
    public DateTime Time { get; set; }
    public MarketTrend Trend { get; set; }
    public IReadOnlyCollection<SwingPoint> Swings { get; set; }
        = [];
    public IReadOnlyCollection<MarketStructurePoint> StructurePoints { get; set; }
        = [];
    public IReadOnlyCollection<StructureBreak> StructureBreaks { get; set; }
        = [];
}