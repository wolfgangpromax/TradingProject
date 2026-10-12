using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface ITradeExecutionService
{
    TradeOrder CreateOrder(TradeRiskAssessment riskAssessment);
    TradeExecution Execute(TradeOrder order, Candle candle);
}