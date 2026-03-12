using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;
using AuthService.Shared.Exceptions;
using AuthService.Application.DTOs.TwoFactor;

namespace AuthService.Application.Features.TwoFactor.Commands.Enable2FA;

public class Enable2FACommandHandler : IRequestHandler<Enable2FACommand, Enable2FAResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuthNotificationService _notificationService;

    public Enable2FACommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IAuthNotificationService notificationService)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _notificationService = notificationService;
    }

    public async Task<Enable2FAResponse> Handle(Enable2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        if (user.IsTwoFactorEnabled)
            throw new BadRequestException("Two-factor authentication is already enabled.");

        // Check if there's an existing 2FA setup (possibly in PendingVerification state)
        var existingTwoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(request.UserId);

        // If exists and in PendingVerification, we'll regenerate the setup
        // If exists and Enabled, we already threw above via IsTwoFactorEnabled
        // This allows users to restart the setup process if they didn't complete verification

        // Validar requisitos según el tipo de 2FA
        switch (request.Type)
        {
            case TwoFactorAuthType.SMS:
                // Para SMS, el usuario DEBE tener teléfono verificado
                if (string.IsNullOrEmpty(user.PhoneNumber))
                    throw new BadRequestException("Phone number is required for SMS two-factor authentication. Please add a phone number first.");
                if (!user.PhoneNumberConfirmed)
                    throw new BadRequestException("Phone number must be verified before enabling SMS two-factor authentication. Please verify your phone number first.");
                break;

            case TwoFactorAuthType.Email:
                // Para Email, el usuario DEBE tener email verificado (ya es requisito para login, pero validamos por seguridad)
                if (!user.EmailConfirmed)
                    throw new BadRequestException("Email must be verified before enabling Email two-factor authentication.");
                break;

            case TwoFactorAuthType.Authenticator:
                // Authenticator no requiere validaciones adicionales
                break;

            default:
                throw new BadRequestException("Invalid two-factor authentication type.");
        }

        string secret = string.Empty;
        string qrCodeUri = string.Empty;
        List<string> recoveryCodes;

        // Generar según el tipo de 2FA
        switch (request.Type)
        {
            case TwoFactorAuthType.Authenticator:
                // Generar clave para Authenticator App
                (secret, qrCodeUri) = await _twoFactorService.GenerateAuthenticatorKeyAsync(user.Id, user.Email!);
                break;

            case TwoFactorAuthType.SMS:
            case TwoFactorAuthType.Email:
                // Para SMS/Email, generar códigos pero no QR
                secret = await _twoFactorService.GenerateEmailCodeAsync(user.Id);
                qrCodeUri = string.Empty;
                break;
        }

        // Generar códigos de recuperación
        recoveryCodes = await _twoFactorService.GenerateRecoveryCodesAsync(user.Id);

        // Create or update 2FA entity in PendingVerification state
        // El usuario debe verificar con un código antes de que 2FA esté completamente activo
        TwoFactorAuth twoFactorAuth;
        if (existingTwoFactorAuth != null)
        {
            // Reuse existing entity (possibly restarting setup from PendingVerification)
            twoFactorAuth = existingTwoFactorAuth;
            twoFactorAuth.SetupPending(secret, recoveryCodes);
        }
        else
        {
            // Create new entity
            twoFactorAuth = new TwoFactorAuth(user.Id, request.Type);
            twoFactorAuth.SetupPending(secret, recoveryCodes);
        }

        // GUARDAR en base de datos (pero aún no está habilitado, solo PendingVerification)
        await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);

        // NO enviar códigos de recuperación por email todavía
        // Se enviarán cuando el usuario complete la verificación

        return new Enable2FAResponse(secret, qrCodeUri, recoveryCodes);
    }
}
