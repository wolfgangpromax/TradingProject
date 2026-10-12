using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapDetectionService
{
    IReadOnlyCollection<FairValueGap> Detect(IReadOnlyCollection<Candle> candles);
}