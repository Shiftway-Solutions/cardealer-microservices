using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.DTOs.Privacy;
using UserService.Application.Interfaces;
using UserService.Domain.Entities.Privacy;
using UserService.Domain.Interfaces;

namespace UserService.Application.UseCases.Privacy.RequestAccountDeletion;

/// <summary>
/// Command para solicitar eliminación de cuenta (Cancelación ARCO)
/// </summary>
public record RequestAccountDeletionCommand(
    Guid UserId,
    string? Email, // Email del usuario para buscar en UserService DB
    DeletionReason Reason,
    string? OtherReason,
    string? Feedback,
    string? IpAddress,
    string? UserAgent
) : IRequest<AccountDeletionResponseDto>;

/// <summary>
/// Handler para RequestAccountDeletionCommand
/// </summary>
public class RequestAccountDeletionCommandHandler : IRequestHandler<RequestAccountDeletionCommand, AccountDeletionResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationServiceClient _notificationClient;
    private readonly IPrivacyRequestRepository _privacyRepository;

    public RequestAccountDeletionCommandHandler(
        IUserRepository userRepository,
        INotificationServiceClient notificationClient,
        IPrivacyRequestRepository privacyRepository)
    {
        _userRepository = userRepository;
        _notificationClient = notificationClient;
        _privacyRepository = privacyRepository;
    }

    public async Task<AccountDeletionResponseDto> Handle(RequestAccountDeletionCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener datos del usuario - buscar por email ya que los IDs entre AuthService y UserService pueden diferir
        Domain.Entities.User? user = null;

        // Primero intentar buscar por email (más confiable entre microservicios)
        if (!string.IsNullOrEmpty(request.Email))
        {
            user = await _userRepository.GetByEmailAsync(request.Email);
        }

        // Fallback: buscar por ID
        if (user == null)
        {
            user = await _userRepository.GetByIdAsync(request.UserId);
        }

        if (user == null)
        {
            throw new InvalidOperationException($"Usuario no encontrado. Email: {request.Email ?? "N/A"}, ID: {request.UserId}");
        }

        // 2. Verificar si ya existe una solicitud pendiente (usar el ID de UserService)
        var existingRequest = await _privacyRepository.GetPendingDeletionRequestAsync(user.Id);

        if (existingRequest != null)
        {
            return new AccountDeletionResponseDto(
                RequestId: existingRequest.Id,
                Status: "AlreadyPending",
                Message: "Ya tienes una solicitud de eliminación pendiente. Revisa tu email para el código de confirmación.",
                GracePeriodEndsAt: existingRequest.GracePeriodEndsAt ?? DateTime.UtcNow.AddDays(15),
                ConfirmationEmailSentTo: MaskEmail(user.Email)
            );
        }

        // 3. Generar código de confirmación (6 dígitos)
        var confirmationCode = GenerateConfirmationCode();
        var gracePeriodEnds = DateTime.UtcNow.AddDays(15); // 15 días de gracia según Ley 172-13

        // 4. Crear PrivacyRequest en base de datos
        var privacyRequest = new PrivacyRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id, // Usar el ID de UserService, no el de AuthService
            Type = PrivacyRequestType.Cancellation,
            Status = PrivacyRequestStatus.Pending,
            DeletionReason = request.Reason.ToString(),
            DeletionReasonOther = request.OtherReason,
            GracePeriodEndsAt = gracePeriodEnds,
            ConfirmationCode = confirmationCode,
            IsConfirmed = false,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            Description = request.Feedback,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1) // Código expira en 24 horas
        };

        await _privacyRepository.AddAsync(privacyRequest);

        // 5. Enviar email de confirmación con código
        await _notificationClient.SendAccountDeletionConfirmationCodeAsync(
            user.Email,
            user.FirstName ?? "Usuario",
            confirmationCode,
            gracePeriodEnds
        );

        return new AccountDeletionResponseDto(
            RequestId: privacyRequest.Id,
            Status: "PendingConfirmation",
            Message: "Hemos enviado un código de confirmación a tu email. Tu cuenta será eliminada en 15 días si no cancelas la solicitud.",
            GracePeriodEndsAt: gracePeriodEnds,
            ConfirmationEmailSentTo: MaskEmail(user.Email)
        );
    }

    private static string GenerateConfirmationCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "***@***.***";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***@***.***";

        var localPart = parts[0];
        var domainPart = parts[1];

        var maskedLocal = localPart.Length > 2
            ? localPart[0] + new string('*', localPart.Length - 2) + localPart[^1]
            : new string('*', localPart.Length);

        var domainParts = domainPart.Split('.');
        var maskedDomain = domainParts[0].Length > 2
            ? domainParts[0][0] + new string('*', domainParts[0].Length - 2) + domainParts[0][^1]
            : new string('*', domainParts[0].Length);

        return $"{maskedLocal}@{maskedDomain}.{string.Join(".", domainParts[1..])}";
    }
}
