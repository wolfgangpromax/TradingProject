using Microsoft.AspNetCore.Mvc;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Repositories;

namespace TradingAnalyst.Api.Controllers;

[ApiController]
[Route("api/candles")]
public sealed class CandlesController : ControllerBase
{
    private readonly IMarketSymbolRepository _marketSymbolRepository;
    private readonly ICandleRepository _candleRepository;

    public CandlesController(IMarketSymbolRepository marketSymbolRepository, ICandleRepository candleRepository)
    {
        _marketSymbolRepository = marketSymbolRepository;
        _candleRepository = candleRepository;
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> Get(string symbol, [FromQuery] Timeframe timeframe = Timeframe.FiveMinutes, [FromQuery] int limit = 300, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000) return BadRequest("Limit must be between 1 and 5000.");
        var marketSymbol = await _marketSymbolRepository.GetBySymbolAsync("BINANCE", symbol, cancellationToken);
        if (marketSymbol is null) return NotFound($"Symbol '{symbol}' was not found for BINANCE.");
        var candles = await _candleRepository.GetRecentAsync(marketSymbol.MarketSymbolId, timeframe, limit, cancellationToken);
        return Ok(new { marketSymbol.Exchange, marketSymbol.Symbol, Timeframe = timeframe.ToString(), Candles = candles.OrderBy(x => x.OpenTime).ToList() });
    }
}
