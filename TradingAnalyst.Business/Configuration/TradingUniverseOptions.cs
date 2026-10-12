using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Configuration;

public sealed class TradingUniverseOptions
{
    public const string SectionName = "TradingUniverse";
    public string Exchange { get; set; } = "BINANCE";
    public List<string> Symbols { get; set; } = [];
    public List<Timeframe> Timeframes { get; set; } = [];
    public int RefreshIntervalSeconds { get; set; } = 60;
}