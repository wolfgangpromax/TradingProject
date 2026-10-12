using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.MarketData;
using TradingAnalyst.Data.MarketData.Dtos.Binance;

namespace TradingAnalyst.Data.MarketData.Providers;

internal sealed class BinanceMarketDataProvider : IMarketDataProvider
{
    private const string ExchangeName = "BINANCE";

    private readonly HttpClient _httpClient;

    public BinanceMarketDataProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<MarketSymbol>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<BinanceExchangeInfoDto>("/api/v3/exchangeInfo",cancellationToken);

        if (response?.Symbols is null)
        {
            return [];
        }

        return response.Symbols
            .Where(x =>
                x.QuoteAsset.Equals(
                    "USDT",
                    StringComparison.OrdinalIgnoreCase))
            .Select(x => new MarketSymbol
            {
                Exchange = ExchangeName,
                Symbol = x.Symbol,
                BaseAsset = x.BaseAsset,
                QuoteAsset = x.QuoteAsset,

                PricePrecision = ToByte(x.QuoteAssetPrecision),
                QuantityPrecision = ToByte(x.BaseAssetPrecision),

                IsActive = x.Status.Equals(
                    "TRADING",
                    StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    public async Task<IReadOnlyCollection<Candle>> GetCandlesAsync(string symbol, Timeframe timeframe, DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Binance candle limit must be between 1 and 1000.");
        }

        var interval = ConvertTimeframe(timeframe);

        var parameters = new List<string>
        {
            $"symbol={Uri.EscapeDataString(symbol.ToUpperInvariant())}",
            $"interval={interval}"
        };

        if (startTime.HasValue)
        {
            parameters.Add(
                $"startTime={ToUnixMilliseconds(startTime.Value)}");
        }

        if (endTime.HasValue)
        {
            parameters.Add(
                $"endTime={ToUnixMilliseconds(endTime.Value)}");
        }

        if (limit.HasValue)
        {
            parameters.Add($"limit={limit.Value}");
        }

        var url =
            $"/api/v3/klines?{string.Join("&", parameters)}";

        using var response =await _httpClient.GetAsync(url,cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document =await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var candles = new List<Candle>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var openTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()).UtcDateTime;
            var closeTime = DateTimeOffset.FromUnixTimeMilliseconds(item[6].GetInt64()).UtcDateTime;

            candles.Add(new Candle
            {
                Timeframe = timeframe,

                OpenTime = openTime,
                CloseTime = closeTime,

                Open = ParseDecimal(item[1]),
                High = ParseDecimal(item[2]),
                Low = ParseDecimal(item[3]),
                Close = ParseDecimal(item[4]),
                Volume = ParseDecimal(item[5]),

                IsClosed = DateTime.UtcNow > closeTime
            });
        }

        return candles;
    }

    private static string ConvertTimeframe(
        Timeframe timeframe)
    {
        return timeframe switch
        {
            Timeframe.OneMinute => "1m",
            Timeframe.ThreeMinutes => "3m",
            Timeframe.FiveMinutes => "5m",
            Timeframe.FifteenMinutes => "15m",
            Timeframe.ThirtyMinutes => "30m",

            Timeframe.OneHour => "1h",
            Timeframe.FourHours => "4h",
            Timeframe.OneDay => "1d",

            _ => throw new ArgumentOutOfRangeException(
                nameof(timeframe),
                timeframe,
                "Unsupported Binance timeframe.")
        };
    }

    private static decimal ParseDecimal(
        JsonElement element)
    {
        return decimal.Parse(
            element.GetString()
                ?? throw new InvalidOperationException(
                    "Binance returned an invalid numeric value."),
            CultureInfo.InvariantCulture);
    }

    private static long ToUnixMilliseconds(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(value, DateTimeKind.Utc),

            _ => value
        };

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    private static byte? ToByte(int value)
    {
        if (value < byte.MinValue ||
            value > byte.MaxValue)
        {
            return null;
        }

        return (byte)value;
    }
}