using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Repositories;

public interface IMarketSymbolRepository
{
    Task<IReadOnlyCollection<MarketSymbol>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<MarketSymbol?> GetBySymbolAsync(string exchange, string symbol, CancellationToken cancellationToken = default);
    Task<MarketSymbol> UpsertAsync(MarketSymbol marketSymbol, CancellationToken cancellationToken = default);
}