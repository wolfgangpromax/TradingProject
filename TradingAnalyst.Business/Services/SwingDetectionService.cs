using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class SwingDetectionService : ISwingDetectionService
{
    public IReadOnlyCollection<SwingPoint> DetectSwings(IReadOnlyCollection<Candle> candles, int strength = 2)
    {
        if (strength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(strength),"Strength must be at least 1.");
        }

        if (candles.Count < (strength * 2) + 1)
        {
            return [];
        }

        var orderedCandles = candles.OrderBy(x => x.OpenTime).ToList();

        var swings = new List<SwingPoint>();

        for (var i = strength; i < orderedCandles.Count - strength; i++)
        {
            var candidate = orderedCandles[i];

            var isSwingHigh = true;
            var isSwingLow = true;

            for (var offset = 1; offset <= strength; offset++)
            {
                var left = orderedCandles[i - offset];
                var right = orderedCandles[i + offset];

                if (candidate.High <= left.High ||
                    candidate.High <= right.High)
                {
                    isSwingHigh = false;
                }

                if (candidate.Low >= left.Low ||
                    candidate.Low >= right.Low)
                {
                    isSwingLow = false;
                }

                if (!isSwingHigh && !isSwingLow)
                {
                    break;
                }
            }

            if (isSwingHigh)
            {
                swings.Add(new SwingPoint
                {
                    OpenTime = candidate.OpenTime,
                    ConfirmedAt = orderedCandles[i + strength].CloseTime,
                    Type = SwingType.High,
                    Price = candidate.High,
                    CandleIndex = i
                });
            }

            if (isSwingLow)
            {
                swings.Add(new SwingPoint
                {
                    OpenTime = candidate.OpenTime,
                    ConfirmedAt = orderedCandles[i + strength].CloseTime,
                    Type = SwingType.Low,
                    Price = candidate.Low,
                    CandleIndex = i
                });
            }
        }

        return swings;
    }
}