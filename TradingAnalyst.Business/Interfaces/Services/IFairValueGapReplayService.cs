using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapReplayService
{
    IReadOnlyCollection<FairValueGapSnapshot> Replay(   IReadOnlyCollection<Candle> candles);
}