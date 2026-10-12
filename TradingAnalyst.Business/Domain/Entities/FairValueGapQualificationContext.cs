using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapQualificationContext
{
    public required DateTime CurrentTime { get; init; }
    public decimal? AverageTrueRange { get; init; }
    public MarketTrend MarketTrend { get; init; } = MarketTrend.Unknown;
}