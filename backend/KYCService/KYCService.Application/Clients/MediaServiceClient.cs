using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KYCService.Application.Clients;

/// <summary>
/// Client for communicating with MediaService to get fresh pre-signed URLs
/// </summary>
public interface IMediaServiceClient
{
    /// <summary>
    /// Get a fresh pre-signed URL for a storage key
    /// The returned URL is valid for 1 hour
    /// </summary>
    Task<MediaUrlResponse?> GetFreshUrlAsync(string storageKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from MediaService for URL generation
/// </summary>
public class MediaUrlResponse
{
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string StorageKey { get; set; } = string.Empty;
}

/// <summary>
/// Implementation of MediaService client using HttpClient
/// </summary>
public class MediaServiceClient : IMediaServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MediaServiceClient> _logger;
    private readonly string _baseUrl;

    public MediaServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MediaServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Get MediaService URL from configuration
        _baseUrl = configuration["Services:MediaService:BaseUrl"]
            ?? configuration["MediaService:BaseUrl"]
            ?? "http://mediaservice:8080";

        _logger.LogInformation("MediaServiceClient initialized with base URL: {BaseUrl}", _baseUrl);
    }

    public async Task<MediaUrlResponse?> GetFreshUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            _logger.LogWarning("GetFreshUrlAsync called with empty storageKey");
            return null;
        }

        try
        {
            // URL encode the storage key for the query string
            var encodedKey = Uri.EscapeDataString(storageKey);
            var requestUrl = $"{_baseUrl}/api/media/url?storageKey={encodedKey}";

            _logger.LogDebug("Requesting fresh URL for storageKey: {StorageKey}", storageKey);

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get fresh URL from MediaService. Status: {StatusCode}, StorageKey: {StorageKey}",
                    response.StatusCode, storageKey);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MediaUrlResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null)
            {
                _logger.LogDebug("Successfully got fresh URL for storageKey: {StorageKey}", storageKey);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting fresh URL for storageKey: {StorageKey}", storageKey);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout getting fresh URL for storageKey: {StorageKey}", storageKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting fresh URL for storageKey: {StorageKey}", storageKey);
            return null;
        }
    }
}
