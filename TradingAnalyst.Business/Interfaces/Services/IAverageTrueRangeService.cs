using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IAverageTrueRangeService
{
    decimal? Calculate(IReadOnlyCollection<Candle> candles, int period);
}