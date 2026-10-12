using System.Text.Json.Serialization;

namespace TradingAnalyst.Data.MarketData.Dtos.Binance;

internal sealed class BinanceSymbolDto
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("baseAsset")]
    public string BaseAsset { get; set; } = string.Empty;

    [JsonPropertyName("quoteAsset")]
    public string QuoteAsset { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("baseAssetPrecision")]
    public int BaseAssetPrecision { get; set; }

    [JsonPropertyName("quoteAssetPrecision")]
    public int QuoteAssetPrecision { get; set; }
}