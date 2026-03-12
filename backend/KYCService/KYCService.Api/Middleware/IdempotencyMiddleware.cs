using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KYCService.Application.Clients;

namespace KYCService.Api.Middleware;

/// <summary>
/// Middleware for handling idempotent requests using the centralized IdempotencyService
/// Prevents duplicate processing of POST/PUT/PATCH requests
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    // HTTP methods that should be idempotent
    private static readonly HashSet<string> IdempotentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH"
    };

    // Endpoints that require idempotency
    private static readonly HashSet<string> IdempotentEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/kyc/profiles",
        "/api/kyc/documents",
        "/api/identity-verification/process",
        "/api/identity-verification/verify",
        "/api/kyc/submit-for-review",
        "/api/kycprofiles"
    };

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyServiceClient idempotencyClient, IAuditServiceClient auditClient)
    {
        // Only apply to idempotent methods on specific endpoints
        if (!ShouldApplyIdempotency(context))
        {
            await _next(context);
            return;
        }

        // Get idempotency key from header
        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // SECURITY: Require idempotency key on mutation endpoints to prevent duplicates
            _logger.LogWarning("Request to {Path} without required idempotency key", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "X-Idempotency-Key header is required for this operation",
                code = "MISSING_IDEMPOTENCY_KEY"
            }));
            return;
        }

        // Get user ID from claims for audit
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? context.User.FindFirst("sub")?.Value
                       ?? context.User.FindFirst("user_id")?.Value
                       ?? "anonymous";

        // Generate request hash for conflict detection
        var requestHash = await GenerateRequestHashAsync(context);

        // Check IdempotencyService for existing key
        IdempotencyCheckResponse? checkResult = null;
        try
        {
            checkResult = await idempotencyClient.CheckAsync(idempotencyKey, requestHash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IdempotencyService check failed, proceeding without idempotency check");
            // If IdempotencyService is unavailable, proceed normally
            await _next(context);
            return;
        }

        if (checkResult != null && checkResult.Exists)
        {
            if (checkResult.IsProcessing)
            {
                // Request is still being processed - return 409 Conflict
                _logger.LogWarning("Duplicate request detected while processing. Key: {Key}, User: {UserId}",
                    idempotencyKey, userIdClaim);

                // Log to audit service
                _ = auditClient.LogKYCEventAsync(
                    userIdClaim,
                    KYCAuditActions.DuplicateRequestBlocked,
                    $"kyc-idempotency:{idempotencyKey}",
                    GetClientIp(context),
                    context.Request.Headers["User-Agent"].FirstOrDefault(),
                    success: false,
                    errorMessage: "Duplicate request blocked - still processing",
                    additionalData: new Dictionary<string, object>
                    {
                        { "idempotencyKey", idempotencyKey },
                        { "path", context.Request.Path.Value ?? "" }
                    });

                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Request is already being processed",
                    errorCode = "DUPLICATE_REQUEST_IN_PROGRESS",
                    idempotencyKey
                }));
                return;
            }

            if (checkResult.IsCompleted && checkResult.Record != null)
            {
                // Request was already completed - return cached response
                _logger.LogInformation("Returning cached response for idempotent request. Key: {Key}", idempotencyKey);

                context.Response.StatusCode = checkResult.Record.ResponseStatusCode;
                context.Response.ContentType = checkResult.Record.ResponseContentType;
                if (!string.IsNullOrEmpty(checkResult.Record.ResponseBody))
                {
                    await context.Response.WriteAsync(checkResult.Record.ResponseBody);
                }
                return;
            }

            // Check for hash mismatch (different request body with same key)
            if (!checkResult.RequestHashMatches)
            {
                _logger.LogWarning("Idempotency key reused with different request. Key: {Key}", idempotencyKey);

                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Idempotency key was used with a different request body",
                    errorCode = "IDEMPOTENCY_KEY_REUSED",
                    idempotencyKey
                }));
                return;
            }
        }

        // Start processing with IdempotencyService
        var startRequest = new IdempotencyStartRequest
        {
            Key = idempotencyKey,
            HttpMethod = context.Request.Method,
            Path = context.Request.Path.Value ?? "/",
            RequestHash = requestHash,
            ClientId = userIdClaim
        };

        bool started = false;
        try
        {
            started = await idempotencyClient.StartProcessingAsync(startRequest);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IdempotencyService start failed, proceeding without idempotency");
            // If IdempotencyService is unavailable, proceed normally
            await _next(context);
            return;
        }

        if (!started)
        {
            // Failed to start - might be a race condition
            _logger.LogWarning("Failed to start idempotency processing for key: {Key}", idempotencyKey);

            // Check again if it exists now
            var recheckResult = await idempotencyClient.CheckAsync(idempotencyKey, requestHash);
            if (recheckResult.Exists)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Request is already being processed",
                    errorCode = "DUPLICATE_REQUEST_IN_PROGRESS",
                    idempotencyKey
                }));
                return;
            }

            // Proceed without idempotency if service is unavailable
            _logger.LogWarning("IdempotencyService unavailable, proceeding without idempotency");
            await _next(context);
            return;
        }

        // Capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Read the response
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();

            // Complete the idempotency record
            await idempotencyClient.CompleteAsync(
                idempotencyKey,
                context.Response.StatusCode,
                responseText);

            // Write response to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing idempotent request");

            // Mark as failed in IdempotencyService
            await idempotencyClient.FailAsync(idempotencyKey, ex.Message);

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldApplyIdempotency(HttpContext context)
    {
        if (!IdempotentMethods.Contains(context.Request.Method))
            return false;

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        return IdempotentEndpoints.Any(e => path.Contains(e, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GenerateRequestHashAsync(HttpContext context)
    {
        try
        {
            // Enable buffering so we can read the body multiple times
            context.Request.EnableBuffering();

            // Read the request body
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Create hash from method + path + body
            var content = $"{context.Request.Method}:{context.Request.Path}:{body}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extension method to add idempotency middleware
/// </summary>
public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IdempotencyMiddleware>();
    }
}
