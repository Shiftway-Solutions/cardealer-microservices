using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Shared;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infrastructure.External;

public class ResendEmailService : IEmailProvider
{
    private readonly HttpClient _httpClient;
    private readonly string? _fromEmail;
    private readonly string? _fromName;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly bool _isConfigured;
    private const string ResendApiUrl = "https://api.resend.com/emails";

    public ResendEmailService(
        IOptions<NotificationSettings> settings,
        ILogger<ResendEmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Resend");

        var resendSettings = settings.Value.Resend;

        // Verificar si está configurado
        if (string.IsNullOrWhiteSpace(resendSettings?.ApiKey))
        {
            _logger.LogWarning("Resend API Key not configured. Email sending will be mocked.");
            _isConfigured = false;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", resendSettings.ApiKey);
        _fromEmail = resendSettings.FromEmail;
        _fromName = resendSettings.FromName;
        _isConfigured = true;
    }

    public string ProviderName => "Resend";

    public async Task<(bool success, string? messageId, string? error)> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        Dictionary<string, object>? metadata = null)
    {
        // Si no está configurado, simular envío exitoso
        if (!_isConfigured)
        {
            _logger.LogInformation("[MOCK] Email would be sent to {To} with subject: {Subject}", to, subject);
            return (true, $"mock-{Guid.NewGuid()}", null);
        }

        try
        {
            var fromAddress = string.IsNullOrEmpty(_fromName)
                ? _fromEmail
                : $"{_fromName} <{_fromEmail}>";

            var payload = new
            {
                from = fromAddress,
                to = new[] { to },
                subject = subject,
                html = isHtml ? body : null,
                text = isHtml ? null : body
            };

            var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending email via Resend to {To} with subject: {Subject}", to, subject);

            var response = await _httpClient.PostAsync(ResendApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully via Resend to {To}", to);

                // Parse response to get message ID
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var messageId = doc.RootElement.GetProperty("id").GetString();
                    return (true, messageId, null);
                }
                catch
                {
                    return (true, null, null);
                }
            }
            else
            {
                _logger.LogWarning("Failed to send email via Resend to {To}. Status: {StatusCode}, Response: {Response}",
                    to, response.StatusCode, responseBody);
                return (false, null, $"Resend API error: {response.StatusCode} - {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via Resend to {To}", to);
            return (false, null, ex.Message);
        }
    }
}
