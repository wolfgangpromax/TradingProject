namespace TradingAnalyst.Business.Configuration;

public sealed class FairValueGapQualificationOptions
{
    public decimal MinimumGapAtrRatio { get; set; } = 0.10m;
}