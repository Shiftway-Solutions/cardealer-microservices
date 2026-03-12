using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// Command to remove an employee from a dealer
/// </summary>
public record RemoveEmployeeCommand(Guid DealerId, Guid EmployeeId) : IRequest<Unit>;

public class RemoveEmployeeCommandHandler : IRequestHandler<RemoveEmployeeCommand, Unit>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly ILogger<RemoveEmployeeCommandHandler> _logger;

    public RemoveEmployeeCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        ILogger<RemoveEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(RemoveEmployeeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing employee {EmployeeId} from dealer {DealerId}",
            request.EmployeeId, request.DealerId);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee == null || employee.DealerId != request.DealerId)
        {
            throw new NotFoundException($"Employee {request.EmployeeId} not found for dealer {request.DealerId}");
        }

        await _employeeRepository.DeleteAsync(employee.Id);

        _logger.LogInformation("Employee {EmployeeId} removed successfully", employee.Id);

        return Unit.Value;
    }
}
