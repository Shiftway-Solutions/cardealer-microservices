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
            // The cache keys are SHA256 hashes that include the userId, so we can't
            // predict the exact key. We also store reverse-mapping keys to enable lookup.
            await _cache.RemoveAsync($"{CachePrefix}{userId}:batch", ct);
            await _cache.RemoveAsync($"{CachePrefix}{userId}:realtime", ct);

            // Also remove the last-known cache key for this user (stored at set time)
            var lastKeyBatch = await _cache.GetStringAsync($"{CachePrefix}user-key:{userId}:batch", ct);
            if (!string.IsNullOrEmpty(lastKeyBatch))
                await _cache.RemoveAsync($"{CachePrefix}{lastKeyBatch}", ct);

            var lastKeyRt = await _cache.GetStringAsync($"{CachePrefix}user-key:{userId}:realtime", ct);
            if (!string.IsNullOrEmpty(lastKeyRt))
                await _cache.RemoveAsync($"{CachePrefix}{lastKeyRt}", ct);

            _logger.LogInformation("Cache invalidated for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Stores a reverse-mapping key so we can find the SHA256 cache key for a given user.
    /// Call this when setting a cache entry.
    /// </summary>
    public async Task StoreUserCacheKeyMappingAsync(string userId, string cacheKey, string mode, int ttlSeconds, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
            };
            await _cache.SetStringAsync($"{CachePrefix}user-key:{userId}:{mode}", cacheKey, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store cache key mapping for user {UserId}", userId);
        }
    }
}
