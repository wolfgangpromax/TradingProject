using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Api.Workers;

public sealed class MarketDataWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TradingUniverseOptions _options;
    private readonly ILogger<MarketDataWorker> _logger;

    public MarketDataWorker(IServiceScopeFactory scopeFactory,IOptions<TradingUniverseOptions> options,ILogger<MarketDataWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market data worker started.");

        // Synchronize immediately when the application starts.
        await SynchronizeAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.RefreshIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SynchronizeAsync(stoppingToken);
        }
    }

    private async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        foreach (var symbol in _options.Symbols)
        {
            foreach (var timeframe in _options.Timeframes)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var marketDataService = scope.ServiceProvider.GetRequiredService<IMarketDataService>();

                    var count = await marketDataService.SynchronizeIncrementalAsync(
                                _options.Exchange,
                                symbol,
                                timeframe,
                                cancellationToken);
                    var repairedCount = await marketDataService.RepairCandleGapsAsync(_options.Exchange, symbol, timeframe, validationLimit: 500, cancellationToken);

                    _logger.LogInformation(
                        "Synchronized {Count} candles and repaired {RepairedCount} gaps for {Symbol} {Timeframe}.",
                        count, repairedCount, symbol, timeframe);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Failed to synchronize {Symbol} {Timeframe}.", symbol, timeframe);
                }
            }
        }
    }
}