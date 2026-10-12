using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IMarketTrendService
{
    MarketTrend DetermineTrend(IReadOnlyCollection<MarketStructurePoint> structurePoints);
}