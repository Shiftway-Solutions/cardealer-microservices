using MediatR;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AuthService.Application.Features.Auth.Commands.VerifyRevokedDeviceLogin;

/// <summary>
/// Handler para gestionar login desde dispositivos revocados.
/// Proceso: AUTH-SEC-005
/// 
/// Cuando un dispositivo previamente revocado intenta iniciar sesión:
/// 1. Detecta que el fingerprint del dispositivo está en la lista de revocados
/// 2. Genera código de 6 dígitos
/// 3. Envía email de alerta: "Alguien está intentando acceder desde un dispositivo revocado"
/// 4. Requiere código para continuar
/// 5. Si el código es válido, permite el login y limpia el dispositivo de la lista
/// </summary>
public class VerifyRevokedDeviceLoginCommandHandler :
    IRequestHandler<RequestRevokedDeviceLoginCommand, RequestRevokedDeviceLoginResponse>,
    IRequestHandler<VerifyRevokedDeviceLoginCommand, VerifyRevokedDeviceLoginResponse>
{
    private readonly IDistributedCache _cache;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VerifyRevokedDeviceLoginCommandHandler> _logger;

    private const int CODE_EXPIRATION_MINUTES = 10;
    private const int MAX_VERIFICATION_ATTEMPTS = 3;
    private const int LOCKOUT_MINUTES = 30;
    private const string REVOKED_DEVICE_LOGIN_PREFIX = "revoked_device_login:";
    private const string REVOKED_DEVICE_LOCKOUT_PREFIX = "revoked_device_lockout:";

    public VerifyRevokedDeviceLoginCommandHandler(
        IDistributedCache cache,
        INotificationService notificationService,
        ILogger<VerifyRevokedDeviceLoginCommandHandler> logger)
    {
        _cache = cache;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Solicitar código de verificación para login desde dispositivo revocado.
    /// </summary>
    public async Task<RequestRevokedDeviceLoginResponse> Handle(
        RequestRevokedDeviceLoginCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "AUTH-SEC-005: Login attempt from revoked device detected. User: {UserId}, IP: {IpAddress}, Device: {Device}",
            request.UserId, request.IpAddress, request.DeviceFingerprint);

        try
        {
            // Verificar lockout
            var lockoutKey = $"{REVOKED_DEVICE_LOCKOUT_PREFIX}{request.DeviceFingerprint}";
            var lockoutValue = await _cache.GetStringAsync(lockoutKey, cancellationToken);
            if (!string.IsNullOrEmpty(lockoutValue))
            {
                var lockoutUntil = DateTime.Parse(lockoutValue);
                if (DateTime.UtcNow < lockoutUntil)
                {
                    var remainingMinutes = (int)(lockoutUntil - DateTime.UtcNow).TotalMinutes + 1;
                    _logger.LogWarning(
                        "AUTH-SEC-005: Device {Device} is locked out. Remaining: {Minutes} min",
                        request.DeviceFingerprint, remainingMinutes);
                    return new RequestRevokedDeviceLoginResponse(
                        RequiresVerification: true,
                        Message: $"Demasiados intentos fallidos. Intenta de nuevo en {remainingMinutes} minutos."
                    );
                }
            }

            // Generar código y token
            var code = GenerateSecureCode();
            var verificationToken = GenerateVerificationToken();
            var codeExpiration = DateTime.UtcNow.AddMinutes(CODE_EXPIRATION_MINUTES);

            // Almacenar en cache
            var cacheKey = $"{REVOKED_DEVICE_LOGIN_PREFIX}{verificationToken}";
            var cacheData = new RevokedDeviceLoginData
            {
                UserId = request.UserId,
                Email = request.Email,
                CodeHash = HashCode(code),
                DeviceFingerprint = request.DeviceFingerprint,
                IpAddress = request.IpAddress ?? "Unknown",
                UserAgent = request.UserAgent ?? "Unknown",
                Browser = request.Browser ?? "Unknown",
                OperatingSystem = request.OperatingSystem ?? "Unknown",
                ExpiresAt = codeExpiration,
                RemainingAttempts = MAX_VERIFICATION_ATTEMPTS
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(cacheData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = codeExpiration.AddMinutes(1)
                },
                cancellationToken);

            // Enviar email de alerta con código
            await SendRevokedDeviceLoginAlertEmail(
                request.Email,
                code,
                request.Browser ?? "Unknown",
                request.OperatingSystem ?? "Unknown",
                request.IpAddress ?? "Unknown",
                CODE_EXPIRATION_MINUTES,
                cancellationToken);

            _logger.LogInformation(
                "AUTH-SEC-005: Verification code sent for revoked device login. User: {UserId}, Token: {Token}",
                request.UserId, verificationToken[..8] + "...");

            return new RequestRevokedDeviceLoginResponse(
                RequiresVerification: true,
                Message: "Se ha detectado un intento de inicio de sesión desde un dispositivo previamente desconectado. " +
                         "Por seguridad, hemos enviado un código de verificación a tu correo electrónico.",
                VerificationToken: verificationToken,
                CodeExpiresAt: codeExpiration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH-SEC-005: Error processing revoked device login request for user {UserId}",
                request.UserId);
            throw;
        }
    }

    /// <summary>
    /// Verificar código y permitir login desde dispositivo revocado.
    /// </summary>
    public async Task<VerifyRevokedDeviceLoginResponse> Handle(
        VerifyRevokedDeviceLoginCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AUTH-SEC-005: Verifying revoked device login code. Token: {Token}",
            request.VerificationToken[..Math.Min(8, request.VerificationToken.Length)] + "...");

        try
        {
            var cacheKey = $"{REVOKED_DEVICE_LOGIN_PREFIX}{request.VerificationToken}";
            var cacheValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cacheValue))
            {
                return new VerifyRevokedDeviceLoginResponse(
                    Success: false,
                    Message: "El código ha expirado o no es válido. Por favor, intenta iniciar sesión nuevamente."
                );
            }

            var codeData = JsonSerializer.Deserialize<RevokedDeviceLoginData>(cacheValue);
            if (codeData == null)
            {
                return new VerifyRevokedDeviceLoginResponse(
                    Success: false,
                    Message: "Error al procesar la verificación."
                );
            }

            // Verificar expiración
            if (DateTime.UtcNow > codeData.ExpiresAt)
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                return new VerifyRevokedDeviceLoginResponse(
                    Success: false,
                    Message: "El código ha expirado. Por favor, intenta iniciar sesión nuevamente."
                );
            }

            // Verificar código
            var providedCodeHash = HashCode(request.Code);
            if (providedCodeHash != codeData.CodeHash)
            {
                codeData.RemainingAttempts--;

                if (codeData.RemainingAttempts <= 0)
                {
                    // Lockout del dispositivo
                    await _cache.RemoveAsync(cacheKey, cancellationToken);
                    var lockoutKey = $"{REVOKED_DEVICE_LOCKOUT_PREFIX}{codeData.DeviceFingerprint}";
                    var lockoutUntil = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES);
                    await _cache.SetStringAsync(
                        lockoutKey,
                        lockoutUntil.ToString("O"),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = lockoutUntil
                        },
                        cancellationToken);

                    _logger.LogWarning(
                        "AUTH-SEC-005: Device {Device} locked out after failed verification attempts",
                        codeData.DeviceFingerprint);

                    return new VerifyRevokedDeviceLoginResponse(
                        Success: false,
                        Message: $"Demasiados intentos incorrectos. Dispositivo bloqueado por {LOCKOUT_MINUTES} minutos.",
                        RemainingAttempts: 0
                    );
                }

                // Actualizar intentos restantes
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(codeData),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = codeData.ExpiresAt.AddMinutes(1)
                    },
                    cancellationToken);

                _logger.LogWarning(
                    "AUTH-SEC-005: Invalid code for revoked device login. Remaining attempts: {Attempts}",
                    codeData.RemainingAttempts);

                return new VerifyRevokedDeviceLoginResponse(
                    Success: false,
                    Message: "Código incorrecto.",
                    RemainingAttempts: codeData.RemainingAttempts
                );
            }

            // Código válido - limpiar cache y permitir login
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            // Limpiar el dispositivo de la lista de revocados
            var revokedDeviceKey = $"revoked_device:{codeData.UserId}:{codeData.DeviceFingerprint}";
            await _cache.RemoveAsync(revokedDeviceKey, cancellationToken);

            _logger.LogInformation(
                "AUTH-SEC-005: Revoked device login verified successfully. User: {UserId}, Device cleared.",
                codeData.UserId);

            return new VerifyRevokedDeviceLoginResponse(
                Success: true,
                Message: "Verificación exitosa. Puedes continuar con el inicio de sesión.",
                DeviceCleared: true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH-SEC-005: Error verifying revoked device login code");
            throw;
        }
    }

    private static string GenerateSecureCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = (BitConverter.ToUInt32(bytes, 0) % 900000) + 100000;
        return code.ToString();
    }

    private static string GenerateVerificationToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    private async Task SendRevokedDeviceLoginAlertEmail(
        string email,
        string code,
        string browser,
        string operatingSystem,
        string ipAddress,
        int expirationMinutes,
        CancellationToken cancellationToken)
    {
        var subject = "🚨 Alerta de Seguridad: Intento de acceso desde dispositivo desconectado - OKLA";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border: 1px solid #e9ecef; }}
        .alert-box {{ background: #ffe6e6; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; }}
        .code-box {{ background: #fff; border: 3px solid #dc3545; padding: 20px; text-align: center; margin: 20px 0; border-radius: 10px; }}
        .code {{ font-size: 36px; font-weight: bold; color: #dc3545; letter-spacing: 10px; font-family: monospace; }}
        .device-info {{ background: #fff; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #ffc107; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚨 Alerta de Seguridad</h1>
            <p>Intento de acceso desde dispositivo desconectado</p>
        </div>
        <div class='content'>
            <div class='alert-box'>
                <strong>⚠️ Atención:</strong> Alguien está intentando iniciar sesión en tu cuenta desde un dispositivo que fue <strong>previamente desconectado</strong> por razones de seguridad.
            </div>

            <p>Si <strong>eres tú</strong> quien está intentando acceder, ingresa el siguiente código para continuar:</p>
            
            <div class='code-box'>
                <div class='code'>{code}</div>
                <p style='margin-top: 10px; color: #666;'>Este código expira en <strong>{expirationMinutes} minutos</strong></p>
            </div>

            <div class='device-info'>
                <h3 style='margin-top: 0; color: #856404;'>📱 Dispositivo detectado:</h3>
                <ul>
                    <li><strong>Navegador:</strong> {browser}</li>
                    <li><strong>Sistema Operativo:</strong> {operatingSystem}</li>
                    <li><strong>Dirección IP:</strong> {ipAddress}</li>
                    <li><strong>Fecha y hora:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>
            </div>

            <div class='alert-box' style='background: #fff3cd; border-color: #ffc107;'>
                <strong>🔒 Si NO eres tú:</strong>
                <ul style='margin-bottom: 0;'>
                    <li><strong>NO compartas este código con nadie</strong></li>
                    <li>Ignora este correo - el atacante NO podrá acceder sin el código</li>
                    <li>Cambia tu contraseña inmediatamente</li>
                    <li>Activa la autenticación de dos factores</li>
                    <li>Contacta a soporte si sospechas actividad maliciosa</li>
                </ul>
            </div>

            <p style='color: #666; font-size: 14px;'>
                <strong>¿Por qué recibí este correo?</strong><br>
                Este dispositivo fue desconectado previamente desde la sección de seguridad de tu cuenta. 
                Como medida de protección, requerimos verificación adicional antes de permitir el acceso nuevamente.
            </p>
        </div>
        <div class='footer'>
            <p>Este es un correo automático de seguridad de OKLA.</p>
            <p>Si tienes preguntas, contacta a soporte@okla.com.do</p>
        </div>
    </div>
</body>
</html>";

        await _notificationService.SendEmailAsync(email, subject, htmlBody);
    }

    private class RevokedDeviceLoginData
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;
        public string DeviceFingerprint { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int RemainingAttempts { get; set; }
    }
}
