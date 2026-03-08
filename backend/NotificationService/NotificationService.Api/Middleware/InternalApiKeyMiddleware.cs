namespace NotificationService.Api.Middleware;

/// <summary>
/// Validates X-Internal-Api-Key header on internal endpoints (/api/internal/*).
/// Defense-in-depth: even though these endpoints run inside K8s network,
/// a compromised pod or misconfigured Ingress shouldn't grant email-sending capability.
/// </summary>
public class InternalApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InternalApiKeyMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-Internal-Api-Key";

    public InternalApiKeyMiddleware(RequestDelegate next, ILogger<InternalApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!context.Request.Path.StartsWithSegments("/api/internal"))
        {
            await _next(context);
            return;
        }

        var configuredKey = configuration["Security:InternalApiKey"];

        if (string.IsNullOrEmpty(configuredKey))
        {
            _logger.LogError(
                "Security:InternalApiKey not configured — REJECTING internal request to {Path} (fail-closed). " +
                "Set this via K8s secret or appsettings.",
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = "Internal API key not configured" });
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) ||
            !string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Internal API request to {Path} rejected — invalid or missing {Header} header",
                context.Request.Path, ApiKeyHeaderName);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid internal API key" });
            return;
        }

        await _next(context);
    }
}

public static class InternalApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseInternalApiKeyValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<InternalApiKeyMiddleware>();
    }
}
