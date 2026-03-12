using System.Net.Http.Json;
using System.Security.Claims;
using CarDealer.Shared.RateLimiting.Interfaces;
using CarDealer.Shared.RateLimiting.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarDealer.Shared.RateLimiting.Services;

/// <summary>
/// HTTP-based rate limit client that calls the RateLimitingService API
/// </summary>
public class HttpRateLimitClient : IRateLimitClient
{
    private readonly HttpClient _httpClient;
    private readonly RateLimitOptions _options;
    private readonly ILogger<HttpRateLimitClient> _logger;

    public HttpRateLimitClient(
        HttpClient httpClient,
        IOptions<RateLimitOptions> options,
        ILogger<HttpRateLimitClient> logger)
    {
        _httpClient = httpClient;
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
            var request = new
            {
                ClientId = clientId,
                Endpoint = endpoint,
                Tier = tier ?? "authenticated"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.ServiceUrl}/api/ratelimit/check",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RateLimitResult>(cancellationToken: cancellationToken);
                return result ?? RateLimitResult.Allowed(clientId, "default", _options.DefaultLimit, DateTime.UtcNow.AddMinutes(1));
            }

            // Service unavailable - fail open
            _logger.LogWarning("RateLimitingService returned {StatusCode}", response.StatusCode);
            return RateLimitResult.Allowed(clientId, "service-unavailable", _options.DefaultLimit, DateTime.UtcNow.AddMinutes(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling RateLimitingService for {ClientId} on {Endpoint}", clientId, endpoint);
            // Fail-open: allow the request on error
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

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (roles.Contains("Admin"))
            return "admin";
        if (roles.Contains("Premium") || roles.Contains("Enterprise"))
            return "premium";
        if (roles.Contains("Dealer"))
            return "dealer";

        return "authenticated";
    }

    private bool IsExcludedPath(string path)
    {
        return _options.ExcludedPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}
