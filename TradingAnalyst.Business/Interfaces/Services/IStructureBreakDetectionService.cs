using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Domain.ValueObjects;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IStructureBreakDetectionService
{
    IReadOnlyCollection<StructureBreak> DetectBreaks(
        IReadOnlyCollection<Candle> candles,
        IReadOnlyCollection<MarketStructurePoint> structurePoints,
        MarketTrend trend,
        IReadOnlySet<StructureLevelKey>? consumedLevels = null);

    StructureBreak? DetectBreak(Candle candle, MarketStructureState state);
}