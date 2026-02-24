using System.Security.Claims;
using System.Text.Json;
using KYCService.Application.Clients;
using KYCService.Domain.Interfaces;

namespace KYCService.Api.Middleware;

/// <summary>
/// Configuration for rate limiting
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// Maximum requests per window
    /// </summary>
    public int MaxRequests { get; set; } = 10;
    
    /// <summary>
    /// Window duration in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Middleware for rate limiting KYC operations
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Endpoint-specific rate limits (ordered from most-specific to least-specific for prefix matching)
    private static readonly Dictionary<string, RateLimitConfig> EndpointLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        // Identity verification process - strict (10 per hour)
        { "/api/identity-verification/process", new RateLimitConfig { MaxRequests = 10, WindowSeconds = 3600 } },
        
        // Submit for review - very strict (3 per day)
        { "/api/kyc/submit-for-review", new RateLimitConfig { MaxRequests = 3, WindowSeconds = 86400 } },
        
        // Document upload POST - moderate (20 per 10 minutes)
        // NOTE: This key targets paths like /api/kyc/profiles/{id}/documents only for writes;
        // the HTTP-method check in GetRateLimitConfig prevents it from hitting GET requests.
        { "/api/kyc/documents", new RateLimitConfig { MaxRequests = 20, WindowSeconds = 600 } },

        // Default for other KYC endpoints (generous for read operations)
        { "/api/kyc", new RateLimitConfig { MaxRequests = 100, WindowSeconds = 60 } },
        { "/api/identity-verification", new RateLimitConfig { MaxRequests = 50, WindowSeconds = 60 } }
    };

    // POST-only endpoints that have stricter rate limits checked by HTTP method
    private static readonly Dictionary<string, RateLimitConfig> PostOnlyLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        // Profile creation - very strict (5 per hour, POST only)
        { "/api/kyc/profiles", new RateLimitConfig { MaxRequests = 5, WindowSeconds = 3600 } },
    };

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitRepository repository, IAuditServiceClient auditClient)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        // Admins and compliance officers bypass rate limiting to allow document review workflows
        var accountType = context.User.FindFirst("account_type")?.Value;
        if (accountType == "4" || accountType == "5") // Admin = 4, PlatformEmployee = 5
        {
            await _next(context);
            return;
        }

        // Check if this endpoint has rate limiting
        var config = GetRateLimitConfig(path, context.Request.Method);
        if (config == null)
        {
            await _next(context);
            return;
        }

        // Get rate limit key (user ID or IP)
        var rateLimitKey = GetRateLimitKey(context);
        if (string.IsNullOrEmpty(rateLimitKey))
        {
            await _next(context);
            return;
        }

        // Get or create rate limit entry
        var entry = await repository.IncrementAsync(
            rateLimitKey, 
            path, 
            TimeSpan.FromSeconds(config.WindowSeconds));

        // Check if limit exceeded
        if (entry.RequestCount > config.MaxRequests)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Key} on {Path}. Count: {Count}/{Max}", 
                rateLimitKey, path, entry.RequestCount, config.MaxRequests);

            // Log security event to centralized AuditService
            await LogRateLimitExceeded(auditClient, context, rateLimitKey, path, entry.RequestCount, config.MaxRequests);

            // Return 429 Too Many Requests
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            
            var retryAfter = (int)(entry.WindowEnd - DateTime.UtcNow).TotalSeconds;
            context.Response.Headers.Append("Retry-After", retryAfter.ToString());
            context.Response.Headers.Append("X-RateLimit-Limit", config.MaxRequests.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", "0");
            context.Response.Headers.Append("X-RateLimit-Reset", ((DateTimeOffset)entry.WindowEnd).ToUnixTimeSeconds().ToString());

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Rate limit exceeded",
                errorCode = "KYC_RATE_LIMIT_EXCEEDED",
                message = $"Too many requests. Please try again in {retryAfter} seconds.",
                retryAfter,
                limit = config.MaxRequests,
                windowSeconds = config.WindowSeconds
            }));
            return;
        }

        // Add rate limit headers
        var remaining = Math.Max(0, config.MaxRequests - entry.RequestCount);
        context.Response.Headers.Append("X-RateLimit-Limit", config.MaxRequests.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", ((DateTimeOffset)entry.WindowEnd).ToUnixTimeSeconds().ToString());

        await _next(context);
    }

    private RateLimitConfig? GetRateLimitConfig(string path, string method)
    {
        // Check POST-only limits first (profile creation, etc.)
        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var kvp in PostOnlyLimits.OrderByDescending(k => k.Key.Length))
            {
                if (path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
        }

        // Find the most specific matching config for other methods
        foreach (var kvp in EndpointLimits.OrderByDescending(k => k.Key.Length))
        {
            if (path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }
        return null;
    }

    private string? GetRateLimitKey(HttpContext context)
    {
        // Try to get user ID first
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? context.User.FindFirst("sub")?.Value
                       ?? context.User.FindFirst("user_id")?.Value;
        
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            return $"user:{userIdClaim}";
        }

        // Fall back to IP address
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
        {
            return $"ip:{ip}";
        }

        return null;
    }

    private async Task LogRateLimitExceeded(
        IAuditServiceClient auditClient,
        HttpContext context,
        string rateLimitKey,
        string path,
        int currentCount,
        int maxAllowed)
    {
        // Extract user ID if available
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? context.User.FindFirst("sub")?.Value
                       ?? "anonymous";

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

        // Log to centralized AuditService
        await auditClient.LogKYCEventAsync(
            userIdClaim,
            KYCAuditActions.RateLimitExceeded,
            $"kyc-rate-limit:{path}",
            ipAddress,
            userAgent,
            success: false,
            errorMessage: $"Rate limit exceeded on {path}. Count: {currentCount}/{maxAllowed}",
            additionalData: new Dictionary<string, object>
            {
                { "rateLimitKey", rateLimitKey },
                { "path", path },
                { "currentCount", currentCount },
                { "maxAllowed", maxAllowed },
                { "method", context.Request.Method }
            });
    }
}

/// <summary>
/// Extension method to add rate limiting middleware
/// </summary>
public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseKYCRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}
