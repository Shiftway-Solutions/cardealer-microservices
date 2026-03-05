using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace CarDealer.Shared.Logging.Middleware;

/// <summary>
/// Middleware that enriches logs with request context (TraceId, UserId, RequestPath, etc.)
/// Security (CWE-532): Sensitive headers (Authorization, Cookie, etc.) are NEVER logged.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Headers that must NEVER appear in log output to prevent token leakage.
    /// </summary>
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-CSRF-Token",
        "X-Api-Key"
    };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var spanId = Activity.Current?.SpanId.ToString() ?? "N/A";

        // Extract user info if authenticated
        var userId = context.User?.FindFirst("sub")?.Value
                  ?? context.User?.FindFirst("userId")?.Value
                  ?? "anonymous";

        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                         ?? Guid.NewGuid().ToString("N");

        // Set correlation ID in response headers
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Response.Headers["X-Trace-Id"] = traceId;

        // Enrich all logs within this request scope
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("SpanId", spanId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("ClientIP", GetClientIp(context)))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown"))
        {
            try
            {
                await _next(context);

                stopwatch.Stop();

                var statusCode = context.Response.StatusCode;
                var level = statusCode >= 500 ? LogLevel.Error
                          : statusCode >= 400 ? LogLevel.Warning
                          : LogLevel.Information;

                _logger.Log(level,
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "HTTP {Method} {Path} failed after {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);

                throw; // Re-throw to let other middleware handle the exception
            }
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded headers (behind load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
