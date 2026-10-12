using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapMitigationService
{
    void Update(FairValueGapState state, Candle candle);
}