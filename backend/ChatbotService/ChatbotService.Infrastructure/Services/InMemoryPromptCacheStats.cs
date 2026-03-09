using ChatbotService.Domain.Interfaces;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// R17-PC: Thread-safe in-memory accumulator for Anthropic prompt cache token usage.
/// Uses Interlocked operations for lock-free, high-throughput updates.
///
/// This shadows the OpenTelemetry counters in ChatbotMetrics to enable a readable
/// REST endpoint (/api/chat/metrics/prompt-cache) that reports cost-savings vs the 60% target.
///
/// Lifetime: Singleton — accumulated since service startup.
/// Reset: On service restart (use external monitoring/dashboards for persistence).
/// </summary>
public sealed class InMemoryPromptCacheStats : IPromptCacheStats
{
    private long _cacheReadTokens;
    private long _cacheWriteTokens;
    private long _totalInputTokens;
    private long _totalLlmCalls;
    private long _lastCallAtTicks;   // DateTimeOffset.UtcNow.Ticks

    public void RecordCall(long cacheReadTokens, long cacheWriteTokens, long totalInputTokens)
    {
        Interlocked.Add(ref _cacheReadTokens, cacheReadTokens);
        Interlocked.Add(ref _cacheWriteTokens, cacheWriteTokens);
        Interlocked.Add(ref _totalInputTokens, totalInputTokens);
        Interlocked.Increment(ref _totalLlmCalls);
        Interlocked.Exchange(ref _lastCallAtTicks, DateTimeOffset.UtcNow.Ticks);
    }

    public PromptCacheReport GetReport()
    {
        var lastCallTicks = Interlocked.Read(ref _lastCallAtTicks);
        return new PromptCacheReport
        {
            TotalLlmCalls = Interlocked.Read(ref _totalLlmCalls),
            TotalInputTokens = Interlocked.Read(ref _totalInputTokens),
            CacheReadTokens = Interlocked.Read(ref _cacheReadTokens),
            CacheWriteTokens = Interlocked.Read(ref _cacheWriteTokens),
            LastCallAt = lastCallTicks > 0 ? new DateTimeOffset(lastCallTicks, TimeSpan.Zero) : null,
            MeasuredAt = DateTimeOffset.UtcNow
        };
    }
}
