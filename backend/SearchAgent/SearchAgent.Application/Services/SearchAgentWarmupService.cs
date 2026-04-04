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
///
/// Retry strategy: retries up to 3 times with 15 s intervals.
/// This handles the common case where the DB (SearchAgentConfig) is not ready
/// when the container first starts (EF Core migration may still be running).
/// </summary>
public class SearchAgentWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SearchAgentWarmupService> _logger;

    private const int MaxAttempts = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

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
        await Task.Delay(InitialDelay, stoppingToken);

        if (stoppingToken.IsCancellationRequested)
            return;

        _logger.LogInformation("[Warmup] Pre-warming Claude API connection and system-prompt cache...");

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
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

                _logger.LogInformation(
                    "[Warmup] Claude API warm-up completed on attempt {Attempt} — system prompt cached, TCP connection live.",
                    attempt);
                return; // Success — exit retry loop
            }
            catch (OperationCanceledException)
            {
                // Service is stopping — normal shutdown, not an error
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex,
                    "[Warmup] Attempt {Attempt}/{Max} failed (likely DB not ready). Retrying in {Delay}s...",
                    attempt, MaxAttempts, RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (Exception ex)
            {
                // All attempts exhausted — non-critical, first real user request will be slower
                _logger.LogWarning(ex,
                    "[Warmup] All {Max} warmup attempts failed. First real request may be slower.",
                    MaxAttempts);
            }
        }
    }
}
