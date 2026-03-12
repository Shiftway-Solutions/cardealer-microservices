using MediatR;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Application.DTOs.Security;
using AuthService.Shared.Exceptions;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Handler para el comando ChangePassword.
/// Proceso: AUTH-SEC-001
/// 
/// Flujo:
/// 1. Validar usuario existe
/// 2. Verificar cuenta no bloqueada
/// 3. Verificar contraseña actual
/// 4. Validar nueva contraseña diferente a actual
/// 5. Actualizar contraseña (hash con BCrypt)
/// 6. Revocar todas las demás sesiones (OWASP)
/// 7. Revocar todos los refresh tokens
/// 8. Enviar notificación por email
/// 9. Registrar evento de auditoría
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository sessionRepository,
        IAuthNotificationService notificationService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ChangePasswordResponse> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        _logger.LogInformation(
            "Password change attempt initiated for user {UserId} from IP {IpAddress}",
            userId, request.IpAddress ?? "unknown");

        try
        {
            // 1. Obtener usuario
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Password change attempt for non-existent user {UserId}", userId);
                throw new NotFoundException("User not found.");
            }

            // 2. Verificar cuenta no bloqueada
            if (user.IsLockedOut())
            {
                _logger.LogWarning(
                    "Password change blocked - User {UserId} is locked out until {LockoutEnd}",
                    userId, user.LockoutEnd);
                throw new BadRequestException("Account is temporarily locked. Please try again later.");
            }

            // 3. Verificar contraseña actual
            var isCurrentPasswordValid = await _userRepository.VerifyPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                // Incrementar intentos fallidos por seguridad
                user.IncrementAccessFailedCount();
                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger.LogWarning(
                    "Password change failed - Invalid current password for user {UserId}. " +
                    "Failed attempts: {FailedAttempts}",
                    userId, user.AccessFailedCount);

                throw new BadRequestException("Current password is incorrect.");
            }

            // 4. Verificar nueva contraseña diferente a actual
            var isNewSameAsCurrent = await _userRepository.VerifyPasswordAsync(user, request.NewPassword);
            if (isNewSameAsCurrent)
            {
                _logger.LogWarning(
                    "Password change failed - New password same as current for user {UserId}",
                    userId);
                throw new BadRequestException("New password must be different from current password.");
            }

            // 5. Actualizar contraseña usando método del dominio
            user.UpdatePassword(request.NewPassword, _passwordHasher);
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation(
                "Password updated successfully for user {UserId}",
                userId);

            // 6. Revocar todas las sesiones excepto la actual (OWASP Best Practice)
            // Esto fuerza re-login en todos los dispositivos
            await _sessionRepository.RevokeAllUserSessionsAsync(
                userId,
                exceptSessionId: null, // Revocar TODAS las sesiones
                reason: "password_changed",
                cancellationToken);

            _logger.LogInformation(
                "All sessions revoked for user {UserId} due to password change",
                userId);

            // 7. Revocar todos los refresh tokens
            await _refreshTokenRepository.RevokeAllForUserAsync(
                userId,
                reason: "password_changed",
                cancellationToken);

            _logger.LogInformation(
                "All refresh tokens revoked for user {UserId} due to password change",
                userId);

            // 8. Enviar notificación por email
            try
            {
                await _notificationService.SendPasswordChangedConfirmationAsync(user.Email!);
                _logger.LogInformation(
                    "Password change confirmation email sent to user {UserId}",
                    userId);
            }
            catch (Exception emailEx)
            {
                // No fallar el proceso si el email no se envía
                _logger.LogWarning(
                    emailEx,
                    "Failed to send password change confirmation email to user {UserId}",
                    userId);
            }

            // 9. Resetear contador de intentos fallidos
            if (user.AccessFailedCount > 0)
            {
                user.ResetAccessFailedCount();
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            // Log de auditoría
            _logger.LogInformation(
                "AUTH-SEC-001: Password changed successfully for user {UserId}. " +
                "IP: {IpAddress}, UserAgent: {UserAgent}",
                userId,
                request.IpAddress ?? "unknown",
                request.UserAgent ?? "unknown");

            return new ChangePasswordResponse(
                Success: true,
                Message: "Password changed successfully. Please log in again with your new password."
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
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error during password change for user {UserId}", userId);
            throw new BadRequestException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password change for user {UserId}", userId);
            throw;
        }
    }
}
