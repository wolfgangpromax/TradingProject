using System.Data;
using Dapper;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Data.Persistence.Connection;

namespace TradingAnalyst.Data.Persistence.Repositories;

internal sealed class CandleRepository : ICandleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CandleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertAsync(Candle candle, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new
        {
            candle.MarketSymbolId,
            Timeframe = (short)candle.Timeframe,
            candle.OpenTime,
            candle.CloseTime,
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.Volume,
            candle.IsClosed
        };

        var command = new CommandDefinition(
            "dbo.uspCandleUpsert",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<IReadOnlyCollection<Candle>> GetRecentAsync(int marketSymbolId, Timeframe timeframe, int limit, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new
        {
            MarketSymbolId = marketSymbolId,
            Timeframe = (short)timeframe,
            Limit = limit
        };

        var command = new CommandDefinition(
            "dbo.uspCandleGetRecent",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<Candle>(command);

        return result.AsList();
    }

    public async Task<Candle?> GetLatestAsync(int marketSymbolId, Timeframe timeframe, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            "dbo.uspCandleGetLatest",
            new
            {
                MarketSymbolId = marketSymbolId,
                Timeframe = (short)timeframe
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Candle>(command);
    }
}