using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IMarketStructureReplayService
{
    IReadOnlyCollection<MarketStructureSnapshot> Replay(IReadOnlyCollection<Candle> candles, int swingStrength = 2);
}