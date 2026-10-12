using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapAlignmentService
{
    FairValueGapAlignment Evaluate(FairValueGapState state, MarketTrend marketTrend);
}