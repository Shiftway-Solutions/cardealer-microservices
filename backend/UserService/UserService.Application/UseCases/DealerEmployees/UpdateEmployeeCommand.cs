using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// Command to update a dealer employee
/// </summary>
public record UpdateEmployeeCommand(
    Guid DealerId,
    Guid EmployeeId,
    string? Role = null,
    string? Status = null,
    Guid? LocationId = null
) : IRequest<Unit>;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    public UpdateEmployeeCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating employee {EmployeeId} for dealer {DealerId}",
            request.EmployeeId, request.DealerId);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee == null || employee.DealerId != request.DealerId)
        {
            throw new NotFoundException($"Employee {request.EmployeeId} not found for dealer {request.DealerId}");
        }

        // Update role if provided
        if (!string.IsNullOrEmpty(request.Role))
        {
            if (Enum.TryParse<DealerRole>(request.Role, out var dealerRole))
            {
                employee.DealerRole = dealerRole;
            }
            else
            {
                throw new BadRequestException($"Invalid role: {request.Role}");
            }
        }

        // Update status if provided
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<EmployeeStatus>(request.Status, out var status))
            {
                employee.Status = status;

                if (status == EmployeeStatus.Active && employee.ActivationDate == null)
                {
                    employee.ActivationDate = DateTime.UtcNow;
                }
            }
            else
            {
                throw new BadRequestException($"Invalid status: {request.Status}");
            }
        }

        // TODO: Add location management if needed
        // if (request.LocationId.HasValue)
        // {
        //     employee.LocationId = request.LocationId.Value;
        // }

        await _employeeRepository.UpdateAsync(employee);

        _logger.LogInformation("Employee {EmployeeId} updated successfully", employee.Id);

        return Unit.Value;
    }
}
