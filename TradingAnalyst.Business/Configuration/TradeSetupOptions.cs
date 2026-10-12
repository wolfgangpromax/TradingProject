namespace TradingAnalyst.Business.Configuration;

public sealed class TradeSetupOptions
{
    public decimal StopLossAtrBufferMultiplier { get; set; } = 0.10m;
    public decimal MinimumRiskRewardRatio { get; set; } = 2.00m;
}