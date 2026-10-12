using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class MarketStructureAnalysisService
    : IMarketStructureAnalysisService
{
    private readonly ISwingDetectionService _swingDetectionService;
    private readonly IMarketStructureService _marketStructureService;
    private readonly IMarketTrendService _marketTrendService;
    private readonly IStructureBreakDetectionService _structureBreakDetectionService;

    public MarketStructureAnalysisService(ISwingDetectionService swingDetectionService, IMarketStructureService marketStructureService,
        IMarketTrendService marketTrendService, IStructureBreakDetectionService structureBreakDetectionService)
    {
        _swingDetectionService = swingDetectionService;
        _marketStructureService = marketStructureService;
        _marketTrendService = marketTrendService;
        _structureBreakDetectionService = structureBreakDetectionService;
    }

    public MarketStructureAnalysis Analyze(IReadOnlyCollection<Candle> candles, int swingStrength = 2)
    {
        if (swingStrength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(swingStrength), "Swing strength must be at least 1.");
        }

        if (candles.Count == 0)
        {
            return new MarketStructureAnalysis
            {
                Trend = MarketTrend.Unknown
            };
        }

        var closedCandles = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTime)
            .ToList();

        if (closedCandles.Count == 0)
        {
            return new MarketStructureAnalysis
            {
                Trend = MarketTrend.Unknown
            };
        }

        var swings = _swingDetectionService.DetectSwings(closedCandles, swingStrength);

        var structurePoints = _marketStructureService.Classify(swings);

        var trend = _marketTrendService.DetermineTrend(structurePoints);

        var structureBreaks = _structureBreakDetectionService.DetectBreaks(closedCandles, structurePoints, trend);

        return new MarketStructureAnalysis
        {
            Trend = trend,
            Swings = swings,
            StructurePoints = structurePoints,
            StructureBreaks = structureBreaks,
            AnalysisTime = closedCandles
                .Max(x => x.CloseTime)
        };
    }
}