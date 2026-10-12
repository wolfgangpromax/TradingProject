using System.Text.Json.Serialization;

namespace TradingAnalyst.Data.MarketData.Dtos.Mexc;

internal sealed class MexcExchangeInfoDto
{
    [JsonPropertyName("symbols")]
    public List<MexcSymbolDto> Symbols { get; set; } = [];
}