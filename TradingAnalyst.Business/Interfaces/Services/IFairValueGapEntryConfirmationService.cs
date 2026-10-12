using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapEntryConfirmationService
{
    FairValueGapEntryConfirmation Evaluate(FairValueGapInteractionState interaction, Candle candle);
    void Update(FairValueGapEntryConfirmationState state, Candle candle);
}