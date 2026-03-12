using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Shared;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotificationService.Infrastructure.External;

public class SendGridEmailService : IEmailProvider
{
    private readonly SendGridClient? _client;
    private readonly EmailAddress? _fromAddress;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly bool _isConfigured;

    public SendGridEmailService(
        IOptions<NotificationSettings> settings,
        ILogger<SendGridEmailService> logger)
    {
        _logger = logger;
        var sendGridSettings = settings.Value.SendGrid;

        // Verificar si está configurado
        if (string.IsNullOrWhiteSpace(sendGridSettings?.ApiKey))
        {
            _logger.LogWarning("SendGrid API Key not configured. Email sending will be mocked.");
            _isConfigured = false;
            return;
        }

        _client = new SendGridClient(sendGridSettings.ApiKey);
        _fromAddress = new EmailAddress(sendGridSettings.FromEmail, sendGridSettings.FromName);
        _isConfigured = true;
    }

    public string ProviderName => "SendGrid";

    public async Task<(bool success, string? messageId, string? error)> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        Dictionary<string, object>? metadata = null)
    {
        // Si no está configurado, simular envío exitoso
        if (!_isConfigured || _client == null || _fromAddress == null)
        {
            _logger.LogInformation("[MOCK] Email would be sent to {To} with subject: {Subject}", to, subject);
            return (true, $"mock-{Guid.NewGuid()}", null);
        }

        try
        {
            var toEmail = new EmailAddress(to);
            var message = MailHelper.CreateSingleEmail(_fromAddress, toEmail, subject,
                isHtml ? null : body, isHtml ? body : null);

            if (metadata != null)
            {
                message.AddHeaders(metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""));
            }

            var response = await _client.SendEmailAsync(message);
            var responseBody = await response.Body.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {To}", to);
                return (true, response.Headers.GetValues("X-Message-Id").FirstOrDefault(), null);
            }
            else
            {
                _logger.LogWarning("Failed to send email to {To}. Status: {StatusCode}", to, response.StatusCode);
                return (false, null, $"SendGrid API error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            return (false, null, ex.Message);
        }
    }
}