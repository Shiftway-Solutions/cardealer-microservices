using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Infrastructure.Services;

/// <summary>
/// Redis-backed cache service for RecoAgent recommendation responses.
/// Supports both batch (4h TTL) and real-time (15min TTL) cache strategies.
/// </summary>
public class RecoCacheService : IRecoCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RecoCacheService> _logger;
    private const string CachePrefix = "reco-agent:";

    public RecoCacheService(IDistributedCache cache, ILogger<RecoCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetCachedResponseAsync(string cacheKey, CancellationToken ct = default)
    {
        try
        {
            var cached = await _cache.GetStringAsync($"{CachePrefix}{cacheKey}", ct);
            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {Key}. Proceeding without cache.", cacheKey);
            return null;
        }
    }

    public async Task SetCachedResponseAsync(string cacheKey, string responseJson, int ttlSeconds, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
            };

            await _cache.SetStringAsync($"{CachePrefix}{cacheKey}", responseJson, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for key {Key}. Response not cached.", cacheKey);
        }
    }

    public async Task InvalidateUserCacheAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            // Invalidate by removing all cache entries for this user
            // In a production Redis setup, you'd use SCAN + DEL with a pattern
            // For now, we remove the most common cache key patterns
            await _cache.RemoveAsync($"{CachePrefix}{userId}:batch", ct);
            await _cache.RemoveAsync($"{CachePrefix}{userId}:realtime", ct);
            _logger.LogInformation("Cache invalidated for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }
}
