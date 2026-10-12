using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Domain.ValueObjects;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGap
{
    public required FairValueGapType Type { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime ConfirmedAt { get; init; }
    public required decimal LowerPrice { get; init; }
    public required decimal UpperPrice { get; init; }
    public required decimal GapSize { get; init; }
    public FairValueGapKey Key => new(StartTime, ConfirmedAt, Type, LowerPrice, UpperPrice);
}