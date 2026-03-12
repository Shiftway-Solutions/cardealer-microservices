// =============================================================================
// LLM Gateway Service — Cascade Fallback Orchestrator
//
// Execution flow:
//   1. Check Redis cache (if not skipped)
//   2. Try Claude (primary, level 0)
//   3. On 429/500/503 → Try Gemini within 500ms (level 1)
//   4. On Gemini fail → Try Llama within 500ms (level 2)
//   5. On all fail → Serve from Redis cache (degraded mode, level 3)
//   6. On cache miss → Return static degraded response
//
// Every fallback event is logged with:
//   - Agent affected
//   - Primary model that failed
//   - Fallback model activated
//   - Fallback level reached
//   - Total redirection latency
//   - Timestamp
// =============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarDealer.Shared.LlmGateway.Abstractions;
using CarDealer.Shared.LlmGateway.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CarDealer.Shared.LlmGateway.Services;

/// <summary>
/// Central LLM Gateway with cascading fallback: Claude → Gemini → Llama → Redis Cache.
/// </summary>
public sealed class LlmGatewayService : ILlmGateway
{
    private readonly IReadOnlyList<ILlmProvider> _providers;
    private readonly LlmGatewayOptions _options;
    private readonly ILogger<LlmGatewayService> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IApiCostTracker? _costTracker;

    // Metrics counters (thread-safe)
    private static long _claudeCount;
    private static long _geminiCount;
    private static long _llamaCount;
    private static long _cacheCount;
    private static long _totalCount;

    // Thread-safe RNG for traffic splitting
    private static readonly Random _trafficRng = new();

    public LlmGatewayService(
        IEnumerable<ILlmProvider> providers,
        IOptions<LlmGatewayOptions> options,
        ILogger<LlmGatewayService> logger,
        IConnectionMultiplexer? redis = null,
        IApiCostTracker? costTracker = null)
    {
        _options = options.Value;
        _logger = logger;
        _redis = redis;
        _costTracker = costTracker;

        // Order providers by cascade level: Claude (0) → Gemini (1) → Llama (2)
        _providers = providers
            .OrderBy(p => p.ProviderType)
            .ToList()
            .AsReadOnly();
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        var totalSw = Stopwatch.StartNew();
        Interlocked.Increment(ref _totalCount);

        // ── FORCE DEGRADED MODE ──────────────────────────────────────────────
        if (_options.ForceDegradedMode)
        {
            _logger.LogInformation(
                "[LLMGateway] Degraded mode active — serving from cache for agent={Agent}, requestId={RequestId}",
                request.CallerAgent, request.RequestId);

            return await ServeDegradedResponse(request, totalSw, ct);
        }

        // ── CHECK CACHE FIRST ────────────────────────────────────────────────
        if (!request.SkipCache && _options.EnableCacheFallback)
        {
            var cached = await TryGetFromCacheAsync(request, ct);
            if (cached != null)
            {
                totalSw.Stop();
                cached = cached with
                {
                    TotalLatency = totalSw.Elapsed,
                    FromCache = true,
                    FallbackLevel = 0 // Cache hit = no fallback needed
                };

                Interlocked.Increment(ref _cacheCount);

                _logger.LogDebug(
                    "[LLMGateway] Cache HIT for agent={Agent} (latency: {Latency}ms)",
                    request.CallerAgent, totalSw.ElapsedMilliseconds);

                return cached;
            }
        }

        // ── AGGRESSIVE CACHE MODE: TRAFFIC SPLIT ──────────────────────────
        // When $700/month threshold is hit, split traffic:
        //   40% Claude, 40% Gemini Flash, 20% Llama local
        var orderedProviders = _providers;
        var effectiveCacheTtl = (TimeSpan?)null; // null = use per-request or global default

        // ── CONTRA #5: PER-DEALER DEGRADATION ZONE (80%–100% usage) ──────
        // When a dealer is in the degradation zone, aggressively use cheaper
        // providers + extended cache TTL to keep cost per conversation ≤ $0.04.
        // This takes priority over global aggressive mode since it's per-dealer.
        if (request.PreferCache && request.DealerUsagePercent >= 0.80)
        {
            orderedProviders = GetDegradationZoneProviders();
            effectiveCacheTtl = _options.CostAlerts.DegradationModeCacheTtl;

            _logger.LogInformation(
                "[LLMGateway] DEGRADATION ZONE active for dealer={DealerId} (usage={UsagePct:P0}) — " +
                "routing to {Provider}, extended cache TTL={CacheTtlHours}h. " +
                "Target: ≤$0.04/conversation. Agent={Agent}",
                request.DealerId,
                request.DealerUsagePercent,
                orderedProviders.FirstOrDefault()?.ProviderType.ToString() ?? "none",
                _options.CostAlerts.DegradationModeCacheTtl.TotalHours,
                request.CallerAgent);
        }
        else if (_costTracker != null && _options.CostAlerts.Enabled)
        {
            var isAggressiveMode = await _costTracker.IsAggressiveCacheModeActiveAsync(ct);
            if (isAggressiveMode)
            {
                orderedProviders = GetTrafficSplitProviders();
                _logger.LogDebug(
                    "[LLMGateway] Aggressive cache mode — routing to {Provider} for agent={Agent}",
                    orderedProviders.FirstOrDefault()?.ProviderType.ToString() ?? "none",
                    request.CallerAgent);
            }
        }

        // ── CASCADE THROUGH PROVIDERS ────────────────────────────────────
        var failedProviders = new List<(LlmProviderType Provider, string Error, int? StatusCode, long LatencyMs)>();

        foreach (var provider in orderedProviders)
        {
            // Skip disabled providers
            if (!IsProviderEnabled(provider.ProviderType))
                continue;

            var providerSw = Stopwatch.StartNew();

            try
            {
                var response = await provider.CompleteAsync(request, ct);
                providerSw.Stop();
                totalSw.Stop();

                // Update metrics
                IncrementProviderCounter(provider.ProviderType);

                // Cache the successful response for future degraded-mode use
                // In degradation zone, use extended TTL to maximize cache hits.
                _ = TryCacheResponseAsync(request, response, effectiveCacheTtl);

                // Record cost for threshold monitoring
                if (_costTracker != null && _options.CostAlerts.Enabled)
                {
                    _ = _costTracker.RecordCostAsync(response, request.CallerAgent, request.DealerId, ct);
                }

                // If this wasn't the primary provider, log the fallback event
                if (failedProviders.Count > 0)
                {
                    LogFallbackEvent(request, failedProviders, provider, totalSw.Elapsed);
                }

                return response with
                {
                    FallbackLevel = failedProviders.Count,
                    TotalLatency = totalSw.Elapsed
                };
            }
            catch (LlmProviderException ex) when (ex.IsTransient)
            {
                providerSw.Stop();
                failedProviders.Add((
                    provider.ProviderType,
                    ex.Message,
                    ex.HttpStatusCode,
                    providerSw.ElapsedMilliseconds));

                _logger.LogWarning(
                    "[LLMGateway] Provider {Provider} FAILED (transient): {Error} — cascading to next (agent={Agent}, latency={Latency}ms)",
                    provider.DisplayName, ex.Message, request.CallerAgent, providerSw.ElapsedMilliseconds);
            }
            catch (LlmProviderException ex) when (!ex.IsTransient)
            {
                providerSw.Stop();
                failedProviders.Add((
                    provider.ProviderType,
                    ex.Message,
                    ex.HttpStatusCode,
                    providerSw.ElapsedMilliseconds));

                _logger.LogError(
                    "[LLMGateway] Provider {Provider} FAILED (non-transient): {Error} — cascading (agent={Agent})",
                    provider.DisplayName, ex.Message, request.CallerAgent);
            }
            catch (Exception ex)
            {
                providerSw.Stop();
                failedProviders.Add((
                    provider.ProviderType,
                    ex.Message,
                    null,
                    providerSw.ElapsedMilliseconds));

                _logger.LogError(ex,
                    "[LLMGateway] Provider {Provider} UNEXPECTED ERROR — cascading (agent={Agent})",
                    provider.ProviderType, request.CallerAgent);
            }
        }

        // ── ALL PROVIDERS FAILED → DEGRADED MODE ────────────────────────────
        _logger.LogCritical(
            "[LLMGateway] ALL PROVIDERS FAILED for agent={Agent}, requestId={RequestId}. " +
            "Failures: {Failures}. Activating degraded mode (Redis cache).",
            request.CallerAgent,
            request.RequestId,
            string.Join("; ", failedProviders.Select(f =>
                $"{f.Provider}={f.StatusCode ?? 0} ({f.LatencyMs}ms)")));

        LogFallbackEvent(request, failedProviders, null, totalSw.Elapsed);

        return await ServeDegradedResponse(request, totalSw, ct);
    }

    public async Task<LlmHealthReport> GetHealthAsync(CancellationToken ct = default)
    {
        var health = new Dictionary<LlmProviderType, bool>();

        foreach (var provider in _providers)
        {
            try
            {
                health[provider.ProviderType] = await provider.IsHealthyAsync(ct);
            }
            catch
            {
                health[provider.ProviderType] = false;
            }
        }

        // Check Redis health
        try
        {
            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                await db.PingAsync();
                health[LlmProviderType.Cache] = true;
            }
            else
            {
                health[LlmProviderType.Cache] = false;
            }
        }
        catch
        {
            health[LlmProviderType.Cache] = false;
        }

        return new LlmHealthReport
        {
            ProviderHealth = health,
            CheckedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Get current model distribution metrics (% of requests per provider).
    /// </summary>
    public static LlmModelDistribution GetModelDistribution()
    {
        var total = Interlocked.Read(ref _totalCount);
        if (total == 0)
        {
            return new LlmModelDistribution
            {
                Claude = 0,
                Gemini = 0,
                Llama = 0,
                Cache = 0,
                TotalRequests = 0,
                Since = DateTimeOffset.UtcNow
            };
        }

        return new LlmModelDistribution
        {
            Claude = Math.Round((double)Interlocked.Read(ref _claudeCount) / total * 100, 1),
            Gemini = Math.Round((double)Interlocked.Read(ref _geminiCount) / total * 100, 1),
            Llama = Math.Round((double)Interlocked.Read(ref _llamaCount) / total * 100, 1),
            Cache = Math.Round((double)Interlocked.Read(ref _cacheCount) / total * 100, 1),
            TotalRequests = total,
            Since = DateTimeOffset.UtcNow
        };
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private bool IsProviderEnabled(LlmProviderType type) => type switch
    {
        LlmProviderType.Claude => _options.Claude.Enabled,
        LlmProviderType.Gemini => _options.Gemini.Enabled,
        LlmProviderType.Llama => _options.Llama.Enabled,
        _ => false
    };

    /// <summary>
    /// In aggressive cache mode, select a PRIMARY provider based on traffic split percentages,
    /// then keep the rest as fallback cascade.
    /// Default split: 40% Claude, 40% Gemini Flash, 20% Llama local.
    /// </summary>
    private IReadOnlyList<ILlmProvider> GetTrafficSplitProviders()
    {
        var thresholds = _options.CostAlerts;
        var roll = _trafficRng.Next(100); // 0–99

        LlmProviderType primaryType;
        if (roll < thresholds.AggressiveModeClaudePercent)
            primaryType = LlmProviderType.Claude;
        else if (roll < thresholds.AggressiveModeClaudePercent + thresholds.AggressiveModeGeminiPercent)
            primaryType = LlmProviderType.Gemini;
        else
            primaryType = LlmProviderType.Llama;

        // Reorder: selected primary first, then the rest as fallback
        var reordered = new List<ILlmProvider>();
        var primary = _providers.FirstOrDefault(p => p.ProviderType == primaryType);
        if (primary != null) reordered.Add(primary);

        foreach (var p in _providers)
        {
            if (p.ProviderType != primaryType)
                reordered.Add(p);
        }

        return reordered.AsReadOnly();
    }

    /// <summary>
    /// In per-dealer degradation zone (80%–100% conversation usage), select a PRIMARY provider
    /// prioritizing cheap/free models to keep marginal cost ≤ $0.04/conversation.
    /// Default split: 15% Claude, 60% Gemini Flash, 25% Llama local.
    ///
    /// CONTRA #5 FIX: Soft limit with progressive degradation.
    /// </summary>
    private IReadOnlyList<ILlmProvider> GetDegradationZoneProviders()
    {
        var thresholds = _options.CostAlerts;
        var roll = _trafficRng.Next(100); // 0–99

        LlmProviderType primaryType;
        if (roll < thresholds.DegradationModeClaudePercent)
            primaryType = LlmProviderType.Claude;
        else if (roll < thresholds.DegradationModeClaudePercent + thresholds.DegradationModeGeminiPercent)
            primaryType = LlmProviderType.Gemini;
        else
            primaryType = LlmProviderType.Llama;

        var reordered = new List<ILlmProvider>();
        var primary = _providers.FirstOrDefault(p => p.ProviderType == primaryType);
        if (primary != null) reordered.Add(primary);

        foreach (var p in _providers)
        {
            if (p.ProviderType != primaryType)
                reordered.Add(p);
        }

        return reordered.AsReadOnly();
    }

    private static void IncrementProviderCounter(LlmProviderType type)
    {
        switch (type)
        {
            case LlmProviderType.Claude: Interlocked.Increment(ref _claudeCount); break;
            case LlmProviderType.Gemini: Interlocked.Increment(ref _geminiCount); break;
            case LlmProviderType.Llama: Interlocked.Increment(ref _llamaCount); break;
            case LlmProviderType.Cache: Interlocked.Increment(ref _cacheCount); break;
        }
    }

    private async Task<LlmResponse> ServeDegradedResponse(
        LlmRequest request, Stopwatch totalSw, CancellationToken ct)
    {
        // Try to serve from cache
        if (_options.EnableCacheFallback)
        {
            var cached = await TryGetFromCacheAsync(request, ct);
            if (cached != null)
            {
                totalSw.Stop();
                Interlocked.Increment(ref _cacheCount);

                _logger.LogWarning(
                    "[LLMGateway] DEGRADED MODE: Serving cached response for agent={Agent}",
                    request.CallerAgent);

                return cached with
                {
                    FallbackLevel = 3,
                    TotalLatency = totalSw.Elapsed,
                    FromCache = true,
                    Provider = LlmProviderType.Cache,
                    ModelId = "redis-cache"
                };
            }
        }

        // Absolute last resort: static degraded response
        totalSw.Stop();
        Interlocked.Increment(ref _cacheCount);

        _logger.LogCritical(
            "[LLMGateway] TOTAL DEGRADATION: No cache available. Returning static fallback for agent={Agent}",
            request.CallerAgent);

        return new LlmResponse
        {
            Content = GetStaticFallbackContent(request.CallerAgent),
            Provider = LlmProviderType.Cache,
            ModelId = "static-fallback",
            FallbackLevel = 3,
            TotalLatency = totalSw.Elapsed,
            ProviderLatency = TimeSpan.Zero,
            FromCache = true,
            StopReason = "degraded_mode"
        };
    }

    private static string GetStaticFallbackContent(string agent) => agent.ToLowerInvariant() switch
    {
        "searchagent" => "{}",
        "recoagent" => "[]",
        "supportagent" => "Lo siento, estoy teniendo dificultades técnicas en este momento. " +
                          "Por favor intenta de nuevo en unos minutos o contáctanos por WhatsApp al +1-809-XXX-XXXX.",
        "chatbotagent" or "dealerchatagent" =>
            "Disculpa, no puedo procesar tu consulta en este momento. " +
            "Un representante del dealer te contactará pronto.",
        _ => "El servicio está temporalmente degradado. Por favor intenta de nuevo."
    };

    // ── REDIS CACHE ──────────────────────────────────────────────────────────

    private string BuildCacheKey(LlmRequest request)
    {
        var key = request.CacheKey ?? $"{request.SystemPrompt}|{request.UserMessage}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"{_options.RedisCachePrefix}:{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }

    private async Task<LlmResponse?> TryGetFromCacheAsync(LlmRequest request, CancellationToken ct)
    {
        if (_redis == null) return null;

        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = BuildCacheKey(request);
            var cached = await db.StringGetAsync(cacheKey);

            if (cached.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<LlmResponse>(cached.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LLMGateway] Redis cache read failed");
            return null;
        }
    }

    private async Task TryCacheResponseAsync(LlmRequest request, LlmResponse response, TimeSpan? degradationTtlOverride = null)
    {
        if (_redis == null || request.SkipCache || !_options.EnableCacheFallback) return;

        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = BuildCacheKey(request);
            var json = JsonSerializer.Serialize(response);
            // Priority: per-request override > degradation zone override > global CacheTtl.
            var ttl = request.CacheTtlOverride ?? degradationTtlOverride ?? _options.CacheTtl;
            await db.StringSetAsync(cacheKey, json, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LLMGateway] Redis cache write failed");
        }
    }

    // ── FALLBACK LOGGING ─────────────────────────────────────────────────────

    private void LogFallbackEvent(
        LlmRequest request,
        List<(LlmProviderType Provider, string Error, int? StatusCode, long LatencyMs)> failures,
        ILlmProvider? resolvedBy,
        TimeSpan totalLatency)
    {
        var entry = new FallbackLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            RequestId = request.RequestId,
            AgentAffected = request.CallerAgent,
            PrimaryModelFailed = failures.FirstOrDefault().Provider.ToString(),
            PrimaryFailureReason = failures.FirstOrDefault().Error,
            PrimaryHttpStatus = failures.FirstOrDefault().StatusCode,
            FallbackModelActivated = resolvedBy?.DisplayName ?? "Redis Cache (degraded)",
            FallbackLevel = resolvedBy != null
                ? (int)resolvedBy.ProviderType
                : 3,
            TotalRedirectionLatencyMs = totalLatency.TotalMilliseconds,
            FailureChain = failures.Select(f => new FailureChainEntry
            {
                Provider = f.Provider.ToString(),
                HttpStatus = f.StatusCode,
                LatencyMs = f.LatencyMs,
                Error = f.Error
            }).ToList()
        };

        _logger.LogWarning(
            "[LLMGateway:FALLBACK] Agent={Agent} | " +
            "PrimaryFailed={PrimaryModel} (HTTP {PrimaryStatus}) | " +
            "FallbackActivated={FallbackModel} | " +
            "Level={FallbackLevel} (0=Claude, 1=Gemini, 2=Llama, 3=Cache) | " +
            "TotalLatency={TotalLatencyMs}ms | " +
            "RequestId={RequestId} | " +
            "Timestamp={Timestamp:O}",
            entry.AgentAffected,
            entry.PrimaryModelFailed,
            entry.PrimaryHttpStatus,
            entry.FallbackModelActivated,
            entry.FallbackLevel,
            entry.TotalRedirectionLatencyMs,
            entry.RequestId,
            entry.Timestamp);
    }

    // =========================================================================
    // LOG DTOs
    // =========================================================================

    private sealed class FallbackLogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public string RequestId { get; set; } = null!;
        public string AgentAffected { get; set; } = null!;
        public string PrimaryModelFailed { get; set; } = null!;
        public string? PrimaryFailureReason { get; set; }
        public int? PrimaryHttpStatus { get; set; }
        public string FallbackModelActivated { get; set; } = null!;
        public int FallbackLevel { get; set; }
        public double TotalRedirectionLatencyMs { get; set; }
        public List<FailureChainEntry> FailureChain { get; set; } = [];
    }

    private sealed class FailureChainEntry
    {
        public string Provider { get; set; } = null!;
        public int? HttpStatus { get; set; }
        public long LatencyMs { get; set; }
        public string Error { get; set; } = null!;
    }
}

/// <summary>
/// Model distribution statistics for the admin dashboard.
/// </summary>
public sealed class LlmModelDistribution
{
    /// <summary>Percentage of requests served by Claude.</summary>
    public double Claude { get; init; }

    /// <summary>Percentage of requests served by Gemini.</summary>
    public double Gemini { get; init; }

    /// <summary>Percentage of requests served by Llama.</summary>
    public double Llama { get; init; }

    /// <summary>Percentage of requests served from cache.</summary>
    public double Cache { get; init; }

    /// <summary>Total requests processed since startup.</summary>
    public long TotalRequests { get; init; }

    /// <summary>When this snapshot was taken.</summary>
    public DateTimeOffset Since { get; init; }
}
