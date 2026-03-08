namespace RecoAgent.Domain.Interfaces;

/// <summary>
/// Redis cache interface for RecoAgent recommendation caching.
/// Supports both batch (4h TTL) and real-time (15m TTL) caching.
/// </summary>
public interface IRecoCacheService
{
    Task<string?> GetCachedResponseAsync(string cacheKey, CancellationToken ct = default);
    Task SetCachedResponseAsync(string cacheKey, string responseJson, int ttlSeconds, CancellationToken ct = default);
    Task InvalidateUserCacheAsync(string userId, CancellationToken ct = default);
    Task StoreUserCacheKeyMappingAsync(string userId, string cacheKey, string mode, int ttlSeconds, CancellationToken ct = default);
}
