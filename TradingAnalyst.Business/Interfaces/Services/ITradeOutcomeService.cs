using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface ITradeOutcomeService
{
    TradeOutcome Evaluate(TradeSetup setup, Candle candle);
    void Update(TradeOutcomeState state, Candle candle);
}