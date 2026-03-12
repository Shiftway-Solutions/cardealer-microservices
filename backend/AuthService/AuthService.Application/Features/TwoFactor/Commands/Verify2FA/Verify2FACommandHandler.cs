// AuthService.Application/UseCases/Verify2FA/Verify2FACommandHandler.cs
using AuthService.Shared.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;
using AuthService.Application.DTOs.TwoFactor;

namespace AuthService.Application.Features.TwoFactor.Commands.Verify2FA;

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, Verify2FAResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuthNotificationService _notificationService;

    public Verify2FACommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IAuthNotificationService notificationService)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _notificationService = notificationService;
    }

    public async Task<Verify2FAResponse> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        if (user.TwoFactorAuth == null)
            throw new BadRequestException("Two-factor authentication is not set up for this user.");

        bool isValid;

        // Verificar código según el tipo
        switch (request.Type)
        {
            case TwoFactorAuthType.Authenticator:
                if (string.IsNullOrEmpty(user.TwoFactorAuth.Secret))
                    throw new BadRequestException("Two-factor authentication secret is not configured.");

                isValid = _twoFactorService.VerifyAuthenticatorCode(user.TwoFactorAuth.Secret, request.Code);
                break;

            case TwoFactorAuthType.SMS:
            case TwoFactorAuthType.Email:
                isValid = await _twoFactorService.VerifyCodeAsync(user.Id, request.Code, request.Type);
                break;

            default:
                throw new BadRequestException("Invalid two-factor authentication type.");
        }

        if (isValid)
        {
            // Si está en PendingVerification, confirmar el enable
            if (user.TwoFactorAuth.Status == TwoFactorAuthStatus.PendingVerification)
            {
                user.TwoFactorAuth.ConfirmEnable();

                // Ahora sí enviar los códigos de recuperación por email
                if (user.TwoFactorAuth.RecoveryCodes.Any())
                {
                    await _notificationService.SendTwoFactorBackupCodesAsync(
                        user.Email!,
                        user.TwoFactorAuth.RecoveryCodes);
                }
            }

            // Marcar como usado
            user.TwoFactorAuth.MarkAsUsed();
            await _userRepository.UpdateAsync(user, cancellationToken);

            return new Verify2FAResponse(true, "Two-factor authentication verified and enabled successfully.");
        }

        // Incrementar intentos fallidos
        user.TwoFactorAuth.IncrementFailedAttempts();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return new Verify2FAResponse(false, "Invalid verification code.");
    }
}
