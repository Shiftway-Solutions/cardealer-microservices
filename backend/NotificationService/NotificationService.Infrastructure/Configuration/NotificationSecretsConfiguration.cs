using CarDealer.Shared.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Configuration;

/// <summary>
/// Extension methods para configurar NotificationService desde secretos.
/// </summary>
public static class NotificationSecretsConfiguration
{
    /// <summary>
    /// Configura NotificationSettings desde secretos y variables de entorno.
    /// Los secretos tienen prioridad sobre appsettings.json.
    /// </summary>
    public static IServiceCollection AddNotificationSecretsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ISecretProvider>(sp =>
        {
            var logger = sp.GetService<ILogger<CompositeSecretProvider>>();
            return CompositeSecretProvider.CreateDefault(logger);
        });

        // Configurar NotificationSettings con override desde secretos
        services.PostConfigure<NotificationService.Shared.NotificationSettings>(settings =>
        {
            var secretProvider = CompositeSecretProvider.CreateDefault();

            // SendGrid
            var sendGridApiKey = secretProvider.GetSecret(SecretKeys.SendGridApiKey);
            if (!string.IsNullOrEmpty(sendGridApiKey))
            {
                settings.SendGrid.ApiKey = sendGridApiKey;
            }

            var sendGridFromEmail = secretProvider.GetSecret(SecretKeys.SendGridFromEmail);
            if (!string.IsNullOrEmpty(sendGridFromEmail))
            {
                settings.SendGrid.FromEmail = sendGridFromEmail;
            }

            var sendGridFromName = secretProvider.GetSecret(SecretKeys.SendGridFromName);
            if (!string.IsNullOrEmpty(sendGridFromName))
            {
                settings.SendGrid.FromName = sendGridFromName;
            }

            // Twilio
            var twilioAccountSid = secretProvider.GetSecret(SecretKeys.TwilioAccountSid);
            if (!string.IsNullOrEmpty(twilioAccountSid))
            {
                settings.Twilio.AccountSid = twilioAccountSid;
            }

            var twilioAuthToken = secretProvider.GetSecret(SecretKeys.TwilioAuthToken);
            if (!string.IsNullOrEmpty(twilioAuthToken))
            {
                settings.Twilio.AuthToken = twilioAuthToken;
            }

            var twilioFromNumber = secretProvider.GetSecret(SecretKeys.TwilioFromNumber);
            if (!string.IsNullOrEmpty(twilioFromNumber))
            {
                settings.Twilio.FromNumber = twilioFromNumber;
            }

            // Firebase
            var firebaseProjectId = secretProvider.GetSecret(SecretKeys.FirebaseProjectId);
            if (!string.IsNullOrEmpty(firebaseProjectId))
            {
                settings.Firebase.ProjectId = firebaseProjectId;
            }

            // Resend (alternative email provider)
            var resendApiKey = secretProvider.GetSecret(SecretKeys.ResendApiKey);
            if (!string.IsNullOrEmpty(resendApiKey))
            {
                settings.Resend.ApiKey = resendApiKey;
            }

            var resendFromEmail = secretProvider.GetSecret(SecretKeys.ResendFromEmail);
            if (!string.IsNullOrEmpty(resendFromEmail))
            {
                settings.Resend.FromEmail = resendFromEmail;
            }

            var resendFromName = secretProvider.GetSecret(SecretKeys.ResendFromName);
            if (!string.IsNullOrEmpty(resendFromName))
            {
                settings.Resend.FromName = resendFromName;
            }
        });

        return services;
    }

    /// <summary>
    /// Valida que los secretos requeridos estén configurados.
    /// Retorna warnings para secretos faltantes (graceful degradation).
    /// </summary>
    public static void ValidateNotificationSecrets(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        var secretProvider = serviceProvider.GetService<ISecretProvider>()
                             ?? CompositeSecretProvider.CreateDefault();

        // Database es requerido
        if (!secretProvider.HasSecret(SecretKeys.DatabaseConnectionString) &&
            !secretProvider.HasSecret("NOTIFICATION_DATABASE_CONNECTION_STRING"))
        {
            logger.LogWarning(
                "Database connection string not found in secrets. " +
                "Set {Key} or NOTIFICATION_DATABASE_CONNECTION_STRING environment variable.",
                SecretKeys.DatabaseConnectionString);
        }

        // Email - Opcional con graceful degradation
        var hasSendGrid = secretProvider.HasSecret(SecretKeys.SendGridApiKey);
        var hasResend = secretProvider.HasSecret(SecretKeys.ResendApiKey);

        if (!hasSendGrid && !hasResend)
        {
            logger.LogWarning(
                "No email provider configured (SendGrid or Resend). Email notifications will be disabled. " +
                "Set {SendGridKey} or {ResendKey} environment variable to enable.",
                SecretKeys.SendGridApiKey, SecretKeys.ResendApiKey);
        }
        else if (!hasSendGrid && hasResend)
        {
            logger.LogInformation("SendGrid not configured but Resend credentials found. Using Resend provider.");
        }

        // SMS - Opcional con graceful degradation
        if (!secretProvider.HasSecret(SecretKeys.TwilioAccountSid))
        {
            logger.LogWarning(
                "Twilio credentials not configured. SMS notifications will be disabled. " +
                "Set {Key} environment variable to enable.",
                SecretKeys.TwilioAccountSid);
        }

        // Push - Opcional con graceful degradation
        if (!secretProvider.HasSecret(SecretKeys.FirebaseProjectId))
        {
            logger.LogWarning(
                "Firebase credentials not configured. Push notifications will be disabled. " +
                "Set {Key} environment variable to enable.",
                SecretKeys.FirebaseProjectId);
        }

        // RabbitMQ
        if (!secretProvider.HasSecret(SecretKeys.RabbitMqHost) &&
            !secretProvider.HasSecret("RABBITMQ_CONNECTION_STRING"))
        {
            logger.LogWarning(
                "RabbitMQ connection not configured. Message bus features may be limited. " +
                "Set {Key} environment variable.",
                SecretKeys.RabbitMqHost);
        }
    }
}
