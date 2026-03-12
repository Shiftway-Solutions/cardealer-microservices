using MediatR;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using AuthService.Shared.Exceptions;
using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Entities;

namespace AuthService.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IVerificationTokenRepository _verificationTokenRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
      IUserRepository userRepository,
      IVerificationTokenRepository verificationTokenRepository,
      IAuthNotificationService notificationService,
      ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _verificationTokenRepository = verificationTokenRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            // Por seguridad, siempre devolvemos éxito incluso si el email no existe
            if (user == null)
            {
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
                return new ForgotPasswordResponse(true, "If the email exists, a reset link has been sent.");
            }

            // Crear y guardar token de reset en la base de datos
            _logger.LogInformation("Creating password reset token for user {UserId}", user.Id);

            var verificationToken = VerificationToken.CreatePasswordResetToken(
                request.Email,
                user.Id.ToString(),
                expiryHours: 1  // Token válido por 1 hora
            );

            _logger.LogInformation("Saving token to database: {TokenId}", verificationToken.Id);
            await _verificationTokenRepository.AddAsync(verificationToken);
            _logger.LogInformation("Token saved successfully with ID: {TokenId}", verificationToken.Id);

            // Enviar email con el token
            await _notificationService.SendPasswordResetEmailAsync(request.Email, verificationToken.Token);

            _logger.LogInformation("Password reset email sent to {Email}", request.Email);

            return new ForgotPasswordResponse(true, "Password reset email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset for {Email}", request.Email);
            throw new ServiceUnavailableException("An error occurred while processing your request.");
        }
    }
}
