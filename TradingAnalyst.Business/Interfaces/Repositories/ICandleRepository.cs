using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Interfaces.Repositories;

public interface ICandleRepository
{
    Task UpsertAsync(Candle candle, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Candle>> GetRecentAsync(int marketSymbolId, Timeframe timeframe, int limit, CancellationToken cancellationToken = default);
    Task<Candle?> GetLatestAsync(int marketSymbolId, Timeframe timeframe, CancellationToken cancellationToken = default);
}