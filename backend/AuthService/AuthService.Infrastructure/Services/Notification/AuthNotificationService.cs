using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Services;
using AuthService.Infrastructure.External;
using AuthService.Infrastructure.Services.Messaging;
using AuthService.Shared;
using AuthService.Shared.NotificationMessages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services.Notification;

public class AuthNotificationService : IAuthNotificationService
{
    private readonly NotificationServiceClient _notificationServiceClient;
    private readonly INotificationEventProducer _notificationProducer;
    private readonly NotificationServiceSettings _settings;
    private readonly NotificationServiceRabbitMQSettings _rabbitMqSettings;
    private readonly ILogger<AuthNotificationService> _logger;

    public AuthNotificationService(
        NotificationServiceClient notificationServiceClient,
        INotificationEventProducer notificationProducer,
        IOptions<NotificationServiceSettings> settings,
        IOptions<NotificationServiceRabbitMQSettings> rabbitMqSettings,
        ILogger<AuthNotificationService> logger)
    {
        _notificationServiceClient = notificationServiceClient;
        _notificationProducer = notificationProducer;
        _settings = settings.Value;
        _rabbitMqSettings = rabbitMqSettings.Value; // .Value para obtener la instancia
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        var resetUrl = $"{_settings.FrontendBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendPasswordResetViaRabbitMQ(email, resetToken, resetUrl);
        }
        else
        {
            await SendPasswordResetViaHttp(email, resetToken, resetUrl);
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendWelcomeViaRabbitMQ(email, username);
        }
        else
        {
            await SendWelcomeViaHttp(email, username);
        }
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken)
    {
        var confirmUrl = $"{_settings.FrontendBaseUrl}/verificar-email?token={Uri.EscapeDataString(confirmationToken)}";

        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendEmailConfirmationViaRabbitMQ(email, confirmationToken, confirmUrl);
        }
        else
        {
            await SendEmailConfirmationViaHttp(email, confirmationToken, confirmUrl);
        }
    }

    public async Task SendTwoFactorCodeAsync(string destination, string code, TwoFactorAuthType method)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendTwoFactorCodeViaRabbitMQ(destination, code, method);
        }
        else
        {
            await SendTwoFactorCodeViaHttp(destination, code, method);
        }
    }

    public async Task SendTwoFactorBackupCodesAsync(string email, List<string> backupCodes)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendTwoFactorBackupCodesViaRabbitMQ(email, backupCodes);
        }
        else
        {
            await SendTwoFactorBackupCodesViaHttp(email, backupCodes);
        }
    }

    public async Task SendNewAuthenticatorSetupAsync(string email, string secret, string qrCodeUri, List<string> recoveryCodes)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendNewAuthenticatorSetupViaRabbitMQ(email, secret, qrCodeUri, recoveryCodes);
        }
        else
        {
            await SendNewAuthenticatorSetupViaHttp(email, secret, qrCodeUri, recoveryCodes);
        }
    }

    public async Task SendPasswordChangedConfirmationAsync(string email)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendPasswordChangedConfirmationViaRabbitMQ(email);
        }
        else
        {
            await SendPasswordChangedConfirmationViaHttp(email);
        }
    }

    #region RabbitMQ Methods

    private async Task SendPasswordResetViaRabbitMQ(string email, string resetToken, string resetUrl)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "Password Reset Request",
                TemplateName = "PasswordReset",
                Body = GeneratePasswordResetEmailBody(resetUrl),
                Data = new Dictionary<string, object>
                {
                    ["resetUrl"] = resetUrl,
                    ["expiryHours"] = 1,
                    ["token"] = resetToken
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("Password reset notification sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendPasswordResetViaHttp(email, resetToken, resetUrl);
        }
    }

    private async Task SendWelcomeViaRabbitMQ(string email, string username)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "Welcome to Our Platform!",
                TemplateName = "Welcome",
                Body = GenerateWelcomeEmailBody(username),
                Data = new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["loginUrl"] = $"{_settings.FrontendBaseUrl}/login",
                    ["supportEmail"] = "support@example.com"
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("Welcome email sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendWelcomeViaHttp(email, username);
        }
    }

    private async Task SendEmailConfirmationViaRabbitMQ(string email, string confirmationToken, string confirmUrl)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "Confirm Your Email Address",
                TemplateName = "EmailConfirmation",
                Body = GenerateEmailConfirmationBody(confirmUrl),
                Data = new Dictionary<string, object>
                {
                    ["confirmationUrl"] = confirmUrl,
                    ["expiryHours"] = 24,
                    ["token"] = confirmationToken
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("Email confirmation sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send email confirmation via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendEmailConfirmationViaHttp(email, confirmationToken, confirmUrl);
        }
    }

    private async Task SendTwoFactorCodeViaRabbitMQ(string destination, string code, TwoFactorAuthType method)
    {
        try
        {
            NotificationEvent notification = method switch
            {
                TwoFactorAuthType.SMS => new SmsNotificationEvent
                {
                    To = destination,
                    Body = $"Your verification code is: {code}. This code will expire in 5 minutes.",
                    TemplateName = "TwoFactorSMS",
                    Data = new Dictionary<string, object>
                    {
                        ["code"] = code,
                        ["expiryMinutes"] = 5
                    }
                },
                TwoFactorAuthType.Email => new EmailNotificationEvent
                {
                    To = destination,
                    Subject = "Your Verification Code",
                    Body = GenerateTwoFactorEmailBody(code),
                    TemplateName = "TwoFactorEmail",
                    Data = new Dictionary<string, object>
                    {
                        ["code"] = code,
                        ["expiryMinutes"] = 5
                    },
                    IsHtml = true
                },
                _ => throw new System.ArgumentException($"Unsupported 2FA method: {method}")
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("2FA code sent via RabbitMQ to {Destination} using {Method}", destination, method);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA code via RabbitMQ to {Destination}. Falling back to HTTP.", destination);
            await SendTwoFactorCodeViaHttp(destination, code, method);
        }
    }

    private async Task SendTwoFactorBackupCodesViaRabbitMQ(string email, List<string> backupCodes)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "Your Two-Factor Authentication Backup Codes",
                TemplateName = "TwoFactorBackupCodes",
                Body = GenerateBackupCodesEmailBody(backupCodes),
                Data = new Dictionary<string, object>
                {
                    ["backupCodes"] = backupCodes,
                    ["generatedAt"] = System.DateTime.UtcNow
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("2FA backup codes sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA backup codes via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendTwoFactorBackupCodesViaHttp(email, backupCodes);
        }
    }

    private async Task SendNewAuthenticatorSetupViaRabbitMQ(string email, string secret, string qrCodeUri, List<string> recoveryCodes)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "🔐 New Authenticator Setup Required - Your Device Was Lost",
                TemplateName = "NewAuthenticatorSetup",
                Body = GenerateNewAuthenticatorSetupEmailBody(secret, qrCodeUri, recoveryCodes),
                Data = new Dictionary<string, object>
                {
                    ["secret"] = secret,
                    ["qrCodeUri"] = qrCodeUri,
                    ["recoveryCodes"] = recoveryCodes,
                    ["generatedAt"] = System.DateTime.UtcNow
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("New authenticator setup sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send new authenticator setup via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendNewAuthenticatorSetupViaHttp(email, secret, qrCodeUri, recoveryCodes);
        }
    }

    private async Task SendPasswordChangedConfirmationViaRabbitMQ(string email)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "🔐 Your Password Has Been Changed",
                TemplateName = "PasswordChanged",
                Body = GeneratePasswordChangedEmailBody(),
                Data = new Dictionary<string, object>
                {
                    ["changedAt"] = System.DateTime.UtcNow,
                    ["email"] = email
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("Password changed confirmation sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed confirmation via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendPasswordChangedConfirmationViaHttp(email);
        }
    }

    #endregion

    #region HTTP Fallback Methods

    private async Task SendPasswordResetViaHttp(string email, string resetToken, string resetUrl)
    {
        var subject = "Password Reset Request";
        var body = GeneratePasswordResetEmailBody(resetUrl);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("Password reset email sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send password reset email via HTTP to {Email}", email);
        }
    }

    private async Task SendWelcomeViaHttp(string email, string username)
    {
        var subject = "Welcome to Our Platform!";
        var body = GenerateWelcomeEmailBody(username);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("Welcome email sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send welcome email via HTTP to {Email}", email);
        }
    }

    private async Task SendEmailConfirmationViaHttp(string email, string confirmationToken, string confirmUrl)
    {
        var subject = "Confirm Your Email Address";
        var body = GenerateEmailConfirmationBody(confirmUrl);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("Email confirmation sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send email confirmation via HTTP to {Email}", email);
        }
    }

    private async Task SendTwoFactorCodeViaHttp(string destination, string code, TwoFactorAuthType method)
    {
        bool success = false;

        if (method == TwoFactorAuthType.SMS)
        {
            var message = $"Your verification code is: {code}. This code will expire in 5 minutes.";
            success = await _notificationServiceClient.SendSmsAsync(destination, message);
        }
        else if (method == TwoFactorAuthType.Email)
        {
            var subject = "Your Verification Code";
            var body = GenerateTwoFactorEmailBody(code);
            success = await _notificationServiceClient.SendEmailAsync(destination, subject, body, true);
        }

        if (success)
        {
            _logger.LogInformation("2FA code sent via HTTP to {Destination} using {Method}", destination, method);
        }
        else
        {
            _logger.LogError("Failed to send 2FA code via HTTP to {Destination} using {Method}", destination, method);
        }
    }

    private async Task SendTwoFactorBackupCodesViaHttp(string email, List<string> backupCodes)
    {
        var subject = "Your Two-Factor Authentication Backup Codes";
        var body = GenerateBackupCodesEmailBody(backupCodes);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("2FA backup codes sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send 2FA backup codes via HTTP to {Email}", email);
        }
    }

    private async Task SendNewAuthenticatorSetupViaHttp(string email, string secret, string qrCodeUri, List<string> recoveryCodes)
    {
        var subject = "🔐 New Authenticator Setup Required - Your Device Was Lost";
        var body = GenerateNewAuthenticatorSetupEmailBody(secret, qrCodeUri, recoveryCodes);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("New authenticator setup sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send new authenticator setup via HTTP to {Email}", email);
        }
    }

    private async Task SendPasswordChangedConfirmationViaHttp(string email)
    {
        var subject = "🔐 Your Password Has Been Changed";
        var body = GeneratePasswordChangedEmailBody();

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("Password changed confirmation sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send password changed confirmation via HTTP to {Email}", email);
        }
    }

    #endregion

    #region Email Body Generators

    private string GeneratePasswordResetEmailBody(string resetUrl)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Password Reset Request</h2>
                    <p>You have requested to reset your password.</p>
                    <p>Please click the button below to reset your password:</p>
                    <p><a href='{resetUrl}' class='button'>Reset Password</a></p>
                    <p>This link will expire in 1 hour.</p>
                    <p>If you did not request this reset, please ignore this email.</p>
                    <div class='footer'>
                        <p>If you're having trouble with the button, copy and paste this link into your browser:</p>
                        <p>{resetUrl}</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateWelcomeEmailBody(string username)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .welcome {{ color: #28a745; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2 class='welcome'>Welcome, {username}!</h2>
                    <p>Thank you for joining our platform. We're excited to have you on board!</p>
                    <p>Your account has been successfully created and you can now start using all our features.</p>
                    <p>If you have any questions, feel free to contact our support team.</p>
                    <p>Best regards,<br/>The Team</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateEmailConfirmationBody(string confirmUrl)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 4px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Email Confirmation</h2>
                    <p>Thank you for registering with us!</p>
                    <p>Please confirm your email address by clicking the button below:</p>
                    <p><a href='{confirmUrl}' class='button'>Confirm Email Address</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you did not create an account, please ignore this email.</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateTwoFactorEmailBody(string code)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .code {{ font-size: 24px; font-weight: bold; color: #007bff; letter-spacing: 4px; padding: 10px; background-color: #f8f9fa; border-radius: 4px; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Your Verification Code</h2>
                    <p>Your verification code is:</p>
                    <div class='code'>{code}</div>
                    <p>This code will expire in 5 minutes.</p>
                    <p>If you didn't request this code, please ignore this message.</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateBackupCodesEmailBody(List<string> backupCodes)
    {
        var codesHtml = string.Join("<br/>", backupCodes);

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .codes {{ font-family: monospace; background-color: #f8f9fa; padding: 15px; border-radius: 4px; }}
                    .warning {{ color: #856404; background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Backup Codes</h2>
                    <p>Here are your two-factor authentication backup codes:</p>
                    <div class='codes'>{codesHtml}</div>
                    <div class='warning'>
                        <p><strong>Important:</strong> Save these codes in a safe place. Each code can only be used once.</p>
                        <p>If you lose your authenticator app and don't have backup codes, you may lose access to your account.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateNewAuthenticatorSetupEmailBody(string secret, string qrCodeUri, List<string> recoveryCodes)
    {
        var codesHtml = string.Join("<br/>", recoveryCodes);
        // Generate QR code image URL using external service
        var qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(qrCodeUri)}";

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .alert {{ color: #721c24; background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 4px; margin-bottom: 20px; }}
                    .qr-section {{ text-align: center; background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .secret {{ font-family: monospace; font-size: 18px; letter-spacing: 2px; background-color: #e9ecef; padding: 10px; border-radius: 4px; word-break: break-all; }}
                    .codes {{ font-family: monospace; background-color: #f8f9fa; padding: 15px; border-radius: 4px; }}
                    .warning {{ color: #856404; background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin-top: 20px; }}
                    .steps {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 4px; }}
                    .steps ol {{ margin: 0; padding-left: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='alert'>
                        <h2>🔐 New Authenticator Setup Required</h2>
                        <p>You've used all your recovery codes. A NEW authenticator secret has been generated for your account.</p>
                    </div>
                    
                    <div class='steps'>
                        <h3>Setup Instructions:</h3>
                        <ol>
                            <li>Open your authenticator app (Google Authenticator, Authy, etc.)</li>
                            <li>Scan the QR code below OR enter the secret manually</li>
                            <li>Your app will start generating 6-digit codes</li>
                            <li>Use these codes for future logins</li>
                        </ol>
                    </div>
                    
                    <div class='qr-section'>
                        <h3>Scan this QR Code:</h3>
                        <img src='{qrImageUrl}' alt='QR Code for Authenticator' style='max-width: 200px;'/>
                        <p style='margin-top: 15px;'><strong>Or enter this secret manually:</strong></p>
                        <div class='secret'>{secret}</div>
                    </div>
                    
                    <h3>New Backup Codes:</h3>
                    <p>Save these codes in a safe place. Each code can only be used once:</p>
                    <div class='codes'>{codesHtml}</div>
                    
                    <div class='warning'>
                        <p><strong>⚠️ Security Notice:</strong></p>
                        <ul>
                            <li>If you didn't request this, your account may be compromised</li>
                            <li>Change your password immediately</li>
                            <li>Contact support if you suspect unauthorized access</li>
                        </ul>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GeneratePasswordChangedEmailBody()
    {
        var changedAt = System.DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm 'UTC'");

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .alert {{ color: #155724; background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 4px; margin-bottom: 20px; }}
                    .warning {{ color: #856404; background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 4px; margin-top: 20px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #666; border-top: 1px solid #eee; padding-top: 15px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='alert'>
                        <h2>🔐 Password Changed Successfully</h2>
                        <p>Your password was changed on <strong>{changedAt}</strong>.</p>
                    </div>
                    
                    <p>This email confirms that your password has been successfully updated.</p>
                    
                    <p><strong>What happened:</strong></p>
                    <ul>
                        <li>Your password was reset using the password reset link</li>
                        <li>All your previous sessions have been logged out for security</li>
                        <li>You'll need to log in again with your new password</li>
                    </ul>
                    
                    <div class='warning'>
                        <p><strong>⚠️ Didn't make this change?</strong></p>
                        <p>If you didn't request this password change, your account may be compromised.</p>
                        <p>Please contact our support team immediately at <a href='mailto:support@okla.com.do'>support@okla.com.do</a></p>
                    </div>
                    
                    <div class='footer'>
                        <p>This is an automated security notification from OKLA.</p>
                        <p>If you have any questions, please contact us at support@okla.com.do</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    #endregion

    #region US-18.2: Security Alert Methods

    /// <summary>
    /// US-18.2: Sends a security alert email when suspicious activity is detected.
    /// Triggers: 3+ failed login attempts, account lockout, etc.
    /// </summary>
    public async Task SendSecurityAlertAsync(string email, SecurityAlertDto alert)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendSecurityAlertViaRabbitMQ(email, alert);
        }
        else
        {
            await SendSecurityAlertViaHttp(email, alert);
        }
    }

    private async Task SendSecurityAlertViaRabbitMQ(string email, SecurityAlertDto alert)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = GetSecurityAlertSubject(alert.AlertType),
                TemplateName = "SecurityAlert",
                Body = GenerateSecurityAlertEmailBody(alert),
                Data = new Dictionary<string, object>
                {
                    ["alertType"] = alert.AlertType,
                    ["ipAddress"] = alert.IpAddress,
                    ["attemptCount"] = alert.AttemptCount,
                    ["timestamp"] = alert.Timestamp,
                    ["location"] = alert.Location ?? "Unknown",
                    ["deviceInfo"] = alert.DeviceInfo ?? "Unknown",
                    ["lockoutDuration"] = alert.LockoutDuration?.TotalMinutes ?? 0
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("Security alert ({AlertType}) sent via RabbitMQ to {Email}", alert.AlertType, email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send security alert via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendSecurityAlertViaHttp(email, alert);
        }
    }

    private async Task SendSecurityAlertViaHttp(string email, SecurityAlertDto alert)
    {
        var subject = GetSecurityAlertSubject(alert.AlertType);
        var body = GenerateSecurityAlertEmailBody(alert);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("Security alert ({AlertType}) sent via HTTP to {Email}", alert.AlertType, email);
        }
        else
        {
            _logger.LogError("Failed to send security alert via HTTP to {Email}", email);
        }
    }

    private static string GetSecurityAlertSubject(string alertType)
    {
        return alertType switch
        {
            "FailedLoginAttempts" => "⚠️ Alerta de Seguridad: Intentos de Inicio de Sesión Fallidos",
            "FailedRecoveryCodeAttempts" => "⚠️ Alerta de Seguridad: Intentos Fallidos con Código de Recuperación",
            "AccountLockout" => "🔒 Tu cuenta ha sido bloqueada temporalmente",
            "NewDeviceLogin" => "📱 Inicio de sesión desde un nuevo dispositivo",
            "SuspiciousActivity" => "⚠️ Actividad Sospechosa Detectada",
            _ => "⚠️ Alerta de Seguridad - OKLA"
        };
    }

    private string GenerateSecurityAlertEmailBody(SecurityAlertDto alert)
    {
        var changedAt = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var location = alert.Location ?? "Ubicación desconocida";
        var lockoutInfo = alert.LockoutDuration.HasValue
            ? $"<p><strong>Duración del bloqueo:</strong> {alert.LockoutDuration.Value.TotalMinutes} minutos</p>"
            : "";

        var alertTypeMessage = alert.AlertType switch
        {
            "FailedLoginAttempts" => $"Detectamos <strong>{alert.AttemptCount}</strong> intentos fallidos de inicio de sesión en tu cuenta.",
            "FailedRecoveryCodeAttempts" => $"Detectamos <strong>{alert.AttemptCount}</strong> intentos fallidos usando códigos de recuperación.",
            "AccountLockout" => "Tu cuenta ha sido bloqueada temporalmente debido a múltiples intentos fallidos de inicio de sesión.",
            "NewDeviceLogin" => "Alguien inició sesión en tu cuenta desde un dispositivo nuevo.",
            "SuspiciousActivity" => "Detectamos actividad sospechosa en tu cuenta.",
            _ => "Se detectó una actividad inusual en tu cuenta."
        };

        var changePasswordUrl = $"{_settings.FrontendBaseUrl}/recuperar-contrasena";
        var supportUrl = $"{_settings.FrontendBaseUrl}/contacto";

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .alert-banner {{ background: linear-gradient(135deg, #ff6b6b, #ee5a52); color: white; padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 20px; }}
                    .alert-banner h2 {{ margin: 0; font-size: 24px; }}
                    .info-box {{ background: #f8f9fa; border-left: 4px solid #dc3545; padding: 15px; margin: 15px 0; }}
                    .details {{ background: #fff3cd; border-radius: 8px; padding: 15px; margin: 15px 0; }}
                    .details p {{ margin: 8px 0; }}
                    .warning {{ background: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                    .actions {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                    .btn {{ display: inline-block; padding: 12px 24px; background: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 5px; }}
                    .btn-secondary {{ background: #6c757d; }}
                    .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='alert-banner'>
                        <h2>⚠️ Alerta de Seguridad - OKLA</h2>
                    </div>
                    
                    <div class='info-box'>
                        <p>{alertTypeMessage}</p>
                    </div>
                    
                    <div class='details'>
                        <h3>📋 Detalles del Evento:</h3>
                        <p><strong>📍 Ubicación:</strong> {location}</p>
                        <p><strong>🌐 Dirección IP:</strong> {alert.IpAddress}</p>
                        <p><strong>🕐 Fecha y Hora:</strong> {changedAt}</p>
                        <p><strong>📊 Intentos:</strong> {alert.AttemptCount}</p>
                        {lockoutInfo}
                    </div>
                    
                    <div class='warning'>
                        <p><strong>⚠️ ¿No fuiste tú?</strong></p>
                        <p>Si no reconoces esta actividad, te recomendamos:</p>
                        <ol>
                            <li>Cambiar tu contraseña inmediatamente</li>
                            <li>Revisar tus dispositivos conectados</li>
                            <li>Habilitar autenticación de dos factores (2FA)</li>
                            <li>Contactar a soporte si necesitas ayuda</li>
                        </ol>
                    </div>
                    
                    <div class='actions'>
                        <p><strong>🔐 Acciones Recomendadas:</strong></p>
                        <p style='text-align: center;'>
                            <a href='{changePasswordUrl}' class='btn'>Cambiar Contraseña</a>
                            <a href='{supportUrl}' class='btn btn-secondary'>Contactar Soporte</a>
                        </p>
                    </div>
                    
                    <div class='footer'>
                        <p>Esta es una notificación de seguridad automática de OKLA.</p>
                        <p>Si tienes preguntas, contáctanos en <a href='mailto:support@okla.com.do'>support@okla.com.do</a></p>
                        <p>© 2026 OKLA - Marketplace de Vehículos</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    #endregion

    #region New Session Notification Methods

    /// <summary>
    /// Sends a notification email when a new session is created from a new device/location.
    /// This is a security measure to alert users of new logins.
    /// </summary>
    public async Task SendNewSessionNotificationAsync(string email, NewSessionNotificationDto sessionInfo)
    {
        if (_rabbitMqSettings.EnableRabbitMQ && _notificationProducer != null)
        {
            await SendNewSessionNotificationViaRabbitMQ(email, sessionInfo);
        }
        else
        {
            await SendNewSessionNotificationViaHttp(email, sessionInfo);
        }
    }

    private async Task SendNewSessionNotificationViaRabbitMQ(string email, NewSessionNotificationDto sessionInfo)
    {
        try
        {
            var notification = new EmailNotificationEvent
            {
                To = email,
                Subject = "🔐 Nuevo inicio de sesión en tu cuenta OKLA",
                TemplateName = "NewSessionNotification",
                Body = GenerateNewSessionNotificationEmailBody(sessionInfo),
                Data = new Dictionary<string, object>
                {
                    ["deviceInfo"] = sessionInfo.DeviceInfo,
                    ["browser"] = sessionInfo.Browser,
                    ["operatingSystem"] = sessionInfo.OperatingSystem,
                    ["ipAddress"] = sessionInfo.IpAddress,
                    ["loginTime"] = sessionInfo.LoginTime,
                    ["location"] = sessionInfo.Location ?? "Unknown"
                },
                IsHtml = true
            };

            await _notificationProducer.PublishNotificationAsync(notification);
            _logger.LogInformation("New session notification sent via RabbitMQ to {Email}", email);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send new session notification via RabbitMQ to {Email}. Falling back to HTTP.", email);
            await SendNewSessionNotificationViaHttp(email, sessionInfo);
        }
    }

    private async Task SendNewSessionNotificationViaHttp(string email, NewSessionNotificationDto sessionInfo)
    {
        var subject = "🔐 Nuevo inicio de sesión en tu cuenta OKLA";
        var body = GenerateNewSessionNotificationEmailBody(sessionInfo);

        var success = await _notificationServiceClient.SendEmailAsync(email, subject, body, true);

        if (success)
        {
            _logger.LogInformation("New session notification sent via HTTP to {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to send new session notification via HTTP to {Email}", email);
        }
    }

    private string GenerateNewSessionNotificationEmailBody(NewSessionNotificationDto sessionInfo)
    {
        var loginTime = sessionInfo.LoginTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var location = sessionInfo.Location ?? "Ubicación desconocida";
        var securityUrl = $"{_settings.FrontendBaseUrl}/cuenta/seguridad";
        var changePasswordUrl = $"{_settings.FrontendBaseUrl}/recuperar-contrasena";

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #4CAF50, #45a049); color: white; padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 20px; }}
                    .header h2 {{ margin: 0; font-size: 24px; }}
                    .info-box {{ background: #e8f5e9; border-left: 4px solid #4CAF50; padding: 15px; margin: 15px 0; }}
                    .details {{ background: #f8f9fa; border-radius: 8px; padding: 15px; margin: 15px 0; }}
                    .details p {{ margin: 8px 0; }}
                    .warning {{ background: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                    .btn {{ display: inline-block; padding: 12px 24px; background: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 5px; }}
                    .btn-danger {{ background: #dc3545; }}
                    .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>🔐 Nuevo Inicio de Sesión</h2>
                    </div>
                    
                    <div class='info-box'>
                        <p>Hemos detectado un nuevo inicio de sesión en tu cuenta de OKLA.</p>
                    </div>
                    
                    <div class='details'>
                        <h3>📋 Detalles de la Sesión:</h3>
                        <p><strong>📱 Dispositivo:</strong> {sessionInfo.DeviceInfo}</p>
                        <p><strong>🌐 Navegador:</strong> {sessionInfo.Browser}</p>
                        <p><strong>💻 Sistema Operativo:</strong> {sessionInfo.OperatingSystem}</p>
                        <p><strong>📍 Ubicación:</strong> {location}</p>
                        <p><strong>🔢 Dirección IP:</strong> {sessionInfo.IpAddress}</p>
                        <p><strong>🕐 Fecha y Hora:</strong> {loginTime}</p>
                    </div>
                    
                    <div class='warning'>
                        <p><strong>⚠️ ¿No fuiste tú?</strong></p>
                        <p>Si no reconoces este inicio de sesión, te recomendamos:</p>
                        <ol>
                            <li>Cambiar tu contraseña inmediatamente</li>
                            <li>Revisar y cerrar las sesiones desconocidas</li>
                            <li>Habilitar autenticación de dos factores (2FA)</li>
                        </ol>
                    </div>
                    
                    <p style='text-align: center;'>
                        <a href='{securityUrl}' class='btn'>Ver Mis Sesiones</a>
                        <a href='{changePasswordUrl}' class='btn btn-danger'>Cambiar Contraseña</a>
                    </p>
                    
                    <div class='footer'>
                        <p>Esta es una notificación de seguridad automática de OKLA.</p>
                        <p>Si tienes preguntas, contáctanos en <a href='mailto:support@okla.com.do'>support@okla.com.do</a></p>
                        <p>© 2026 OKLA - Marketplace de Vehículos</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    #endregion
}
