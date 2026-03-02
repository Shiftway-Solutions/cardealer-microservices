using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Video360Service.Application.Interfaces;

namespace Video360Service.Infrastructure.Services;

/// <summary>
/// Eliminación de fondo usando la API de remove.bg.
/// Plan gratuito: 50 créditos/mes. Plan de pago: $0.10/imagen.
/// Docs: https://www.remove.bg/api
/// Si no se configura la API key, se omite la eliminación de fondo.
/// </summary>
public class RemoveBgService : IBackgroundRemovalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RemoveBgService> _logger;
    private readonly string? _apiKey;
    private const string ApiUrl = "https://api.remove.bg/v1.0/removebg";

    public RemoveBgService(HttpClient httpClient, IConfiguration configuration, ILogger<RemoveBgService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["BackgroundRemoval:RemoveBg:ApiKey"]
                  ?? configuration["REMOVEBG_API_KEY"];

        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<BackgroundRemovalResult> RemoveBackgroundAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogDebug("Background removal skipped: no API key configured");
            return new BackgroundRemovalResult
            {
                IsSuccess = true,
                ImageBytes = imageBytes,
                ContentType = contentType,
                WasSkipped = true
            };
        }

        try
        {
            _logger.LogDebug("Removing background from image ({Size} bytes)", imageBytes.Length);

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "image_file", "image.jpg");
            content.Add(new StringContent("auto"), "type");
            content.Add(new StringContent("rgba"), "channels");
            content.Add(new StringContent("medium"), "size"); // 'medium' for free tier up to 1500x1000

            _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

            var response = await _httpClient.PostAsync(ApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("remove.bg API error {Status}: {Body}", response.StatusCode, errorBody[..Math.Min(300, errorBody.Length)]);

                // If API fails, return original image rather than failing the whole job
                return new BackgroundRemovalResult
                {
                    IsSuccess = true,
                    ImageBytes = imageBytes,
                    ContentType = contentType,
                    WasSkipped = true,
                    ErrorMessage = $"remove.bg API returned {response.StatusCode}"
                };
            }

            var resultBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _logger.LogDebug("Background removed successfully ({Size} bytes → {ResultSize} bytes)", imageBytes.Length, resultBytes.Length);

            return new BackgroundRemovalResult
            {
                IsSuccess = true,
                ImageBytes = resultBytes,
                ContentType = "image/png" // remove.bg always returns PNG
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background removal failed, returning original image");

            // Graceful fallback: return original image without failing the job
            return new BackgroundRemovalResult
            {
                IsSuccess = true,
                ImageBytes = imageBytes,
                ContentType = contentType,
                WasSkipped = true,
                ErrorMessage = ex.Message
            };
        }
    }
}
