using Microsoft.AspNetCore.Http;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace Gateway.Api.Middleware;

/// <summary>
/// Server-side CSRF validation middleware (Double Submit Cookie pattern).
/// Validates that the X-CSRF-Token header matches the csrf_token cookie
/// for all state-changing HTTP methods (POST, PUT, PATCH, DELETE).
/// 
/// OWASP Reference: https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html
/// </summary>
public class CsrfValidationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS"
    };

    // Paths that are exempt from CSRF validation (webhooks, OAuth callbacks, health checks)
    private static readonly string[] ExemptPaths = new[]
    {
        "/health",
        "/api/webhooks/",
        "/api/auth/oauth/",
        "/api/ExternalAuth/",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/verify-email",
        // Security: Narrowed invitation exemptions to specific accept endpoint only
        "/api/invitations/accept",
        "/api/staff/invitations/accept",
        // ChatbotService: Public chatbot endpoints (visitors without login)
        "/api/chat/start",
        "/api/chat/message",
        "/api/chat/end",
        "/api/chat/transfer",
        // AI Agent APIs: JWT-authenticated only — CSRF redundant (no browser cookies involved)
        "/api/search-agent/",
        "/api/support/",
        "/api/reco-agent/",
        "/swagger",
        "/metrics",
    };

    public CsrfValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;

        // Skip CSRF check for safe (read-only) methods
        if (SafeMethods.Contains(method))
        {
            await _next(context);
            return;
        }

        // Skip CSRF check for exempt paths
        var path = context.Request.Path.Value ?? "";
        if (ExemptPaths.Any(exempt => path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Security: Validate service-to-service calls with a shared internal key.
        // The old approach (skip if no cookies) was bypassable by clients that simply
        // don't send cookies. Now we require an explicit X-Internal-Service-Key header.
        var serviceKey = context.Request.Headers["X-Internal-Service-Key"].FirstOrDefault();
        var expectedKey = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_KEY");
        if (!string.IsNullOrEmpty(serviceKey) && !string.IsNullOrEmpty(expectedKey)
            && TimingSafeEquals(serviceKey, expectedKey))
        {
            // Verified internal service call — skip CSRF
            await _next(context);
            return;
        }

        // Validate Double Submit Cookie
        var headerToken = context.Request.Headers["X-CSRF-Token"].FirstOrDefault();
        var cookieToken = context.Request.Cookies["csrf_token"];

        if (string.IsNullOrEmpty(headerToken) || string.IsNullOrEmpty(cookieToken))
        {
            Log.Warning("CSRF validation failed: Missing token. Path: {Path}, Method: {Method}, IP: {IP}",
                path, method, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "CSRF token missing" });
            return;
        }

        // Timing-safe comparison to prevent timing attacks
        if (!TimingSafeEquals(headerToken, cookieToken))
        {
            Log.Warning("CSRF validation failed: Token mismatch. Path: {Path}, Method: {Method}, IP: {IP}",
                path, method, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "CSRF token invalid" });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Timing-safe string comparison to prevent timing attacks (OWASP)
    /// </summary>
    private static bool TimingSafeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}

/// <summary>
/// Extension method to register CSRF middleware in the pipeline
/// </summary>
public static class CsrfMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfValidationMiddleware>();
    }
}
