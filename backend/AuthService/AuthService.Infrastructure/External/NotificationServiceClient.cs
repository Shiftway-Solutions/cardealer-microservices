using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Shared;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthService.Shared.Exceptions;
using ServiceDiscovery.Application.Interfaces;

namespace AuthService.Infrastructure.External;

public class NotificationServiceClient : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly NotificationServiceSettings _settings;
    private readonly ILogger<NotificationServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IServiceDiscovery _serviceDiscovery;

    public NotificationServiceClient(
        HttpClient httpClient,
        IOptions<NotificationServiceSettings> settings,
        ILogger<NotificationServiceClient> logger,
        IServiceDiscovery serviceDiscovery)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _serviceDiscovery = serviceDiscovery;

        // Configurar HttpClient (no establecer BaseAddress aquí, se resolverá dinámicamente)
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AuthService");

        // Configurar opciones de JSON
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private async Task<string> GetNotificationServiceUrlAsync()
    {
        try
        {
            var instance = await _serviceDiscovery.FindServiceInstanceAsync("NotificationService");
            if (instance != null)
            {
                return $"http://{instance.Host}:{instance.Port}";
            }
            _logger.LogWarning("NotificationService not found in Consul, falling back to configured BaseUrl: {BaseUrl}", _settings.BaseUrl);
            return _settings.BaseUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving NotificationService from Consul, falling back to configured BaseUrl: {BaseUrl}", _settings.BaseUrl);
            return _settings.BaseUrl;
        }
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, Dictionary<string, object>? metadata = null)
    {
        if (!_settings.EnableNotifications)
        {
            _logger.LogWarning("Notifications are disabled. Email to {To} was not sent.", to);
            return true; // Simular éxito si las notificaciones están deshabilitadas
        }

        try
        {
            var baseUrl = await GetNotificationServiceUrlAsync();

            // Usar el DTO exacto que espera el NotificationService
            var request = new
            {
                To = to,
                Subject = subject,
                Body = body,
                IsHtml = isHtml,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/notifications/email", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                // Leer la respuesta para verificar que se procesó correctamente
                var responseContent = await response.Content.ReadAsStringAsync();
                var emailResponse = JsonSerializer.Deserialize<EmailNotificationResponse>(responseContent, _jsonOptions);

                if (emailResponse != null && emailResponse.Status?.ToLower() == "sent")
                {
                    _logger.LogInformation("Email sent successfully to {To}, NotificationId: {NotificationId}", to, emailResponse.NotificationId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Email to {To} was accepted but may not have been sent. Response: {Response}", to, responseContent);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send email to {To}. Status: {StatusCode}, Error: {Error}",
                    to, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendSmsAsync(string to, string message, Dictionary<string, object>? metadata = null)
    {
        if (!_settings.EnableNotifications)
        {
            _logger.LogWarning("Notifications are disabled. SMS to {To} was not sent.", to);
            return true;
        }

        try
        {
            var baseUrl = await GetNotificationServiceUrlAsync();

            var request = new
            {
                To = to,
                Message = message,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/notifications/sms", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var smsResponse = JsonSerializer.Deserialize<SmsNotificationResponse>(responseContent, _jsonOptions);

                if (smsResponse != null && smsResponse.Status?.ToLower() == "sent")
                {
                    _logger.LogInformation("SMS sent successfully to {To}, NotificationId: {NotificationId}", to, smsResponse.NotificationId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("SMS to {To} was accepted but may not have been sent. Response: {Response}", to, responseContent);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send SMS to {To}. Status: {StatusCode}, Error: {Error}",
                    to, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendPushAsync(string deviceToken, string title, string body, object? data = null, Dictionary<string, object>? metadata = null)
    {
        if (!_settings.EnableNotifications)
        {
            _logger.LogWarning("Notifications are disabled. Push to {DeviceToken} was not sent.", deviceToken);
            return true;
        }

        try
        {
            var baseUrl = await GetNotificationServiceUrlAsync();

            var request = new
            {
                DeviceToken = deviceToken,
                Title = title,
                Body = body,
                Data = data,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/notifications/push", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var pushResponse = JsonSerializer.Deserialize<PushNotificationResponse>(responseContent, _jsonOptions);

                if (pushResponse != null && pushResponse.Status?.ToLower() == "sent")
                {
                    _logger.LogInformation("Push notification sent successfully to device {DeviceToken}, NotificationId: {NotificationId}", deviceToken, pushResponse.NotificationId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Push to {DeviceToken} was accepted but may not have been sent. Response: {Response}", deviceToken, responseContent);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send push notification to {DeviceToken}. Status: {StatusCode}, Error: {Error}",
                    deviceToken, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to {DeviceToken}", deviceToken);
            return false;
        }
    }

    public async Task<NotificationStatusResponse?> GetNotificationStatusAsync(Guid notificationId)
    {
        try
        {
            var baseUrl = await GetNotificationServiceUrlAsync();
            var response = await _httpClient.GetAsync($"{baseUrl}/api/notifications/{notificationId}/status");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NotificationStatusResponse>(responseContent, _jsonOptions);
            }
            else
            {
                _logger.LogWarning("Failed to get status for notification {NotificationId}. Status: {StatusCode}", notificationId, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for notification {NotificationId}", notificationId);
            return null;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var baseUrl = await GetNotificationServiceUrlAsync();
            var response = await _httpClient.GetAsync($"{baseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification service health check failed");
            return false;
        }
    }

    #region INotificationService Implementation

    /// <summary>
    /// INotificationService.SendEmailAsync implementation - delegates to existing method
    /// </summary>
    async Task INotificationService.SendEmailAsync(string to, string subject, string body, bool isHtml)
    {
        var result = await SendEmailAsync(to, subject, body, isHtml);
        if (!result)
        {
            throw new InvalidOperationException($"Failed to send email to {to}");
        }
    }

    /// <summary>
    /// INotificationService.SendSmsAsync implementation - delegates to existing method
    /// </summary>
    async Task INotificationService.SendSmsAsync(string to, string message)
    {
        var result = await SendSmsAsync(to, message);
        if (!result)
        {
            throw new InvalidOperationException($"Failed to send SMS to {to}");
        }
    }

    /// <summary>
    /// INotificationService.SendPushAsync implementation
    /// </summary>
    async Task INotificationService.SendPushAsync(string deviceToken, string title, string body)
    {
        var result = await SendPushAsync(deviceToken, title, body);
        if (!result)
        {
            throw new InvalidOperationException($"Failed to send push notification to {deviceToken}");
        }
    }

    /// <summary>
    /// INotificationService.IsHealthyAsync implementation
    /// </summary>
    Task<bool> INotificationService.IsHealthyAsync()
    {
        return IsHealthyAsync();
    }

    #endregion

}
