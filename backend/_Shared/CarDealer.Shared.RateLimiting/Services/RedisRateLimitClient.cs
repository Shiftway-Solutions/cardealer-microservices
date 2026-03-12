using System.Security.Claims;
using CarDealer.Shared.RateLimiting.Interfaces;
using CarDealer.Shared.RateLimiting.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CarDealer.Shared.RateLimiting.Services;

/// <summary>
/// Redis-based rate limit client using sliding window algorithm
/// </summary>
public class RedisRateLimitClient : IRateLimitClient
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RedisRateLimitClient> _logger;

    public RedisRateLimitClient(
        IConnectionMultiplexer redis,
        IOptions<RateLimitOptions> options,
        ILogger<RedisRateLimitClient> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckAsync(
        string clientId,
        string endpoint,
        string? tier = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return RateLimitResult.Allowed(clientId, "disabled", int.MaxValue, DateTime.UtcNow.AddHours(1));
        }

        try
        {
            var policy = GetPolicyForEndpoint(endpoint);
            var limit = GetLimitForTier(policy, tier);
            var windowSeconds = policy?.WindowSeconds ?? _options.DefaultWindowSeconds;
            var policyName = policy?.Pattern ?? "default";

            var key = $"{_options.KeyPrefix}:{clientId}:{endpoint}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowStart = now - windowSeconds;
            var resetAt = DateTime.UtcNow.AddSeconds(windowSeconds);

            var db = _redis.GetDatabase();

            // Use Lua script for atomic sliding window operation
            var script = @"
                local key = KEYS[1]
                local now = tonumber(ARGV[1])
                local window_start = tonumber(ARGV[2])
                local limit = tonumber(ARGV[3])
                local window_seconds = tonumber(ARGV[4])

                -- Remove old entries outside the window
                redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)

                -- Count current entries
                local current = redis.call('ZCARD', key)

                if current < limit then
                    -- Add new entry
                    redis.call('ZADD', key, now, now .. ':' .. math.random())
                    redis.call('EXPIRE', key, window_seconds)
                    return {1, limit - current - 1, limit}
                else
                    return {0, 0, limit}
                end
            ";

            var result = await db.ScriptEvaluateAsync(
                script,
                new RedisKey[] { key },
                new RedisValue[] { now, windowStart, limit, windowSeconds });

            var resultArray = (RedisResult[])result!;
            var allowed = (int)resultArray[0] == 1;
            var remaining = (int)resultArray[1];
            var appliedLimit = (int)resultArray[2];

            if (allowed)
            {
                _logger.LogDebug(
                    "Rate limit check passed: ClientId={ClientId}, Endpoint={Endpoint}, Remaining={Remaining}/{Limit}",
                    clientId, endpoint, remaining, appliedLimit);

                return RateLimitResult.Allowed(clientId, policyName, remaining, resetAt, appliedLimit);
            }
            else
            {
                var retryAfter = TimeSpan.FromSeconds(windowSeconds);

                _logger.LogWarning(
                    "Rate limit exceeded: ClientId={ClientId}, Endpoint={Endpoint}, Limit={Limit}",
                    clientId, endpoint, appliedLimit);

                return RateLimitResult.RateLimited(clientId, policyName, appliedLimit, resetAt, retryAfter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {ClientId} on {Endpoint}", clientId, endpoint);
            // On error, allow the request (fail-open)
            return RateLimitResult.Allowed(clientId, "error-fallback", _options.DefaultLimit, DateTime.UtcNow.AddMinutes(1));
        }
    }

    public async Task<RateLimitResult> CheckAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var path = context.Request.Path.Value ?? "/";

        // Check if path is excluded
        if (IsExcludedPath(path))
        {
            return RateLimitResult.Allowed("excluded", "excluded", int.MaxValue, DateTime.UtcNow.AddHours(1));
        }

        var clientId = GetClientIdentifier(context);
        var endpoint = $"{context.Request.Method}:{path}";
        var tier = GetUserTier(context);

        return await CheckAsync(clientId, endpoint, tier, cancellationToken);
    }

    public string GetClientIdentifier(HttpContext context)
    {
        // Priority 1: Custom header
        if (context.Request.Headers.TryGetValue(_options.ClientIdHeader, out var clientIdHeader) &&
            !string.IsNullOrEmpty(clientIdHeader.FirstOrDefault()))
        {
            return clientIdHeader.FirstOrDefault()!;
        }

        // Priority 2: Authenticated user ID
        if (_options.IncludeUserId && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // Priority 3: IP Address (fallback)
        if (_options.UseIpAsFallback)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return $"ip:{forwardedFor.Split(',').First().Trim()}";
            }

            return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        return "anonymous";
    }

    public string GetUserTier(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return "anonymous";
        }

        // Check for specific role claims to determine tier
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (roles.Contains("Admin"))
            return "admin";
        if (roles.Contains("Premium") || roles.Contains("Enterprise"))
            return "premium";
        if (roles.Contains("Dealer"))
            return "dealer";

        return "authenticated";
    }

    private EndpointRateLimitPolicy? GetPolicyForEndpoint(string endpoint)
    {
        foreach (var policy in _options.Policies.Values.Where(p => p.Enabled))
        {
            if (MatchesPattern(endpoint, policy.Pattern))
            {
                return policy;
            }
        }
        return null;
    }

    private int GetLimitForTier(EndpointRateLimitPolicy? policy, string? tier)
    {
        if (policy == null)
            return _options.DefaultLimit;

        if (!string.IsNullOrEmpty(tier) && policy.TierLimits.TryGetValue(tier, out var tierLimit))
        {
            return tierLimit;
        }

        return policy.Limit;
    }

    private bool MatchesPattern(string endpoint, string pattern)
    {
        // Simple wildcard matching
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.TrimEnd('*');
            return endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return endpoint.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsExcludedPath(string path)
    {
        return _options.ExcludedPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}
