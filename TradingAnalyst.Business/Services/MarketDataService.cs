using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.MarketData;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class MarketDataService : IMarketDataService
{
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly IMarketSymbolRepository _marketSymbolRepository;
    private readonly ICandleRepository _candleRepository;
    private readonly ICandleContinuityService _candleContinuityService;

    public MarketDataService(IMarketDataProvider marketDataProvider, IMarketSymbolRepository marketSymbolRepository, ICandleRepository candleRepository,
        ICandleContinuityService candleContinuityService)
    {
        _marketDataProvider = marketDataProvider;
        _marketSymbolRepository = marketSymbolRepository;
        _candleRepository = candleRepository;
        _candleContinuityService = candleContinuityService;
    }

    public async Task<int> SynchronizeSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var symbols = await _marketDataProvider.GetSymbolsAsync(cancellationToken);

        var synchronizedCount = 0;

        foreach (var symbol in symbols)
        {
            await _marketSymbolRepository.UpsertAsync(symbol, cancellationToken);
            synchronizedCount++;
        }

        return synchronizedCount;
    }

    public async Task<IReadOnlyCollection<Candle>> SynchronizeCandlesAsync(string exchange, string symbol, Timeframe timeframe, int limit, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");
        }

        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync(exchange, symbol, cancellationToken);

        if (marketSymbol is null)
        {
            throw new InvalidOperationException($"Market symbol '{exchange}:{symbol}' was not found.");
        }

        var candles = await _marketDataProvider.GetCandlesAsync(symbol, timeframe, startTime: null, endTime: null, limit: limit, cancellationToken);
        
        await SaveCandlesAsync( marketSymbol.MarketSymbolId, candles, cancellationToken);

        return candles;
    }

    public async Task<int> SynchronizeIncrementalAsync(string exchange, string symbol, Timeframe timeframe, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        const int bootstrapLimit = 500;
        const int pageLimit = 1000;

        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync(exchange, symbol, cancellationToken);

        if (marketSymbol is null)
        {
            throw new InvalidOperationException($"Market symbol '{exchange}:{symbol}' was not found.");
        }

        var latestCandle = await _candleRepository.GetLatestAsync(marketSymbol.MarketSymbolId, timeframe, cancellationToken);

        // Nothing stored yet.
        if (latestCandle is null)
        {
            var bootstrapCandles = await _marketDataProvider.GetCandlesAsync(symbol, timeframe, startTime: null, endTime: null, limit: bootstrapLimit, cancellationToken);

            await SaveCandlesAsync(marketSymbol.MarketSymbolId, bootstrapCandles, cancellationToken);

            return bootstrapCandles.Count;
        }

        var totalSynchronized = 0;

        // Re-request the latest stored candle intentionally because it
        // may still be the currently forming candle.
        var nextStartTime = latestCandle.OpenTime;

        while (!cancellationToken.IsCancellationRequested)
        {
            var candles = await _marketDataProvider.GetCandlesAsync(symbol, timeframe, startTime: nextStartTime, endTime: null, limit: pageLimit, cancellationToken);

            if (candles.Count == 0)
            {
                break;
            }

            await SaveCandlesAsync(marketSymbol.MarketSymbolId, candles, cancellationToken);

            totalSynchronized += candles.Count;

            // Less than the maximum page size means Binance has returned
            // everything currently available.
            if (candles.Count < pageLimit)
            {
                break;
            }

            var lastCandle = candles.OrderBy(x => x.OpenTime).Last();

            var newStartTime =GetNextOpenTime(lastCandle.OpenTime, timeframe);

            // Defensive protection against an API/provider returning
            // the same page repeatedly.
            if (newStartTime <= nextStartTime)
            {
                throw new InvalidOperationException($"Candle synchronization did not advance for " + $"{symbol} {timeframe}.");
            }

            nextStartTime = newStartTime;
        }

        return totalSynchronized;
    }

    private async Task SaveCandlesAsync(int marketSymbolId, IReadOnlyCollection<Candle> candles, CancellationToken cancellationToken)
    {
        foreach (var candle in candles)
        {
            candle.MarketSymbolId = marketSymbolId;
            await _candleRepository.UpsertAsync(candle, cancellationToken);
        }
    }

    private static DateTime GetNextOpenTime(DateTime openTime, Timeframe timeframe)
    {
        return timeframe switch
        {
            Timeframe.OneMinute => openTime.AddMinutes(1),
            Timeframe.ThreeMinutes => openTime.AddMinutes(3),
            Timeframe.FiveMinutes => openTime.AddMinutes(5),
            Timeframe.FifteenMinutes => openTime.AddMinutes(15),
            Timeframe.ThirtyMinutes => openTime.AddMinutes(30),
            Timeframe.OneHour => openTime.AddHours(1),
            Timeframe.FourHours => openTime.AddHours(4),
            Timeframe.OneDay => openTime.AddDays(1),

            _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported timeframe.")
        };
    }
    public async Task<int> RepairCandleGapsAsync(string exchange, string symbol, Timeframe timeframe, int validationLimit = 500, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (validationLimit <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(validationLimit),"Validation limit must be greater than 1.");
        }

        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync(exchange, symbol, cancellationToken);

        if (marketSymbol is null)
        {
            throw new InvalidOperationException($"Market symbol '{exchange}:{symbol}' was not found.");
        }

        var candles = await _candleRepository.GetRecentAsync( marketSymbol.MarketSymbolId, timeframe, validationLimit, cancellationToken);

        if (candles.Count < 2)
        {
            return 0;
        }

        var gaps = _candleContinuityService.FindGaps(candles, timeframe);

        if (gaps.Count == 0)
        {
            return 0;
        }

        var repairedCount = 0;

        foreach (var gap in gaps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var missingCandles = await _marketDataProvider.GetCandlesAsync(
                                            symbol,
                                            timeframe,
                                            startTime: gap.ExpectedOpenTime,
                                            endTime: gap.NextAvailableOpenTime,
                                            limit: Math.Min(
                                                gap.MissingCandleCount,
                                                1000),
                                            cancellationToken);

            var candlesToSave = missingCandles
                .Where(x =>
                    x.OpenTime >= gap.ExpectedOpenTime &&
                    x.OpenTime < gap.NextAvailableOpenTime)
                .ToList();

            if (candlesToSave.Count == 0)
            {
                continue;
            }

            await SaveCandlesAsync(marketSymbol.MarketSymbolId, candlesToSave, cancellationToken);

            repairedCount += candlesToSave.Count;
        }

        return repairedCount;
    }
}