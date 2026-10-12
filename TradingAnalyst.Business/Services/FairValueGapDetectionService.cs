using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapDetectionService
    : IFairValueGapDetectionService
{
    public IReadOnlyCollection<FairValueGap> Detect(IReadOnlyCollection<Candle> candles)
    {
        if (candles.Count < 3)
        {
            return [];
        }

        var orderedCandles = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTime)
            .ToList();

        if (orderedCandles.Count < 3)
        {
            return [];
        }

        var gaps = new List<FairValueGap>();

        for (var i = 2; i < orderedCandles.Count; i++)
        {
            var candle1 = orderedCandles[i - 2];
            var candle3 = orderedCandles[i];

            if (candle3.Low > candle1.High)
            {
                gaps.Add(new FairValueGap
                {
                    Type = FairValueGapType.Bullish,

                    StartTime = candle1.OpenTime,
                    ConfirmedAt = candle3.CloseTime,

                    LowerPrice = candle1.High,
                    UpperPrice = candle3.Low,

                    GapSize =
                        candle3.Low -
                        candle1.High
                });

                continue;
            }

            if (candle3.High < candle1.Low)
            {
                gaps.Add(new FairValueGap
                {
                    Type = FairValueGapType.Bearish,

                    StartTime = candle1.OpenTime,
                    ConfirmedAt = candle3.CloseTime,

                    LowerPrice = candle3.High,
                    UpperPrice = candle1.Low,

                    GapSize =
                        candle1.Low -
                        candle3.High
                });
            }
        }

        return gaps;
    }
}