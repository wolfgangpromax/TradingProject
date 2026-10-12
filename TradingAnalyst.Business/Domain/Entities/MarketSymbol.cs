namespace TradingAnalyst.Business.Domain.Entities;

public sealed class MarketSymbol
{
    public int MarketSymbolId { get; set; }
    public string Exchange { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;
    public byte? PricePrecision { get; set; }
    public byte? QuantityPrecision { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}