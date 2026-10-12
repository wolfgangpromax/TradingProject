namespace TradingAnalyst.Api.Scanning;

public sealed class ScannerOptions
{
    public const string SectionName = "Scanner";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    public int CandleLimit { get; set; } = 500;
    public List<string> Symbols { get; set; } = [];
}
