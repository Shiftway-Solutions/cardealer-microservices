namespace SearchAgent.Domain.Interfaces;

/// <summary>
/// Redis cache interface for SearchAgent query caching.
/// </summary>
public interface ISearchCacheService
{
    Task<string?> GetCachedResponseAsync(string queryHash, CancellationToken ct = default);
    Task SetCachedResponseAsync(string queryHash, string responseJson, int ttlSeconds, CancellationToken ct = default);
}
