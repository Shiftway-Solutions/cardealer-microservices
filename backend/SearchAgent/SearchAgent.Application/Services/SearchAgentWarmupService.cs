using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SearchAgent.Application.Features.Search.Queries;

namespace SearchAgent.Application.Services;

/// <summary>
/// Background service that pre-warms the Claude API connection on startup.
///
/// Why: The first HTTP request to the Anthropic API after a cold start incurs:
///   1. TCP connection establishment (~200–500 ms)
///   2. TLS handshake (~100–300 ms)
///   3. Anthropic-side system-prompt caching (ephemeral, ~3-6 s for first write)
///
/// By sending a trivial warmup query 12 seconds after startup, real user requests
/// always hit a pre-cached system prompt and a live TCP connection, keeping P99
/// latency under 3 s instead of 5–14 s on cold first call.
/// </summary>
public class SearchAgentWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SearchAgentWarmupService> _logger;

    public SearchAgentWarmupService(
        IServiceProvider serviceProvider,
        ILogger<SearchAgentWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to be fully initialized before making external calls
        await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken);

        if (stoppingToken.IsCancellationRequested)
            return;

        _logger.LogInformation("[Warmup] Pre-warming Claude API connection and system-prompt cache...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(new ProcessSearchQuery(
                Query: "Toyota",
                SessionId: "warmup",
                Page: 1,
                PageSize: 8,
                UserId: null,
                IpAddress: "127.0.0.1"
            ), stoppingToken);

            _logger.LogInformation("[Warmup] Claude API warm-up completed — system prompt cached, TCP connection live.");
        }
        catch (OperationCanceledException)
        {
            // Service is stopping — normal shutdown, not an error
        }
        catch (Exception ex)
        {
            // Non-critical: warmup failure does NOT prevent service from serving requests.
            // First real user request will still work, with higher latency.
            _logger.LogWarning(ex, "[Warmup] Could not pre-warm Claude API (non-critical). First real request may be slower.");
        }
    }
}
