using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class CandleContinuityService : ICandleContinuityService
{
    public IReadOnlyCollection<CandleGap> FindGaps(IReadOnlyCollection<Candle> candles,Timeframe timeframe)
    {
        if (candles.Count < 2)
        {
            return [];
        }

        var interval = GetInterval(timeframe);

        var orderedCandles = candles.OrderBy(x => x.OpenTime).ToList();

        var gaps = new List<CandleGap>();

        for (var i = 1; i < orderedCandles.Count; i++)
        {
            var previous = orderedCandles[i - 1];
            var current = orderedCandles[i];

            var expectedOpenTime = previous.OpenTime.Add(interval);

            if (current.OpenTime <= expectedOpenTime)
            {
                continue;
            }

            var difference = current.OpenTime - expectedOpenTime;

            var missingCount = (int)(difference.Ticks / interval.Ticks);

            gaps.Add(new CandleGap
            {
                ExpectedOpenTime = expectedOpenTime,
                NextAvailableOpenTime = current.OpenTime,
                MissingCandleCount = missingCount
            });
        }

        return gaps;
    }

    private static TimeSpan GetInterval(Timeframe timeframe)
    {
        return timeframe switch
        {
            Timeframe.OneMinute => TimeSpan.FromMinutes(1),
            Timeframe.ThreeMinutes => TimeSpan.FromMinutes(3),
            Timeframe.FiveMinutes => TimeSpan.FromMinutes(5),
            Timeframe.FifteenMinutes => TimeSpan.FromMinutes(15),
            Timeframe.ThirtyMinutes => TimeSpan.FromMinutes(30),
            Timeframe.OneHour => TimeSpan.FromHours(1),
            Timeframe.FourHours => TimeSpan.FromHours(4),
            Timeframe.OneDay => TimeSpan.FromDays(1),

            _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported timeframe.")
        };
    }
}