using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AdvertisingService.Application.Clients;

public class NotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification using userId + templateKey (legacy method).
    /// Note: This sends to /api/notifications/send which requires the NotificationService
    /// to resolve the user's email internally.
    /// </summary>
    public async Task SendNotificationAsync(
        Guid userId,
        string templateKey,
        Dictionary<string, string> placeholders,
        CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                UserId = userId,
                TemplateKey = templateKey,
                Channel = "email",
                Placeholders = placeholders
            };

            await _httpClient.PostAsJsonAsync("/api/notifications/send", request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    /// <summary>
    /// Sends a pre-rendered HTML email directly via the NotificationService email endpoint.
    /// This is the correct method that works with the current NotificationService API.
    /// </summary>
    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                To = toEmail,
                Subject = subject,
                HtmlBody = htmlBody,
                IsHtml = true
            };

            var response = await _httpClient.PostAsJsonAsync("/api/notifications/email", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to send email to {Email}: {StatusCode} - {Body}",
                    toEmail, response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
