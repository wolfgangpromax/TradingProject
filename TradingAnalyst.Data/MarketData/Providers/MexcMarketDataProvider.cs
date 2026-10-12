using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.MarketData;
using TradingAnalyst.Data.MarketData.Dtos.Mexc;

namespace TradingAnalyst.Data.MarketData.Providers;

internal sealed class MexcMarketDataProvider : IMarketDataProvider
{
    private const string ExchangeName = "MEXC";

    private readonly HttpClient _httpClient;

    public MexcMarketDataProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<MarketSymbol>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<MexcExchangeInfoDto>("/api/v3/exchangeInfo", cancellationToken);

        if (response?.Symbols is null)
        {
            return Array.Empty<MarketSymbol>();
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
                PricePrecision = ToByte(x.QuotePrecision),
                QuantityPrecision = ToByte(x.BaseAssetPrecision),
                IsActive = true
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<Candle>> GetCandlesAsync(string symbol, Timeframe timeframe, DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "MEXC candle limit must be between 1 and 1000.");
        }

        var interval = ConvertTimeframe(timeframe);

        var url =
            $"/api/v3/klines" +
            $"?symbol={Uri.EscapeDataString(symbol.ToUpperInvariant())}" +
            $"&interval={interval}" +
            $"&limit={limit}";

        using var response = await _httpClient.GetAsync(url,cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var candles = new List<Candle>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var openTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()).UtcDateTime;
            var closeTime = DateTimeOffset.FromUnixTimeMilliseconds(item[6].GetInt64()).UtcDateTime;

            candles.Add(new Candle
            {
                OpenTime = openTime,
                CloseTime = closeTime,

                Open = ParseDecimal(item[1]),
                High = ParseDecimal(item[2]),
                Low = ParseDecimal(item[3]),
                Close = ParseDecimal(item[4]),
                Volume = ParseDecimal(item[5]),

                Timeframe = timeframe,

                IsClosed = DateTime.UtcNow > closeTime
            });
        }

        return candles;
    }

    private static string ConvertTimeframe(Timeframe timeframe)
    {
        return timeframe switch
        {
            Timeframe.OneMinute => "1m",
            Timeframe.ThreeMinutes => "3m",
            Timeframe.FiveMinutes => "5m",
            Timeframe.FifteenMinutes => "15m",
            Timeframe.ThirtyMinutes => "30m",
            Timeframe.OneHour => "60m",
            Timeframe.FourHours => "4h",
            Timeframe.OneDay => "1d",

            _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported MEXC timeframe.")
        };
    }

    private static decimal ParseDecimal(JsonElement element)
    {
        return decimal.Parse(element.GetString()
                ?? throw new InvalidOperationException(
                    "MEXC returned an invalid numeric value."),
            CultureInfo.InvariantCulture);
    }

    private static byte? ToByte(int? value)
    {
        if (!value.HasValue ||
            value.Value < byte.MinValue ||
            value.Value > byte.MaxValue)
        {
            return null;
        }

        return (byte)value.Value;
    }
}