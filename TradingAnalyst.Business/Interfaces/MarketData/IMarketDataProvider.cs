using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Interfaces.MarketData;

public interface IMarketDataProvider
{
    Task<IReadOnlyCollection<MarketSymbol>> GetSymbolsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Candle>> GetCandlesAsync(
        string symbol,
        Timeframe timeframe,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
}