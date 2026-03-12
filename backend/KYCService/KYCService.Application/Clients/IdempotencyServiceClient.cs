using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KYCService.Application.Clients;

/// <summary>
/// Client for communicating with the centralized IdempotencyService microservice
/// </summary>
public interface IIdempotencyServiceClient
{
    /// <summary>
    /// Check if an idempotency key exists and get its status
    /// </summary>
    Task<IdempotencyCheckResponse> CheckAsync(string key, string? requestHash = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start processing a request with the given idempotency key
    /// </summary>
    Task<bool> StartProcessingAsync(IdempotencyStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete processing and store the response
    /// </summary>
    Task<bool> CompleteAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the request as failed
    /// </summary>
    Task<bool> FailAsync(string key, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an existing idempotency record
    /// </summary>
    Task<IdempotencyRecordResponse?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an idempotency record (for testing/cleanup)
    /// </summary>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from idempotency check
/// </summary>
public class IdempotencyCheckResponse
{
    public bool Exists { get; set; }
    public bool IsProcessing { get; set; }
    public bool IsCompleted { get; set; }
    public IdempotencyRecordResponse? Record { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequestHashMatches { get; set; } = true;
}

/// <summary>
/// Idempotency record from the service
/// </summary>
public class IdempotencyRecordResponse
{
    public string Key { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public string ResponseContentType { get; set; } = "application/json";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ClientId { get; set; }
}

/// <summary>
/// Request to start processing
/// </summary>
public class IdempotencyStartRequest
{
    public string Key { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string Path { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string? ClientId { get; set; }
}

/// <summary>
/// Request to check an idempotency key
/// </summary>
public class IdempotencyCheckRequest
{
    public string Key { get; set; } = string.Empty;
    public string? RequestHash { get; set; }
}

/// <summary>
/// Request to create a record
/// </summary>
public class IdempotencyCreateRequest
{
    public string Key { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string Path { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResponseContentType { get; set; }
    public string? Status { get; set; }
    public string? ClientId { get; set; }
}

/// <summary>
/// Implementation of IdempotencyService client using HttpClient
/// </summary>
public class IdempotencyServiceClient : IIdempotencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdempotencyServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdempotencyServiceClient(
        HttpClient httpClient,
        ILogger<IdempotencyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<IdempotencyCheckResponse> CheckAsync(string key, string? requestHash = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new IdempotencyCheckRequest
            {
                Key = key,
                RequestHash = requestHash
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/idempotency/check", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<IdempotencyCheckResponse>(responseJson, _jsonOptions);
                return result ?? new IdempotencyCheckResponse { Exists = false };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new IdempotencyCheckResponse { Exists = false };
            }

            _logger.LogWarning("Failed to check idempotency key: {StatusCode}", response.StatusCode);
            return new IdempotencyCheckResponse { Exists = false, ErrorMessage = "Service unavailable" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency key: {Key}", key);
            return new IdempotencyCheckResponse { Exists = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<bool> StartProcessingAsync(IdempotencyStartRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var createRequest = new IdempotencyCreateRequest
            {
                Key = request.Key,
                HttpMethod = request.HttpMethod,
                Path = request.Path,
                RequestHash = request.RequestHash,
                Status = "Processing",
                ClientId = request.ClientId
            };

            var json = JsonSerializer.Serialize(createRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/idempotency", content, cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogDebug("Started processing for idempotency key: {Key}", request.Key);
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Idempotency key already exists: {Key}", request.Key);
                return false;
            }

            _logger.LogWarning("Failed to start processing: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting processing for key: {Key}", request.Key);
            return false;
        }
    }

    public async Task<bool> CompleteAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken = default)
    {
        try
        {
            // Update the existing record with completed status
            var request = new IdempotencyCreateRequest
            {
                Key = key,
                ResponseStatusCode = statusCode,
                ResponseBody = responseBody,
                ResponseContentType = "application/json",
                Status = "Completed"
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/idempotency", content, cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogDebug("Completed idempotency key: {Key}", key);
                return true;
            }

            _logger.LogWarning("Failed to complete idempotency key: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing idempotency key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> FailAsync(string key, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new IdempotencyCreateRequest
            {
                Key = key,
                ResponseStatusCode = 500,
                ResponseBody = JsonSerializer.Serialize(new { error = errorMessage ?? "Operation failed" }, _jsonOptions),
                ResponseContentType = "application/json",
                Status = "Failed"
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/idempotency", content, cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking idempotency key as failed: {Key}", key);
            return false;
        }
    }

    public async Task<IdempotencyRecordResponse?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/idempotency/{Uri.EscapeDataString(key)}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<IdempotencyRecordResponse>(json, _jsonOptions);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to get idempotency record: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting idempotency record: {Key}", key);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/idempotency/{Uri.EscapeDataString(key)}", cancellationToken);

            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting idempotency record: {Key}", key);
            return false;
        }
    }
}
