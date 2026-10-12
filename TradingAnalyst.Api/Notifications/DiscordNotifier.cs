using System.Net.Http.Json;

namespace TradingAnalyst.Api.Notifications;

public interface IDiscordNotifier
{
    bool IsConfigured { get; }
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}

public sealed class DiscordNotifier : IDiscordNotifier
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordNotifier> _logger;

    public DiscordNotifier(HttpClient httpClient, IConfiguration configuration, ILogger<DiscordNotifier> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Notifications:Discord:WebhookUrl"]);

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var webhookUrl = _configuration["Notifications:Discord:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl)) return;
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(webhookUrl, new { content = message }, cancellationToken);
            if (!response.IsSuccessStatusCode) _logger.LogWarning("Discord notification failed with HTTP {StatusCode}.", (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Discord notification failed."); }
    }
}
