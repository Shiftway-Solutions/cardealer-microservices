using ListingAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ListingAgent.Infrastructure.Services;

/// <summary>
/// Redis-backed implementation of IListingCacheMetrics.
/// Uses two atomic Redis counters (INCR) to track hits and misses for the ListingAgent.
///
/// Redis keys:
///   okla:listing:cache:hits   — total cache hits since service start
///   okla:listing:cache:misses — total cache misses since service start
///
/// These counters persist across restarts (unlike in-memory), giving accurate
/// long-term hit rate data for the ≥50% target measurement.
/// </summary>
public sealed class RedisListingCacheMetrics : IListingCacheMetrics
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisListingCacheMetrics> _logger;

    private const string HitsKey = "okla:listing:cache:hits";
    private const string MissesKey = "okla:listing:cache:misses";

    // In-memory fallback when Redis is unavailable
    private static long _memHits;
    private static long _memMisses;

    public RedisListingCacheMetrics(
        IConnectionMultiplexer redis,
        ILogger<RedisListingCacheMetrics> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task RecordHitAsync(string cacheKey, CancellationToken ct = default)
    {
        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringIncrementAsync(HitsKey);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ListingCacheMetrics] Redis increment HITS failed — using in-memory fallback");
            }
        }
        Interlocked.Increment(ref _memHits);
    }

    public async Task RecordMissAsync(string cacheKey, CancellationToken ct = default)
    {
        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringIncrementAsync(MissesKey);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ListingCacheMetrics] Redis increment MISSES failed — using in-memory fallback");
            }
        }
        Interlocked.Increment(ref _memMisses);
    }

    public async Task<ListingCacheStats> GetStatsAsync(CancellationToken ct = default)
    {
        long hits, misses;

        if (_redis != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var hitsVal = await db.StringGetAsync(HitsKey);
                var missesVal = await db.StringGetAsync(MissesKey);
                hits = hitsVal.IsNullOrEmpty ? 0 : (long)hitsVal;
                misses = missesVal.IsNullOrEmpty ? 0 : (long)missesVal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ListingCacheMetrics] Redis GET stats failed — using in-memory fallback");
                hits = Interlocked.Read(ref _memHits);
                misses = Interlocked.Read(ref _memMisses);
            }
        }
        else
        {
            hits = Interlocked.Read(ref _memHits);
            misses = Interlocked.Read(ref _memMisses);
        }

        return new ListingCacheStats
        {
            CacheHits = hits,
            CacheMisses = misses,
            MeasuredAt = DateTimeOffset.UtcNow
        };
    }
}
