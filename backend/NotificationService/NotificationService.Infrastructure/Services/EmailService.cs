using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Interfaces.External;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Implementación de IEmailService que usa IEmailProvider internamente
/// Este es un adaptador que conecta la interfaz de alto nivel (IEmailService)
/// con el proveedor de email específico (IEmailProvider - SendGrid, etc.)
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailProvider emailProvider, ILogger<EmailService> logger)
    {
        _emailProvider = emailProvider;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

            var (success, messageId, error) = await _emailProvider.SendAsync(to, subject, body, isHtml);

            if (success)
            {
                _logger.LogInformation("Email sent successfully to {To}. MessageId: {MessageId}", to, messageId);
            }
            else
            {
                _logger.LogWarning("Failed to send email to {To}. Error: {Error}", to, error);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email to {To}", to);
            return false;
        }
    }
}
