using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IMarketStructureAnalysisService
{
    MarketStructureAnalysis Analyze(IReadOnlyCollection<Candle> candles, int swingStrength = 2);
}