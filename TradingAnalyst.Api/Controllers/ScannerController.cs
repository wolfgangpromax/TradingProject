using Microsoft.AspNetCore.Mvc;
using TradingAnalyst.Api.Notifications;
using TradingAnalyst.Api.Scanning;

namespace TradingAnalyst.Api.Controllers;

[ApiController]
[Route("api/scanner")]
public sealed class ScannerController : ControllerBase
{
    private readonly ScannerStateStore _store;
    private readonly IDiscordNotifier _discord;
    public ScannerController(ScannerStateStore store, IDiscordNotifier discord) { _store = store; _discord = discord; }

    [HttpGet]
    public IActionResult Get() => Ok(new { DiscordConfigured = _discord.IsConfigured, Rows = _store.GetAll() });

    [HttpPost("discord/test")]
    public async Task<IActionResult> TestDiscord(CancellationToken cancellationToken)
    {
        if (!_discord.IsConfigured) return BadRequest("Discord webhook is not configured. Add Notifications:Discord:WebhookUrl to User Secrets.");
        await _discord.SendAsync("✅ **TradingAnalyst Discord notifier connected.**\nYour local scanner can send setup-state notifications to this channel.", cancellationToken);
        return Ok(new { Sent = true });
    }
}
