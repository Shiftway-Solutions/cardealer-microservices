using MediatR;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthService.Domain.Interfaces.Services;

namespace AuthService.Application.Features.Auth.Commands.RequestSessionRevocation;

/// <summary>
/// Handler para solicitar revocación de sesión.
/// Proceso: AUTH-SEC-003-A
/// 
/// Flujo de seguridad:
/// 1. Validar que la sesión existe
/// 2. Validar que pertenece al usuario
/// 3. Validar que NO es la sesión actual
/// 4. Rate limiting (máx 3 solicitudes por hora)
/// 5. Generar código de 6 dígitos
/// 6. Enviar email con código
/// 7. Almacenar código hasheado en Redis
/// </summary>
public class RequestSessionRevocationCommandHandler
    : IRequestHandler<RequestSessionRevocationCommand, RequestSessionRevocationResponse>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RequestSessionRevocationCommandHandler> _logger;

    private const int CODE_EXPIRATION_MINUTES = 5;
    private const int MAX_REQUESTS_PER_HOUR = 3;
    private const int MAX_VERIFICATION_ATTEMPTS = 3;
    private const string REVOCATION_CODE_PREFIX = "session_revoke_code:";
    private const string RATE_LIMIT_PREFIX = "session_revoke_rate:";

    public RequestSessionRevocationCommandHandler(
        IUserSessionRepository sessionRepository,
        IUserRepository userRepository,
        IDistributedCache cache,
        INotificationService notificationService,
        ILogger<RequestSessionRevocationCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _cache = cache;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<RequestSessionRevocationResponse> Handle(
        RequestSessionRevocationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AUTH-SEC-003-A: Session revocation request initiated by user {UserId} for session {SessionId}",
            request.UserId, request.SessionId);

        try
        {
            // 1. Validar formato de GUID
            if (!Guid.TryParse(request.SessionId, out var sessionGuid))
            {
                throw new BadRequestException("Invalid session ID format.");
            }

            // 2. Verificar rate limiting
            var rateLimitKey = $"{RATE_LIMIT_PREFIX}{request.UserId}:{DateTime.UtcNow:yyyyMMddHH}";
            var rateLimitValue = await _cache.GetStringAsync(rateLimitKey, cancellationToken);
            var requestCount = string.IsNullOrEmpty(rateLimitValue) ? 0 : int.Parse(rateLimitValue);

            if (requestCount >= MAX_REQUESTS_PER_HOUR)
            {
                _logger.LogWarning(
                    "AUTH-SEC-003-A: Rate limit exceeded for user {UserId}. Count: {Count}",
                    request.UserId, requestCount);
                throw new BadRequestException("Too many requests. Please try again in an hour.");
            }

            // 3. Obtener la sesión
            var session = await _sessionRepository.GetByIdAsync(sessionGuid, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning(
                    "AUTH-SEC-003-A: Session {SessionId} not found",
                    request.SessionId);
                throw new NotFoundException("Session not found.");
            }

            // 4. CRÍTICO: Verificar propiedad
            if (session.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "AUTH-SEC-003-A: SECURITY ALERT - User {UserId} attempted to request revocation " +
                    "for session {SessionId} belonging to user {SessionOwner}",
                    request.UserId, request.SessionId, session.UserId);
                throw new NotFoundException("Session not found.");
            }

            // 5. BLOQUEAR si es la sesión actual
            if (request.SessionId == request.CurrentSessionId)
            {
                _logger.LogInformation(
                    "AUTH-SEC-003-A: User {UserId} attempted to revoke current session - blocked",
                    request.UserId);
                return new RequestSessionRevocationResponse(
                    Success: false,
                    Message: "You cannot terminate your current session. Use logout instead.",
                    SessionId: request.SessionId
                );
            }

            // 6. Verificar si ya está revocada
            if (session.IsRevoked)
            {
                return new RequestSessionRevocationResponse(
                    Success: false,
                    Message: "This session has already been terminated.",
                    SessionId: request.SessionId
                );
            }

            // 7. Obtener email del usuario
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                throw new BadRequestException("User email not found.");
            }

            // 8. Generar código de 6 dígitos
            var code = GenerateSecureCode();
            var codeExpiration = DateTime.UtcNow.AddMinutes(CODE_EXPIRATION_MINUTES);

            // 9. Almacenar código hasheado en Redis
            var cacheKey = $"{REVOCATION_CODE_PREFIX}{request.UserId}:{request.SessionId}";
            var cacheData = new RevocationCodeData
            {
                CodeHash = HashCode(code),
                ExpiresAt = codeExpiration,
                RemainingAttempts = MAX_VERIFICATION_ATTEMPTS,
                SessionId = request.SessionId,
                DeviceInfo = session.DeviceInfo ?? "Unknown device",
                Browser = session.Browser ?? "Unknown browser"
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(cacheData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = codeExpiration.AddMinutes(1) // Extra minute for cleanup
                },
                cancellationToken);

            // 10. Incrementar rate limit
            await _cache.SetStringAsync(
                rateLimitKey,
                (requestCount + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
                },
                cancellationToken);

            // 11. Enviar email con código
            await SendRevocationCodeEmail(
                user.Email,
                user.FirstName ?? user.UserName ?? "User",
                code,
                session.DeviceInfo ?? "Unknown device",
                session.Browser ?? "Unknown browser",
                session.IpAddress ?? "Unknown IP",
                session.Location ?? "Unknown location",
                CODE_EXPIRATION_MINUTES,
                cancellationToken);

            _logger.LogInformation(
                "AUTH-SEC-003-A: Revocation code sent to {Email} for session {SessionId}. " +
                "Device: {Device}, Expires: {ExpiresAt}",
                MaskEmail(user.Email), request.SessionId, session.DeviceInfo, codeExpiration);

            return new RequestSessionRevocationResponse(
                Success: true,
                Message: $"A verification code has been sent to your email. Enter the code within {CODE_EXPIRATION_MINUTES} minutes to terminate this session.",
                SessionId: request.SessionId,
                CodeExpiresAt: codeExpiration,
                RemainingAttempts: MAX_VERIFICATION_ATTEMPTS
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
                "AUTH-SEC-003-A: Unexpected error requesting session revocation for user {UserId}",
                request.UserId);
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

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***@***";
        var name = parts[0];
        var domain = parts[1];
        var maskedName = name.Length > 2
            ? name[0] + new string('*', name.Length - 2) + name[^1]
            : "**";
        return $"{maskedName}@{domain}";
    }

    private async Task SendRevocationCodeEmail(
        string email,
        string userName,
        string code,
        string device,
        string browser,
        string ipAddress,
        string location,
        int expirationMinutes,
        CancellationToken cancellationToken)
    {
        var subject = "🔐 Código de verificación para terminar sesión - OKLA";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border: 1px solid #e9ecef; }}
        .code-box {{ background: #fff; border: 2px dashed #667eea; padding: 20px; text-align: center; margin: 20px 0; border-radius: 10px; }}
        .code {{ font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: 8px; font-family: monospace; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .device-info {{ background: #fff; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Verificación de Seguridad</h1>
            <p>Código para terminar sesión</p>
        </div>
        <div class='content'>
            <p>Hola <strong>{userName}</strong>,</p>
            <p>Has solicitado terminar una sesión activa en tu cuenta. Para confirmar esta acción, ingresa el siguiente código:</p>
            
            <div class='code-box'>
                <div class='code'>{code}</div>
                <p style='margin-top: 10px; color: #666;'>Este código expira en <strong>{expirationMinutes} minutos</strong></p>
            </div>

            <div class='device-info'>
                <h3 style='margin-top: 0;'>📱 Dispositivo a desconectar:</h3>
                <ul>
                    <li><strong>Dispositivo:</strong> {device}</li>
                    <li><strong>Navegador:</strong> {browser}</li>
                    <li><strong>IP:</strong> {ipAddress}</li>
                    <li><strong>Ubicación:</strong> {location}</li>
                </ul>
            </div>

            <div class='warning'>
                <strong>⚠️ Importante:</strong>
                <ul style='margin-bottom: 0;'>
                    <li>Si NO solicitaste esto, ignora este correo y revisa la seguridad de tu cuenta.</li>
                    <li>Nunca compartas este código con nadie.</li>
                    <li>El equipo de OKLA nunca te pedirá este código.</li>
                </ul>
            </div>
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
