using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// Command para reenviar una invitación que aún está pendiente
/// </summary>
public record ResendInvitationCommand(
    Guid DealerId,
    Guid InvitationId,
    Guid ResendBy
) : IRequest<DealerEmployeeInvitationDto>;

public class ResendInvitationCommandHandler : IRequestHandler<ResendInvitationCommand, DealerEmployeeInvitationDto>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly ILogger<ResendInvitationCommandHandler> _logger;

    public ResendInvitationCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        IDealerRepository dealerRepository,
        ILogger<ResendInvitationCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _dealerRepository = dealerRepository;
        _logger = logger;
    }

    public async Task<DealerEmployeeInvitationDto> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resending invitation {InvitationId} for dealer {DealerId}",
            request.InvitationId, request.DealerId);

        // Verify dealer exists
        var dealer = await _dealerRepository.GetByIdAsync(request.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException($"Dealer {request.DealerId} no encontrado");
        }

        // Get the invitation
        var invitation = await _employeeRepository.GetInvitationByIdAsync(request.DealerId, request.InvitationId);
        if (invitation == null)
        {
            throw new NotFoundException($"Invitación {request.InvitationId} no encontrada");
        }

        // Verify it belongs to this dealer
        if (invitation.DealerId != request.DealerId)
        {
            throw new ForbiddenException("Esta invitación no pertenece a este dealer");
        }

        // Only pending or expired invitations can be resent
        if (invitation.Status != InvitationStatus.Pending && invitation.Status != InvitationStatus.Expired)
        {
            throw new BadRequestException($"No se puede reenviar una invitación con estado: {invitation.Status}");
        }

        // Update invitation with new token and expiration
        invitation.Token = Guid.NewGuid().ToString("N");
        invitation.ExpirationDate = DateTime.UtcNow.AddDays(7);
        invitation.Status = InvitationStatus.Pending;
        invitation.InvitationDate = DateTime.UtcNow;

        await _employeeRepository.UpdateInvitationAsync(invitation);

        _logger.LogInformation("Invitation {InvitationId} resent with new token", invitation.Id);

        // TODO: Send email notification via NotificationService

        return new DealerEmployeeInvitationDto
        {
            Id = invitation.Id,
            Email = invitation.Email,
            Role = invitation.DealerRole.ToString(),
            Status = invitation.Status.ToString(),
            InvitationDate = invitation.InvitationDate,
            ExpirationDate = invitation.ExpirationDate
        };
    }
}
