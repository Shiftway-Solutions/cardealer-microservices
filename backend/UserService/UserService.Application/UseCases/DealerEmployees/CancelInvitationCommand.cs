using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// Command para cancelar/revocar una invitación
/// </summary>
public record CancelInvitationCommand(Guid DealerId, Guid InvitationId) : IRequest<Unit>;

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand, Unit>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly ILogger<CancelInvitationCommandHandler> _logger;

    public CancelInvitationCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        ILogger<CancelInvitationCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling invitation {InvitationId} for dealer {DealerId}",
            request.InvitationId, request.DealerId);

        var invitation = await _employeeRepository.GetInvitationByIdAsync(request.DealerId, request.InvitationId);
        if (invitation == null)
        {
            throw new NotFoundException($"Invitation {request.InvitationId} not found");
        }

        invitation.Status = InvitationStatus.Revoked;
        await _employeeRepository.UpdateInvitationAsync(invitation);

        _logger.LogInformation("Invitation {InvitationId} cancelled", request.InvitationId);

        return Unit.Value;
    }
}
