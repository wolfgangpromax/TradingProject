using System.Text.Json.Serialization;

namespace TradingAnalyst.Data.MarketData.Dtos.Mexc;

internal sealed class MexcSymbolDto
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("baseAsset")]
    public string BaseAsset { get; set; } = string.Empty;

    [JsonPropertyName("quoteAsset")]
    public string QuoteAsset { get; set; } = string.Empty;

    [JsonPropertyName("baseAssetPrecision")]
    public int? BaseAssetPrecision { get; set; }

    [JsonPropertyName("quotePrecision")]
    public int? QuotePrecision { get; set; }
}