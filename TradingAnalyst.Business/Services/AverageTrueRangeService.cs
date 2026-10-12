using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class AverageTrueRangeService : IAverageTrueRangeService
{
    public decimal? Calculate(IReadOnlyCollection<Candle> candles, int period)
    {
        ArgumentNullException.ThrowIfNull(candles);

        if (period < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "ATR period must be at least 1.");
        }

        var orderedCandles = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTime)
            .ToList();

        // We need the previous close to calculate each True Range.
        // Therefore period + 1 candles are required.
        if (orderedCandles.Count < period + 1)
        {
            return null;
        }

        var startIndex = orderedCandles.Count - period;

        decimal trueRangeSum = 0m;

        for (var i = startIndex; i < orderedCandles.Count; i++)
        {
            var current = orderedCandles[i];
            var previous = orderedCandles[i - 1];

            var highLow = current.High - current.Low;

            var highPreviousClose = Math.Abs(current.High - previous.Close);

            var lowPreviousClose = Math.Abs(current.Low - previous.Close);

            var trueRange =
                Math.Max(
                    highLow,
                    Math.Max(
                        highPreviousClose,
                        lowPreviousClose));

            trueRangeSum += trueRange;
        }

        return trueRangeSum / period;
    }
}