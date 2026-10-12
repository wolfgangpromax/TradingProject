using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IMarketDataService
{
    Task<int> SynchronizeSymbolsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Candle>> SynchronizeCandlesAsync(string exchange, string symbol, Timeframe timeframe, int limit, CancellationToken cancellationToken = default);
    Task<int> SynchronizeIncrementalAsync(string exchange, string symbol, Timeframe timeframe, CancellationToken cancellationToken = default);
    Task<int> RepairCandleGapsAsync(string exchange, string symbol, Timeframe timeframe, int validationLimit = 500, CancellationToken cancellationToken = default);
}