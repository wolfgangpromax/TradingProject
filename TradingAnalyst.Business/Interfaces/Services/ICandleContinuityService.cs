using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface ICandleContinuityService
{
    IReadOnlyCollection<CandleGap> FindGaps(IReadOnlyCollection<Candle> candles, Timeframe timeframe);
}