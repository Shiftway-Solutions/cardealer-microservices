using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using CarDealer.Shared.ErrorHandling.Interfaces;
using CarDealer.Shared.ErrorHandling.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarDealer.Shared.ErrorHandling.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions,
/// returns standardized ProblemDetails responses, and publishes to ErrorService
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly ErrorHandlingOptions _options;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IOptions<ErrorHandlingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IErrorPublisher errorPublisher)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorPublisher);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        IErrorPublisher errorPublisher)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var spanId = Activity.Current?.SpanId.ToString();
        // Capture request properties before any async gap — the HttpContext may be
        // disposed once the response is sent (fire-and-forget tasks would see a closed stream)
        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path.ToString();
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var clientIp = GetClientIp(context);
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
        var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("userId")?.Value;

        // Log at appropriate level: client errors (4xx) as Warning, server errors (5xx) as Error
        var isClientError = TryGetServiceException(exception, out var exStatusCode, out _) && exStatusCode >= 400 && exStatusCode < 500;
        if (isClientError || exception is UnauthorizedAccessException || exception is KeyNotFoundException || exception is ValidationException)
        {
            _logger.LogWarning(exception,
                "Client error in {Method} {Path}: {Message}",
                requestMethod, requestPath, exception.Message);
        }
        else
        {
            _logger.LogError(exception,
                "Unhandled exception in {Method} {Path}: {Message}",
                requestMethod, requestPath, exception.Message);
        }

        // Publish to ErrorService (non-blocking) — use captured values, not context
        _ = PublishErrorAsync(exception, errorPublisher, traceId, spanId, correlationId,
            requestPath, requestMethod, userId, clientIp, userAgent);

        // Guard: if the response has already started (e.g. client disconnected), do not attempt
        // to write to the closed stream — this prevents ObjectDisposedException bubbling up.
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response already started for {Method} {Path}; cannot write error response for: {ExType}",
                requestMethod, requestPath, exception.GetType().Name);
            return;
        }

        // Build the response
        var problemDetails = CreateProblemDetails(exception, traceId, requestPath);

        try
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problemDetails.Status == 0 ? 500 : problemDetails.Status;
            await context.Response.WriteAsync(problemDetails.ToJson());
        }
        catch (ObjectDisposedException ode)
        {
            _logger.LogWarning(ode,
                "Stream disposed before error response could be written for {Method} {Path}",
                requestMethod, requestPath);
        }
        catch (InvalidOperationException ioe)
        {
            _logger.LogWarning(ioe,
                "Cannot write error response (response already started?) for {Method} {Path}",
                requestMethod, requestPath);
        }
    }

    private async Task PublishErrorAsync(
        Exception exception,
        IErrorPublisher errorPublisher,
        string? traceId,
        string? spanId,
        string? correlationId,
        string requestPath,
        string requestMethod,
        string? userId,
        string clientIp,
        string? userAgent)
    {
        try
        {
            var errorContext = new ErrorContext
            {
                RequestPath = requestPath,
                RequestMethod = requestMethod,
                UserId = userId,
                CorrelationId = correlationId,
                TraceId = traceId,
                SpanId = spanId,
                ClientIp = clientIp,
                UserAgent = userAgent
            };

            await errorPublisher.PublishExceptionAsync(exception, errorContext);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish error to ErrorService");
        }
    }

    private Models.ProblemDetails CreateProblemDetails(Exception exception, string? traceId, string path)
    {
        return exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(validationEx, traceId, path),
            UnauthorizedAccessException => Models.ProblemDetails.Unauthorized(
                exception.Message, traceId),
            KeyNotFoundException => Models.ProblemDetails.NotFound(
                exception.Message, traceId, path),
            InvalidOperationException when exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => Models.ProblemDetails.NotFound(exception.Message, traceId, path),
            OperationCanceledException => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Request was cancelled.",
                Status = 499, // Client Closed Request
                Detail = "The request was cancelled by the client.",
                TraceId = traceId,
                ErrorCode = "REQUEST_CANCELLED"
            },
            TimeoutException => new Models.ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                Title = "Gateway Timeout",
                Status = (int)HttpStatusCode.GatewayTimeout,
                Detail = "The request timed out.",
                TraceId = traceId,
                ErrorCode = "TIMEOUT"
            },
            // Handle service-specific exceptions that carry StatusCode/ErrorCode via reflection
            // This covers AuthServiceException and similar base classes from any microservice
            _ when TryGetServiceException(exception, out var statusCode, out var errorCode) => new Models.ProblemDetails
            {
                Type = statusCode switch
                {
                    400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
                    403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                    _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                },
                Title = errorCode ?? "Error",
                Status = statusCode,
                Detail = exception.Message,
                TraceId = traceId,
                ErrorCode = errorCode ?? "UNKNOWN_ERROR"
            },
            _ => Models.ProblemDetails.InternalServerError(
                _options.IncludeExceptionDetails ? exception.Message : null,
                traceId,
                _options.IncludeExceptionDetails)
        };
    }

    // Cache reflection lookups per exception type to avoid repeated reflection on hot path
    private static readonly ConcurrentDictionary<Type, (PropertyInfo? StatusCode, PropertyInfo? ErrorCode)> s_exceptionPropertyCache = new();

    /// <summary>
    /// Tries to extract StatusCode and ErrorCode from service-specific exceptions
    /// (e.g., AuthServiceException, or any exception with int StatusCode and string ErrorCode properties).
    /// Uses cached reflection for performance.
    /// </summary>
    private static bool TryGetServiceException(Exception exception, out int statusCode, out string? errorCode)
    {
        var type = exception.GetType();
        var (statusCodeProp, errorCodeProp) = s_exceptionPropertyCache.GetOrAdd(type, t =>
            (t.GetProperty("StatusCode", typeof(int)), t.GetProperty("ErrorCode", typeof(string))));

        if (statusCodeProp != null)
        {
            statusCode = (int)(statusCodeProp.GetValue(exception) ?? 500);
            errorCode = errorCodeProp?.GetValue(exception) as string;
            return true;
        }

        statusCode = 500;
        errorCode = null;
        return false;
    }

    private static Models.ProblemDetails CreateValidationProblemDetails(
        ValidationException validationEx,
        string? traceId,
        string path)
    {
        var errors = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Models.ProblemDetails.ValidationError(errors, traceId, path);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
