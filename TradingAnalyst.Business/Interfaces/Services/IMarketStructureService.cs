using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IMarketStructureService
{
    IReadOnlyCollection<MarketStructurePoint> Classify(IReadOnlyCollection<SwingPoint> swings);
}