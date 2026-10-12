namespace TradingAnalyst.Business.Configuration;

public sealed class RiskManagementOptions
{
    public decimal InitialAccountEquity { get; set; } = 10_000m;
    public decimal RiskPerTradePercentage { get; set; } = 1.00m;
    public decimal MaximumDailyLossR { get; set; } = 3.00m;
    public int MaximumTradesPerDay { get; set; } = 5;
    public int MaximumActiveTrades { get; set; } = 1;
}