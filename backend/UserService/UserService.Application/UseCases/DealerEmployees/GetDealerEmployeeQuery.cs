using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// Query to get a specific dealer employee
/// </summary>
public record GetDealerEmployeeQuery(Guid DealerId, Guid EmployeeId) : IRequest<DealerEmployeeDto>;

public class GetDealerEmployeeQueryHandler : IRequestHandler<GetDealerEmployeeQuery, DealerEmployeeDto>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetDealerEmployeeQueryHandler> _logger;

    public GetDealerEmployeeQueryHandler(
        IDealerEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        ILogger<GetDealerEmployeeQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DealerEmployeeDto> Handle(GetDealerEmployeeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting employee {EmployeeId} for dealer {DealerId}",
            request.EmployeeId, request.DealerId);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee == null || employee.DealerId != request.DealerId)
        {
            throw new NotFoundException($"Employee {request.EmployeeId} not found for dealer {request.DealerId}");
        }

        var user = await _userRepository.GetByIdAsync(employee.UserId);

        return new DealerEmployeeDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            DealerId = employee.DealerId,
            UserFullName = user?.FullName ?? "Sin nombre",
            UserEmail = user?.Email ?? "Sin email",
            Role = employee.DealerRole.ToString(),
            Status = employee.Status.ToString(),
            InvitationDate = employee.InvitationDate,
            ActivationDate = employee.ActivationDate,
            Permissions = "[]" // TODO: implement permissions
        };
    }
}
