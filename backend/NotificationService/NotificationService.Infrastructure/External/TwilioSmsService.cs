using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Interfaces.External; // ✅ CORRECTO - Domain
using NotificationService.Shared;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NotificationService.Infrastructure.External;

public class TwilioSmsService : ISmsProvider // ✅ Implementa interfaz de Domain
{
    private readonly string? _fromNumber;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly bool _isConfigured;

    public TwilioSmsService(
        IOptions<NotificationSettings> settings,
        ILogger<TwilioSmsService> logger)
    {
        _logger = logger;
        var twilioSettings = settings.Value.Twilio;

        // Verificar si está configurado
        if (string.IsNullOrWhiteSpace(twilioSettings?.AccountSid) ||
            string.IsNullOrWhiteSpace(twilioSettings?.AuthToken))
        {
            _logger.LogWarning("Twilio credentials not configured. SMS sending will be mocked.");
            _isConfigured = false;
            return;
        }

        TwilioClient.Init(twilioSettings.AccountSid, twilioSettings.AuthToken);
        _fromNumber = twilioSettings.FromNumber;
        _isConfigured = true;
    }

    public string ProviderName => "Twilio";

    public async Task<(bool success, string? messageId, string? error)> SendAsync(
        string to,
        string message,
        Dictionary<string, object>? metadata = null)
    {
        // Si no está configurado, simular envío exitoso
        if (!_isConfigured || string.IsNullOrWhiteSpace(_fromNumber))
        {
            _logger.LogInformation("[MOCK] SMS would be sent to {To}: {Message}", to, message);
            return (true, $"mock-sms-{Guid.NewGuid()}", null);
        }

        try
        {
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(to)
            );

            if (messageResource.Status != MessageResource.StatusEnum.Failed)
            {
                _logger.LogInformation("SMS sent successfully to {To}", to);
                return (true, messageResource.Sid, null);
            }
            else
            {
                _logger.LogWarning("Failed to send SMS to {To}. Status: {Status}", to, messageResource.Status);
                return (false, null, $"Twilio error: {messageResource.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {To}", to);
            return (false, null, ex.Message);
        }
    }
}