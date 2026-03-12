using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChatbotService.Domain.Interfaces;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// R17 (MLOps): Redis response cache for LLM inference.
/// Caches identical queries to avoid redundant LLM calls.
/// 
/// Cache key = SHA256(normalized_query + system_prompt).
/// TTL is configurable (default: 30 minutes).
/// 
/// This is especially effective for:
/// - FAQ-type questions (e.g., "horario", "ubicación", "métodos de pago")
/// - Repeated queries within a session
/// - High-traffic periods with similar queries
/// </summary>
public class LlmResponseCacheService : ILlmResponseCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<LlmResponseCacheService> _logger;
    private readonly ChatbotMetrics _metrics;
    private readonly TimeSpan _defaultTtl;
    private readonly bool _enabled;

    // Queries shorter than this are too ambiguous to cache reliably
    private const int MIN_QUERY_LENGTH = 5;

    // Intents that should NOT be cached (dynamic/personalized responses)
    private static readonly HashSet<string> _noCacheIntents = new(StringComparer.OrdinalIgnoreCase)
    {
        "vehicle_search",
        "price_negotiation",
        "lead_capture",
        "appointment_scheduling",
        "contact_agent"
    };

    public LlmResponseCacheService(
        IDistributedCache cache,
        ILogger<LlmResponseCacheService> logger,
        ChatbotMetrics metrics,
        TimeSpan? defaultTtl = null,
        bool enabled = true)
    {
        _cache = cache;
        _logger = logger;
        _metrics = metrics;
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(30);
        _enabled = enabled;
    }

    /// <summary>
    /// Try to get a cached response for the given query.
    /// Returns null if no cache hit.
    /// </summary>
    public async Task<CachedLlmResponseDto?> GetAsync(string query, string? systemPrompt = null, CancellationToken ct = default)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(query) || query.Length < MIN_QUERY_LENGTH)
            return null;

        var key = BuildCacheKey(query, systemPrompt);

        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached != null)
            {
                var response = JsonSerializer.Deserialize<CachedLlmResponseDto>(cached);
                if (response != null)
                {
                    _logger.LogInformation("Cache HIT for query: '{Query}' (key: {Key})",
                        query[..Math.Min(30, query.Length)], key[..12]);
                    _metrics.RecordCacheHit();
                    response.FromCache = true;
                    return response;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for key {Key}", key[..12]);
        }

        _metrics.RecordCacheMiss();
        return null;
    }

    /// <summary>
    /// Cache an LLM response. Only caches non-fallback, high-confidence responses
    /// for cacheable intents.
    /// </summary>
    public async Task SetAsync(
        string query,
        string response,
        string? intent,
        float confidence,
        bool isFallback,
        string? systemPrompt = null,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(query) || query.Length < MIN_QUERY_LENGTH)
            return;

        // Don't cache fallback responses or low-confidence answers
        if (isFallback || confidence < 0.7f)
            return;

        // Don't cache dynamic/personalized intents
        if (intent != null && _noCacheIntents.Contains(intent))
            return;

        var key = BuildCacheKey(query, systemPrompt);
        var cached = new CachedLlmResponseDto
        {
            Response = response,
            Intent = intent,
            Confidence = confidence,
            CachedAt = DateTime.UtcNow,
            FromCache = false
        };

        try
        {
            var json = JsonSerializer.Serialize(cached);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl,
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(key, json, options, ct);
            _logger.LogDebug("Cached response for query: '{Query}' (TTL: {Ttl})",
                query[..Math.Min(30, query.Length)], (ttl ?? _defaultTtl).TotalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for key {Key}", key[..12]);
        }
    }

    /// <summary>
    /// Invalidate all cached LLM responses (e.g., after inventory sync or model update).
    /// Uses StackExchange.Redis SCAN to find and delete all llm:cache:* keys.
    /// </summary>
    public async Task InvalidateAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating all LLM response cache entries");

        try
        {
            // Access the underlying StackExchange.Redis connection for SCAN support.
            // IDistributedCache doesn't support wildcard operations, so we use the
            // concrete Redis connection to efficiently scan and delete cached responses.
            if (_cache is Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache redisCache)
            {
                var field = typeof(Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache)
                    .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(redisCache) is StackExchange.Redis.IDatabase db)
                {
                    var server = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First());
                    var keysDeleted = 0;

                    await foreach (var key in server.KeysAsync(pattern: "llm:cache:*", pageSize: 100))
                    {
                        await db.KeyDeleteAsync(key);
                        keysDeleted++;
                    }

                    _logger.LogInformation("Invalidated {Count} cached LLM responses", keysDeleted);
                    return;
                }
            }

            _logger.LogWarning("Could not access Redis directly for cache invalidation. Entries will expire via TTL.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache invalidation failed. Entries will expire naturally via TTL.");
        }
    }

    /// <summary>
    /// Invalidate a specific cached LLM response by query and system prompt.
    /// Used to remove hallucinated or moderated cached responses.
    /// </summary>
    public async Task InvalidateAsync(string query, string? systemPrompt = null, CancellationToken ct = default)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(query))
            return;

        var key = BuildCacheKey(query, systemPrompt);

        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogInformation("Invalidated cached response for key {Key}", key[..12]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache entry {Key}", key[..12]);
        }
    }

    private static string BuildCacheKey(string query, string? systemPrompt)
    {
        // Normalize: lowercase, trim, collapse whitespace
        var normalized = query.Trim().ToLowerInvariant();
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        // CRITICAL: Use SHA256 for system prompt hashing instead of GetHashCode().
        // string.GetHashCode() is non-deterministic in .NET 6+ (randomized per process)
        // which causes cache misses across pod restarts and different replicas in K8s.
        // SHA256 is deterministic and consistent across all processes/platforms.
        var input = systemPrompt != null
            ? $"{normalized}|{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(systemPrompt)))[..16]}"
            : normalized;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"llm:cache:{Convert.ToHexString(hash)[..16].ToLower()}";
    }
}
