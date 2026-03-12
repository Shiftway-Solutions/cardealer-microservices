using System.IO;
using System.Text;
using System.Text.Json;
using CarDealer.Shared.Idempotency.Attributes;
using CarDealer.Shared.Idempotency.Interfaces;
using CarDealer.Shared.Idempotency.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarDealer.Shared.Idempotency.Middleware;

/// <summary>
/// Middleware for handling idempotent requests
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IdempotencyMiddleware(
        RequestDelegate next,
        IOptions<IdempotencyOptions> options,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyClient idempotencyClient)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Check if this endpoint should be idempotent
        var endpoint = context.GetEndpoint();
        var idempotentAttr = endpoint?.Metadata.GetMetadata<IdempotentAttribute>();
        var skipAttr = endpoint?.Metadata.GetMetadata<SkipIdempotencyAttribute>();

        if (skipAttr != null || idempotentAttr == null)
        {
            await _next(context);
            return;
        }

        // Check if method should be checked
        var method = context.Request.Method;
        if (!_options.IdempotentMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Check if path is excluded
        var path = context.Request.Path.Value ?? "";
        if (_options.ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Get idempotency key from header
        var headerName = idempotentAttr.HeaderName ?? _options.HeaderName;
        if (!context.Request.Headers.TryGetValue(headerName, out var keyHeader) ||
            string.IsNullOrEmpty(keyHeader.FirstOrDefault()))
        {
            if (idempotentAttr.RequireKey || _options.RequireIdempotencyKey)
            {
                _logger.LogWarning("Missing idempotency key for {Method} {Path}", method, path);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var error = new
                {
                    type = "https://httpstatuses.io/400",
                    title = "Missing Idempotency Key",
                    status = 400,
                    detail = $"The '{headerName}' header is required for this request.",
                    instance = path
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error, s_jsonOptions));
                return;
            }

            // Key not required, proceed normally
            await _next(context);
            return;
        }

        var idempotencyKey = keyHeader.FirstOrDefault()!;

        // Add prefix if configured
        if (!string.IsNullOrEmpty(idempotentAttr.KeyPrefix))
        {
            idempotencyKey = $"{idempotentAttr.KeyPrefix}:{idempotencyKey}";
        }

        // Read and hash request body
        string requestBody = "";
        string requestHash = "";

        if (idempotentAttr.IncludeBodyInHash)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            requestHash = idempotencyClient.GenerateRequestHash(requestBody);
        }

        // Check if request already processed
        var checkResult = await idempotencyClient.CheckAsync(idempotencyKey, requestHash);

        if (checkResult.IsConflict)
        {
            _logger.LogWarning(
                "Idempotency conflict for key {Key}: {Error}",
                idempotencyKey, checkResult.ErrorMessage);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";

            var error = new
            {
                type = "https://httpstatuses.io/409",
                title = "Idempotency Conflict",
                status = 409,
                detail = checkResult.ErrorMessage,
                instance = path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error, s_jsonOptions));
            return;
        }

        if (checkResult.IsProcessing)
        {
            _logger.LogWarning("Request with key {Key} is still being processed", idempotencyKey);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";

            var error = new
            {
                type = "https://httpstatuses.io/409",
                title = "Request In Progress",
                status = 409,
                detail = "A request with this idempotency key is currently being processed. Please wait and retry.",
                instance = path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error, s_jsonOptions));
            return;
        }

        if (checkResult.IsCompleted && checkResult.Record != null)
        {
            // Return cached response
            _logger.LogInformation("Replaying cached response for idempotency key {Key}", idempotencyKey);

            context.Response.StatusCode = checkResult.Record.ResponseStatusCode;
            context.Response.ContentType = checkResult.Record.ResponseContentType;
            context.Response.Headers["X-Idempotency-Replayed"] = "true";

            // Add cached headers
            foreach (var header in checkResult.Record.ResponseHeaders)
            {
                context.Response.Headers[header.Key] = header.Value;
            }

            await context.Response.WriteAsync(checkResult.Record.ResponseBody);
            return;
        }

        // New request - start processing
        var record = new IdempotencyRecord
        {
            Key = idempotencyKey,
            HttpMethod = method,
            Path = path,
            RequestHash = requestHash,
            ClientId = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User.FindFirst("sub")?.Value
        };

        var started = await idempotencyClient.StartProcessingAsync(record);

        if (!started)
        {
            // Race condition - another request started processing
            _logger.LogWarning("Failed to start processing - another request won the race for key {Key}", idempotencyKey);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";

            var error = new
            {
                type = "https://httpstatuses.io/409",
                title = "Request Already Processing",
                status = 409,
                detail = "Another request with this idempotency key is being processed.",
                instance = path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error, s_jsonOptions));
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
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            // Extract headers to cache
            var headersToCache = new Dictionary<string, string>();
            foreach (var header in context.Response.Headers.Where(h =>
                h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)))
            {
                headersToCache[header.Key] = header.Value.ToString();
            }

            // Complete the idempotency record
            await idempotencyClient.CompleteAsync(
                idempotencyKey,
                context.Response.StatusCode,
                responseContent,
                context.Response.ContentType ?? "application/json",
                headersToCache);

            // Copy to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request with idempotency key {Key}", idempotencyKey);
            await idempotencyClient.FailAsync(idempotencyKey, ex.Message);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
