using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapAlignment
{
    public required FairValueGapState State { get; init; }
    public required MarketTrend MarketTrend { get; init; }
    public required bool IsAligned { get; init; }
    public required string Reason { get; init; }
}