using System.Data;
using Dapper;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Data.Persistence.Connection;

namespace TradingAnalyst.Data.Persistence.Repositories;

internal sealed class MarketSymbolRepository : IMarketSymbolRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MarketSymbolRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<MarketSymbol>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            "dbo.uspMarketSymbolGetActive",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<MarketSymbol>(command);

        return result.AsList();
    }

    public async Task<MarketSymbol?> GetBySymbolAsync(string exchange, string symbol, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new
        {
            Exchange = exchange,
            Symbol = symbol
        };

        var command = new CommandDefinition(
            "dbo.uspMarketSymbolGetBySymbol",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<MarketSymbol>(command);
    }

    public async Task<MarketSymbol> UpsertAsync(MarketSymbol marketSymbol, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new
        {
            marketSymbol.Exchange,
            marketSymbol.Symbol,
            marketSymbol.BaseAsset,
            marketSymbol.QuoteAsset,
            marketSymbol.PricePrecision,
            marketSymbol.QuantityPrecision,
            marketSymbol.IsActive
        };

        var command = new CommandDefinition(
            "dbo.uspMarketSymbolUpsert",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<MarketSymbol>(command);
    }
}