using MediatR;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using AuthService.Shared.Exceptions;
using AuthService.Domain.Exceptions;
using AuthService.Application.DTOs.Auth;

namespace AuthService.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetTokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
      IUserRepository userRepository,
      IPasswordHasher passwordHasher,
      IPasswordResetTokenService tokenService,
      IRefreshTokenRepository refreshTokenRepository,
      IAuthNotificationService notificationService,
      ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validar token (async — avoids thread pool starvation from .Result)
            var (isValidToken, email) = await _tokenService.ValidateResetTokenAsync(request.Token);
            if (!isValidToken)
            {
                throw new BadRequestException("Invalid or expired reset token.");
            }

            // Verificar que el token es válido para el email
            if (!await _tokenService.IsTokenValidAsync(email, request.Token))
            {
                throw new BadRequestException("Invalid or expired reset token.");
            }

            // Buscar usuario
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            // Actualizar contraseña
            user.UpdatePassword(request.NewPassword, _passwordHasher);
            await _userRepository.UpdateAsync(user);

            // Invalidar token de reset
            await _tokenService.InvalidateTokenAsync(email);

            // 🔐 SEGURIDAD: Revocar TODOS los refresh tokens del usuario
            // Esto cierra todas las sesiones activas (OWASP recomendación)
            await _refreshTokenRepository.RevokeAllForUserAsync(
                user.Id.ToString(),
                "password_reset",
                cancellationToken);
            _logger.LogInformation("All refresh tokens revoked for user {UserId} due to password reset", user.Id);

            // 📧 Enviar email de confirmación de cambio de contraseña
            try
            {
                await _notificationService.SendPasswordChangedConfirmationAsync(email);
                _logger.LogInformation("Password change confirmation email sent to {Email}", email);
            }
            catch (Exception emailEx)
            {
                // No fallar el proceso si el email no se envía
                _logger.LogWarning(emailEx, "Failed to send password change confirmation email to {Email}", email);
            }

            _logger.LogInformation("Password reset completed successfully for user {UserId}", user.Id);

            return new ResetPasswordResponse(
                Message: "Password has been reset successfully.",
                Success: true
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
            _logger.LogError(ex, "Unexpected error during password reset");
            throw;
        }
    }
}