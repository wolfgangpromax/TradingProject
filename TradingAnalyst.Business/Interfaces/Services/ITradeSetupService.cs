using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface ITradeSetupService
{
    TradeSetup? Create(TradeSetupContext context);
}