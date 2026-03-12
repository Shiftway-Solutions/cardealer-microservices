using System.Net;
using System.Text.Json;
using KYCService.Application.Exceptions;

namespace KYCService.Api.Middleware;

/// <summary>
/// Global exception handler middleware for KYC service
/// Converts domain exceptions to appropriate HTTP responses
/// </summary>
public class KYCExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<KYCExceptionHandlerMiddleware> _logger;

    public KYCExceptionHandlerMiddleware(RequestDelegate next, ILogger<KYCExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case DuplicateProfileException ex:
                response.StatusCode = (int)HttpStatusCode.Conflict; // 409
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_DUPLICATE_PROFILE";
                _logger.LogWarning(ex, "Duplicate KYC profile attempt: {Message}", ex.Message);
                break;

            case DuplicateDocumentException ex:
                response.StatusCode = (int)HttpStatusCode.Conflict; // 409
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_DUPLICATE_DOCUMENT";
                _logger.LogWarning(ex, "Duplicate document number attempt: {Message}", ex.Message);
                break;

            case ProfileNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound; // 404
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_PROFILE_NOT_FOUND";
                _logger.LogWarning(ex, "KYC profile not found: {Message}", ex.Message);
                break;

            case InvalidProfileStatusException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest; // 400
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_INVALID_STATUS";
                _logger.LogWarning(ex, "Invalid KYC profile status: {Message}", ex.Message);
                break;

            case KYCAuthorizationException ex:
                response.StatusCode = (int)HttpStatusCode.Forbidden; // 403
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_UNAUTHORIZED";
                _logger.LogWarning(ex, "Unauthorized KYC operation: {Message}", ex.Message);
                break;

            case DocumentVerificationException ex:
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity; // 422
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_DOCUMENT_VERIFICATION_FAILED";
                _logger.LogWarning(ex, "Document verification failed: {Message}", ex.Message);
                break;

            case ConcurrencyException ex:
                response.StatusCode = (int)HttpStatusCode.Conflict; // 409
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_CONCURRENCY_CONFLICT";
                _logger.LogWarning(ex, "Concurrency conflict: {Message}", ex.Message);
                break;

            case FluentValidation.ValidationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest; // 400
                errorResponse.Message = "Validation failed";
                errorResponse.ErrorCode = "KYC_VALIDATION_FAILED";
                errorResponse.Details = ex.Errors.Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList();
                _logger.LogWarning(ex, "Validation failed: {Errors}",
                    string.Join(", ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
                break;

            case KYCException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest; // 400
                errorResponse.Message = ex.Message;
                errorResponse.ErrorCode = "KYC_ERROR";
                _logger.LogWarning(ex, "KYC error: {Message}", ex.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError; // 500
                errorResponse.Message = "An unexpected error occurred. Please try again later.";
                errorResponse.ErrorCode = "KYC_INTERNAL_ERROR";
                // Don't expose internal error details in production
                _logger.LogError(exception, "Unhandled exception in KYC service");
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<ValidationError>? Details { get; set; }
}

/// <summary>
/// Validation error detail
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Extension method to add the exception handler middleware
/// </summary>
public static class KYCExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseKYCExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<KYCExceptionHandlerMiddleware>();
    }
}
