using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// DTO para respuesta de invitación
/// </summary>
public class DealerEmployeeInvitationDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime InvitationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
}

/// <summary>
/// Command para enviar una invitación de empleado por email
/// </summary>
public record SendInvitationCommand(
    Guid DealerId,
    string Email,
    string Role,
    string? Permissions,
    Guid InvitedBy
) : IRequest<DealerEmployeeInvitationDto>;

public class SendInvitationCommandHandler : IRequestHandler<SendInvitationCommand, DealerEmployeeInvitationDto>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SendInvitationCommandHandler> _logger;

    public SendInvitationCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        IDealerRepository dealerRepository,
        IUserRepository userRepository,
        ILogger<SendInvitationCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _dealerRepository = dealerRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DealerEmployeeInvitationDto> Handle(SendInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending invitation to {Email} for dealer {DealerId}",
            request.Email, request.DealerId);

        // Verify dealer exists
        var dealer = await _dealerRepository.GetByIdAsync(request.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException($"Dealer {request.DealerId} not found");
        }

        // Check if user already exists with this email
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            // Check if already an employee
            var existingEmployee = await _employeeRepository.GetByUserIdAndDealerIdAsync(existingUser.Id, request.DealerId);
            if (existingEmployee != null)
            {
                throw new BadRequestException("This email is already associated with an employee of this dealer");
            }
        }

        // Check for existing pending invitation
        var existingInvitation = await _employeeRepository.GetPendingInvitationByEmailAsync(request.DealerId, request.Email);
        if (existingInvitation != null)
        {
            throw new BadRequestException("An invitation has already been sent to this email");
        }

        // Parse role
        if (!Enum.TryParse<DealerRole>(request.Role, out var dealerRole))
        {
            throw new BadRequestException($"Invalid role: {request.Role}");
        }

        // Create invitation
        var invitation = new DealerEmployeeInvitation
        {
            Id = Guid.NewGuid(),
            DealerId = request.DealerId,
            Email = request.Email,
            DealerRole = dealerRole,
            Permissions = request.Permissions ?? "[]",
            InvitedBy = request.InvitedBy,
            Status = InvitationStatus.Pending,
            InvitationDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Token = Guid.NewGuid().ToString("N")
        };

        await _employeeRepository.AddInvitationAsync(invitation);

        _logger.LogInformation("Invitation {InvitationId} sent to {Email}", invitation.Id, request.Email);

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
