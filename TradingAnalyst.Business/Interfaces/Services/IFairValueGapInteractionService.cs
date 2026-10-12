using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapInteractionService
{
    FairValueGapInteraction Evaluate(FairValueGapCandidate candidate, Candle candle);
    void Update(FairValueGapInteractionState state, Candle candle);
}