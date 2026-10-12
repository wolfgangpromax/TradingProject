using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IPositionSizingService
{
    PositionSize Calculate(TradeSetup setup, decimal accountEquity);
}