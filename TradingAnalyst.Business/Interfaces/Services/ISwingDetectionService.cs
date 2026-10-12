using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface ISwingDetectionService
{
    IReadOnlyCollection<SwingPoint> DetectSwings(IReadOnlyCollection<Candle> candles, int strength = 2);
}