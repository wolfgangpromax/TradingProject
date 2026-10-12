using System.Data;

namespace TradingAnalyst.Data.Persistence.Connection;

internal interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}