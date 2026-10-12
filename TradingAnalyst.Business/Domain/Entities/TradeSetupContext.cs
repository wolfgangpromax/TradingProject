namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeSetupContext
{
    public required FairValueGapEntryConfirmation EntryConfirmation { get; init; }
    public decimal? AverageTrueRange { get; init; }
}