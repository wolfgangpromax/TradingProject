using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.ValueObjects;

public readonly record struct FairValueGapKey(DateTime StartTime, DateTime ConfirmedAt, FairValueGapType Type, decimal LowerPrice, decimal UpperPrice);