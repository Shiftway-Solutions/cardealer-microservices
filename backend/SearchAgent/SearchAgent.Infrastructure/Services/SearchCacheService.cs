using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SearchAgent.Domain.Interfaces;

namespace SearchAgent.Infrastructure.Services;

/// <summary>
/// Redis-backed cache service for SearchAgent query responses.
/// Reduces Claude API costs by caching repeated queries.
/// </summary>
public class SearchCacheService : ISearchCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<SearchCacheService> _logger;
    private const string CachePrefix = "search-agent:";

    public SearchCacheService(IDistributedCache cache, ILogger<SearchCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetCachedResponseAsync(string queryHash, CancellationToken ct = default)
    {
        try
        {
            var cached = await _cache.GetStringAsync($"{CachePrefix}{queryHash}", ct);
            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for hash {Hash}. Proceeding without cache.", queryHash);
            return null;
        }
    }

    public async Task SetCachedResponseAsync(string queryHash, string responseJson, int ttlSeconds, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
            };

            await _cache.SetStringAsync($"{CachePrefix}{queryHash}", responseJson, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for hash {Hash}. Response not cached.", queryHash);
        }
    }
}
