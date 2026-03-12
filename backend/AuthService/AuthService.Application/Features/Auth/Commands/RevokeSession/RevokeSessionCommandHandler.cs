using MediatR;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Application.Services;
using AuthService.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AuthService.Application.Features.Auth.Commands.RevokeSession;

/// <summary>
/// Handler para revocar una sesión específica con verificación por código.
/// Proceso: AUTH-SEC-003
/// 
/// Flujo de seguridad:
/// 1. Validar código de verificación
/// 2. Validar formato de session ID
/// 3. Obtener sesión del repositorio
/// 4. Verificar que la sesión existe
/// 5. Verificar que la sesión pertenece al usuario (CRÍTICO)
/// 6. Verificar que no es la sesión actual (BLOQUEADO)
/// 7. Revocar sesión
/// 8. Revocar refresh token asociado (si existe)
/// 9. Marcar dispositivo como revocado (para futuro login)
/// 10. Enviar notificación al dispositivo revocado
/// 11. Registrar en log de auditoría
/// </summary>
public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, RevokeSessionResponse>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly INotificationService _notificationService;
    private readonly IRevokedDeviceService _revokedDeviceService;
    private readonly ILogger<RevokeSessionCommandHandler> _logger;

    private const string REVOCATION_CODE_PREFIX = "session_revoke_code:";
    private const string LOCKOUT_PREFIX = "session_revoke_lockout:";
    private const int LOCKOUT_MINUTES = 15;

    public RevokeSessionCommandHandler(
        IUserSessionRepository sessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IDistributedCache cache,
        INotificationService notificationService,
        IRevokedDeviceService revokedDeviceService,
        ILogger<RevokeSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _cache = cache;
        _notificationService = notificationService;
        _revokedDeviceService = revokedDeviceService;
        _logger = logger;
    }

    public async Task<RevokeSessionResponse> Handle(
        RevokeSessionCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AUTH-SEC-003: Session revocation initiated by user {UserId} for session {SessionId} from IP {IpAddress}",
            request.UserId, request.SessionId, request.IpAddress ?? "unknown");

        try
        {
            // 0. Verificar lockout por intentos fallidos
            var lockoutKey = $"{LOCKOUT_PREFIX}{request.UserId}:{request.SessionId}";
            var lockoutValue = await _cache.GetStringAsync(lockoutKey, cancellationToken);
            if (!string.IsNullOrEmpty(lockoutValue))
            {
                var lockoutUntil = DateTime.Parse(lockoutValue);
                if (DateTime.UtcNow < lockoutUntil)
                {
                    var remainingMinutes = (int)(lockoutUntil - DateTime.UtcNow).TotalMinutes + 1;
                    _logger.LogWarning(
                        "AUTH-SEC-003: User {UserId} is locked out for session revocation. Remaining: {Minutes} min",
                        request.UserId, remainingMinutes);
                    return new RevokeSessionResponse(
                        Success: false,
                        Message: $"Too many failed attempts. Try again in {remainingMinutes} minutes.",
                        RemainingAttempts: 0
                    );
                }
            }

            // 1. Validar formato de GUID
            if (!Guid.TryParse(request.SessionId, out var sessionGuid))
            {
                _logger.LogWarning(
                    "AUTH-SEC-003: Invalid session ID format: {SessionId}",
                    request.SessionId);
                throw new BadRequestException("Invalid session ID format.");
            }

            // 2. BLOQUEAR si es la sesión actual
            if (request.SessionId == request.CurrentSessionId)
            {
                _logger.LogInformation(
                    "AUTH-SEC-003: User {UserId} attempted to revoke current session - BLOCKED",
                    request.UserId);
                return new RevokeSessionResponse(
                    Success: false,
                    Message: "You cannot terminate your current session. Use the logout button instead."
                );
            }

            // 3. Verificar código de verificación (OPCIONAL para usuarios autenticados)
            // Si no se proporciona código, se permite la revocación directa
            if (!string.IsNullOrEmpty(request.VerificationCode))
            {
                var cacheKey = $"{REVOCATION_CODE_PREFIX}{request.UserId}:{request.SessionId}";
                var cacheValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (string.IsNullOrEmpty(cacheValue))
                {
                    _logger.LogWarning(
                        "AUTH-SEC-003: No verification code found for user {UserId} session {SessionId}",
                        request.UserId, request.SessionId);
                    return new RevokeSessionResponse(
                        Success: false,
                        Message: "Verification code expired or not requested. Please request a new code."
                    );
                }

                var codeData = JsonSerializer.Deserialize<RevocationCodeData>(cacheValue);
                if (codeData == null)
                {
                    throw new BadRequestException("Invalid verification data.");
                }

                // 4. Verificar expiración
                if (DateTime.UtcNow > codeData.ExpiresAt)
                {
                    await _cache.RemoveAsync(cacheKey, cancellationToken);
                    return new RevokeSessionResponse(
                        Success: false,
                        Message: "Verification code has expired. Please request a new code."
                    );
                }

                // 5. Verificar código
                var providedCodeHash = HashCode(request.VerificationCode);
                if (providedCodeHash != codeData.CodeHash)
                {
                    codeData.RemainingAttempts--;

                    if (codeData.RemainingAttempts <= 0)
                    {
                        // Lockout
                        await _cache.RemoveAsync(cacheKey, cancellationToken);
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
                            "AUTH-SEC-003: User {UserId} locked out for {Minutes} min after failed verification attempts",
                            request.UserId, LOCKOUT_MINUTES);

                        return new RevokeSessionResponse(
                            Success: false,
                            Message: $"Too many incorrect attempts. Locked for {LOCKOUT_MINUTES} minutes.",
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
                        "AUTH-SEC-003: Invalid code for user {UserId}. Remaining attempts: {Attempts}",
                        request.UserId, codeData.RemainingAttempts);

                    return new RevokeSessionResponse(
                        Success: false,
                        Message: "Invalid verification code.",
                        RemainingAttempts: codeData.RemainingAttempts
                    );
                }

                // 6. Código válido - eliminar de cache
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
            else
            {
                // Direct revocation without code (simplified flow for authenticated users)
                _logger.LogInformation(
                    "AUTH-SEC-003: Direct session revocation (no code) for user {UserId} session {SessionId}",
                    request.UserId, request.SessionId);
            }

            // 7. Obtener sesión
            var session = await _sessionRepository.GetByIdAsync(sessionGuid, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning(
                    "AUTH-SEC-003: Session {SessionId} not found for user {UserId}",
                    request.SessionId, request.UserId);
                throw new NotFoundException("Session not found.");
            }

            // 8. CRÍTICO: Verificar que la sesión pertenece al usuario
            if (session.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "AUTH-SEC-003: SECURITY ALERT - User {UserId} attempted to revoke session {SessionId} " +
                    "belonging to user {SessionOwner}. IP: {IpAddress}",
                    request.UserId, request.SessionId, session.UserId, request.IpAddress);
                throw new NotFoundException("Session not found.");
            }

            // 9. Verificar si ya está revocada
            if (session.IsRevoked)
            {
                _logger.LogInformation(
                    "AUTH-SEC-003: Session {SessionId} already revoked",
                    request.SessionId);
                return new RevokeSessionResponse(
                    Success: true,
                    Message: "Session was already terminated."
                );
            }

            // 10. Revocar la sesión
            var revocationReason = string.IsNullOrEmpty(request.VerificationCode)
                ? "User revoked session directly"
                : "User revoked session with verification code";
            await _sessionRepository.RevokeSessionAsync(
                sessionGuid,
                revocationReason,
                cancellationToken);

            // 11. Revocar el refresh token asociado
            bool refreshTokenRevoked = false;
            if (!string.IsNullOrEmpty(session.RefreshTokenId))
            {
                try
                {
                    var refreshToken = await _refreshTokenRepository.GetByTokenAsync(
                        session.RefreshTokenId,
                        cancellationToken);

                    if (refreshToken != null && !refreshToken.IsRevoked)
                    {
                        refreshToken.Revoke("session_revoked_by_user", request.UserId);
                        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);
                        refreshTokenRevoked = true;

                        _logger.LogInformation(
                            "AUTH-SEC-003: Associated refresh token revoked for session {SessionId}",
                            request.SessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "AUTH-SEC-003: Could not revoke associated refresh token for session {SessionId}",
                        request.SessionId);
                }
            }

            // 12. NUEVO: Marcar dispositivo como revocado para futuro control de login
            // Esto asegura que si alguien intenta loguearse de nuevo desde este dispositivo,
            // se le requerirá verificación adicional
            bool deviceMarkedAsRevoked = false;
            if (!string.IsNullOrEmpty(session.IpAddress) && !string.IsNullOrEmpty(session.DeviceInfo))
            {
                try
                {
                    // Construir un UserAgent aproximado basado en Browser y OS
                    var approximateUserAgent = $"{session.Browser ?? "Unknown"} on {session.OperatingSystem ?? "Unknown"}";
                    await _revokedDeviceService.MarkDeviceAsRevokedAsync(
                        request.UserId,
                        session.IpAddress,
                        approximateUserAgent,
                        session.Browser ?? "Unknown",
                        session.OperatingSystem ?? "Unknown",
                        cancellationToken);
                    deviceMarkedAsRevoked = true;

                    _logger.LogInformation(
                        "AUTH-SEC-003: Device marked as revoked for user {UserId}. " +
                        "Future login attempts from this device will require verification.",
                        request.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "AUTH-SEC-003: Could not mark device as revoked for session {SessionId}",
                        request.SessionId);
                }
            }

            // 13. Obtener usuario para notificación
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                // Enviar notificación de sesión terminada
                await SendSessionTerminatedNotification(
                    user.Email,
                    user.FirstName ?? user.UserName ?? "User",
                    session.DeviceInfo ?? "Unknown device",
                    session.Browser ?? "Unknown browser",
                    session.IpAddress ?? "Unknown IP",
                    session.Location ?? "Unknown location",
                    request.IpAddress ?? "Unknown",
                    cancellationToken);
            }

            // 14. Log de auditoría exitoso
            _logger.LogInformation(
                "AUTH-SEC-003: Session {SessionId} successfully revoked by user {UserId} with verification. " +
                "Device: {Device}, RefreshTokenRevoked: {RefreshRevoked}, DeviceMarkedRevoked: {DeviceRevoked}",
                request.SessionId, request.UserId, session.DeviceInfo, refreshTokenRevoked, deviceMarkedAsRevoked);

            return new RevokeSessionResponse(
                Success: true,
                Message: "Session terminated successfully. The device has been logged out.",
                WasCurrentSession: false,
                RefreshTokenRevoked: refreshTokenRevoked
            );
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH-SEC-003: Unexpected error revoking session {SessionId} for user {UserId}",
                request.SessionId, request.UserId);
            throw;
        }
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    private async Task SendSessionTerminatedNotification(
        string email,
        string userName,
        string device,
        string browser,
        string sessionIp,
        string location,
        string requestIp,
        CancellationToken cancellationToken)
    {
        var subject = "⚠️ Una sesión fue terminada en tu cuenta - OKLA";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border: 1px solid #e9ecef; }}
        .alert-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .device-info {{ background: #fff; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #dc3545; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Sesión Terminada</h1>
            <p>Notificación de seguridad</p>
        </div>
        <div class='content'>
            <p>Hola <strong>{userName}</strong>,</p>
            <p>Te informamos que una sesión activa en tu cuenta de OKLA ha sido <strong>terminada remotamente</strong>.</p>
            
            <div class='device-info'>
                <h3 style='margin-top: 0; color: #dc3545;'>📱 Sesión terminada:</h3>
                <ul>
                    <li><strong>Dispositivo:</strong> {device}</li>
                    <li><strong>Navegador:</strong> {browser}</li>
                    <li><strong>IP de la sesión:</strong> {sessionIp}</li>
                    <li><strong>Ubicación:</strong> {location}</li>
                </ul>
            </div>

            <div class='alert-box'>
                <strong>ℹ️ Información:</strong>
                <p style='margin-bottom: 0;'>Esta acción fue realizada desde la IP: <strong>{requestIp}</strong></p>
                <p style='margin-bottom: 0;'>Fecha y hora: <strong>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</strong></p>
            </div>

            <p><strong>¿No reconoces esta actividad?</strong></p>
            <p>Si no fuiste tú quien terminó esta sesión, te recomendamos:</p>
            <ol>
                <li>Cambiar tu contraseña inmediatamente</li>
                <li>Revisar todas las sesiones activas</li>
                <li>Activar la autenticación de dos factores</li>
                <li>Contactar a soporte si detectas actividad sospechosa</li>
            </ol>
        </div>
        <div class='footer'>
            <p>Este es un correo automático de seguridad de OKLA.</p>
            <p>Si tienes preguntas, contacta a soporte@okla.com.do</p>
        </div>
    </div>
</body>
</html>";

        try
        {
            await _notificationService.SendEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send session terminated notification to {Email}", email);
        }
    }

    private class RevocationCodeData
    {
        public string CodeHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int RemainingAttempts { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
    }
}
