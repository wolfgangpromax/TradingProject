using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingAnalyst.Business.Interfaces.MarketData;
using TradingAnalyst.Business.Interfaces.Repositories;
using TradingAnalyst.Data.MarketData.Providers;
using TradingAnalyst.Data.Persistence.Connection;
using TradingAnalyst.Data.Persistence.Repositories;

namespace TradingAnalyst.Data.DependencyInjection;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TradingAnalystDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'TradingAnalystDatabase' was not found.");
        }

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<IMarketSymbolRepository, MarketSymbolRepository>();
        services.AddScoped<ICandleRepository, CandleRepository>();

        services.AddHttpClient<IMarketDataProvider, MexcMarketDataProvider>(client =>
        {
            var baseUrl = configuration["MarketData:Mexc:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "MEXC BaseUrl configuration was not found.");

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<IMarketDataProvider, BinanceMarketDataProvider>(
        client =>
        {
            var baseUrl =
                configuration["MarketData:Binance:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "Binance BaseUrl configuration was not found.");

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}