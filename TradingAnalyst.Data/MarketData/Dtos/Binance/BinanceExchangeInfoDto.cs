using System.Text.Json.Serialization;

namespace TradingAnalyst.Data.MarketData.Dtos.Binance;

internal sealed class BinanceExchangeInfoDto
{
    [JsonPropertyName("symbols")]
    public List<BinanceSymbolDto> Symbols { get; set; } = [];
}