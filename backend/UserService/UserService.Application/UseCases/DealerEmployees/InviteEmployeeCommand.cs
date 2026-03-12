using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// Command to invite a new employee to a dealer
/// </summary>
public record InviteEmployeeCommand(
    Guid DealerId,
    Guid UserId,
    string Role,
    Guid? LocationId = null
) : IRequest<DealerEmployeeDto>;

public class InviteEmployeeCommandHandler : IRequestHandler<InviteEmployeeCommand, DealerEmployeeDto>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<InviteEmployeeCommandHandler> _logger;

    public InviteEmployeeCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        IDealerRepository dealerRepository,
        IUserRepository userRepository,
        ILogger<InviteEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _dealerRepository = dealerRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DealerEmployeeDto> Handle(InviteEmployeeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inviting user {UserId} as employee for dealer {DealerId}",
            request.UserId, request.DealerId);

        // Verify dealer exists
        var dealer = await _dealerRepository.GetByIdAsync(request.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException($"Dealer {request.DealerId} not found");
        }

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException($"User {request.UserId} not found");
        }

        // Check if user is already an employee
        var existingEmployee = await _employeeRepository.GetByUserIdAndDealerIdAsync(request.UserId, request.DealerId);
        if (existingEmployee != null)
        {
            throw new BadRequestException("User is already an employee of this dealer");
        }

        // Parse role
        if (!Enum.TryParse<DealerRole>(request.Role, out var dealerRole))
        {
            throw new BadRequestException($"Invalid role: {request.Role}");
        }

        // Create employee
        var employee = new DealerEmployee
        {
            Id = Guid.NewGuid(),
            DealerId = request.DealerId,
            UserId = request.UserId,
            DealerRole = dealerRole,
            Status = EmployeeStatus.Pending,
            InvitationDate = DateTime.UtcNow,
            InvitedBy = Guid.Empty, // TODO: Get from current user context
            Permissions = "[]" // TODO: implement permissions system
        };

        await _employeeRepository.AddAsync(employee);

        _logger.LogInformation("Employee {EmployeeId} invited successfully", employee.Id);

        return new DealerEmployeeDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            DealerId = employee.DealerId,
            UserFullName = user.FullName,
            UserEmail = user.Email,
            Role = employee.DealerRole.ToString(),
            Status = employee.Status.ToString(),
            InvitationDate = employee.InvitationDate,
            ActivationDate = employee.ActivationDate,
            Permissions = employee.Permissions
        };
    }
}
