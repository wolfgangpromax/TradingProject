using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapQualificationReplayService
{
    IReadOnlyCollection<FairValueGapQualificationSnapshot> Replay(IReadOnlyCollection<Candle> candles, int atrPeriod);
}