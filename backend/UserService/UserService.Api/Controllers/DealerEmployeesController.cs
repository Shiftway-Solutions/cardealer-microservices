using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.UseCases.DealerEmployees;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller for managing dealer employees (team members)
/// </summary>
[ApiController]
[Route("api/dealers/{dealerId}/employees")]
[Produces("application/json")]
public class DealerEmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DealerEmployeesController> _logger;

    public DealerEmployeesController(IMediator mediator, ILogger<DealerEmployeesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees for a dealer
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DealerEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DealerEmployeeDto>>> GetEmployees(Guid dealerId)
    {
        _logger.LogInformation("Getting employees for dealer {DealerId}", dealerId);
        var result = await _mediator.Send(new GetDealerEmployeesQuery(dealerId));
        return Ok(result);
    }

    /// <summary>
    /// Get a specific employee
    /// </summary>
    [HttpGet("{employeeId}")]
    [ProducesResponseType(typeof(DealerEmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DealerEmployeeDto>> GetEmployee(Guid dealerId, Guid employeeId)
    {
        _logger.LogInformation("Getting employee {EmployeeId} for dealer {DealerId}", employeeId, dealerId);
        var result = await _mediator.Send(new GetDealerEmployeeQuery(dealerId, employeeId));

        if (result == null)
            return NotFound(new { error = "Employee not found" });

        return Ok(result);
    }

    /// <summary>
    /// Invite a new employee to the dealer team
    /// </summary>
    [HttpPost("invite")]
    [ProducesResponseType(typeof(DealerEmployeeInvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DealerEmployeeInvitationDto>> InviteEmployee(
        Guid dealerId,
        [FromBody] InviteEmployeeRequest request)
    {
        _logger.LogInformation("Inviting employee {Email} for dealer {DealerId}", request.Email, dealerId);

        var command = new SendInvitationCommand(
            DealerId: dealerId,
            Email: request.Email,
            Role: request.Role,
            Permissions: request.Permissions,
            InvitedBy: request.InvitedBy
        );

        var result = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetEmployee), new { dealerId, employeeId = result.Id }, result);
    }

    /// <summary>
    /// Update employee role and permissions
    /// </summary>
    [HttpPut("{employeeId}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateEmployeeRole(
        Guid dealerId,
        Guid employeeId,
        [FromBody] UpdateEmployeeRoleRequest request)
    {
        _logger.LogInformation("Updating role for employee {EmployeeId} in dealer {DealerId}", employeeId, dealerId);

        var command = new UpdateEmployeeCommand(
            DealerId: dealerId,
            EmployeeId: employeeId,
            Role: request.Role,
            Status: null
        );

        await _mediator.Send(command);

        return NoContent();
    }

    /// <summary>
    /// Remove an employee from the team
    /// </summary>
    [HttpDelete("{employeeId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveEmployee(Guid dealerId, Guid employeeId)
    {
        _logger.LogInformation("Removing employee {EmployeeId} from dealer {DealerId}", employeeId, dealerId);

        await _mediator.Send(new RemoveEmployeeCommand(dealerId, employeeId));

        return NoContent();
    }

    /// <summary>
    /// Get pending invitations
    /// </summary>
    [HttpGet("invitations")]
    [ProducesResponseType(typeof(IEnumerable<DealerEmployeeInvitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DealerEmployeeInvitationDto>>> GetInvitations(Guid dealerId)
    {
        _logger.LogInformation("Getting invitations for dealer {DealerId}", dealerId);
        var result = await _mediator.Send(new GetInvitationsQuery(dealerId));
        return Ok(result);
    }

    /// <summary>
    /// Cancel an invitation
    /// </summary>
    [HttpDelete("invitations/{invitationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelInvitation(Guid dealerId, Guid invitationId)
    {
        _logger.LogInformation("Cancelling invitation {InvitationId} for dealer {DealerId}", invitationId, dealerId);

        await _mediator.Send(new CancelInvitationCommand(dealerId, invitationId));

        return NoContent();
    }

    /// <summary>
    /// Resend an invitation that is pending or expired
    /// </summary>
    [HttpPost("invitations/{invitationId}/resend")]
    [ProducesResponseType(typeof(DealerEmployeeInvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DealerEmployeeInvitationDto>> ResendInvitation(
        Guid dealerId,
        Guid invitationId)
    {
        _logger.LogInformation("Resending invitation {InvitationId} for dealer {DealerId}", invitationId, dealerId);

        // TODO: Get current user ID from JWT claims
        var currentUserId = Guid.Empty; // Placeholder - should come from User.Claims

        var result = await _mediator.Send(new ResendInvitationCommand(dealerId, invitationId, currentUserId));

        return Ok(result);
    }

    /// <summary>
    /// Get available roles
    /// </summary>
    [HttpGet("~/api/dealer-roles")]
    [ProducesResponseType(typeof(IEnumerable<RoleDefinitionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<RoleDefinitionDto>> GetAvailableRoles()
    {
        var roles = new List<RoleDefinitionDto>
        {
            new()
            {
                Id = "Owner",
                Name = "Propietario",
                Description = "Control total del dealer",
                Permissions = new[] { "all" }
            },
            new()
            {
                Id = "Admin",
                Name = "Administrador",
                Description = "Gestión completa excepto facturación",
                Permissions = new[] { "inventory", "leads", "users", "reports", "settings" }
            },
            new()
            {
                Id = "SalesManager",
                Name = "Gerente de Ventas",
                Description = "Gestión de ventas y equipo de vendedores",
                Permissions = new[] { "inventory", "leads", "sales_team", "reports" }
            },
            new()
            {
                Id = "Salesperson",
                Name = "Vendedor",
                Description = "Gestión de leads y vehículos asignados",
                Permissions = new[] { "inventory_view", "leads_assigned", "own_stats" }
            },
            new()
            {
                Id = "InventoryManager",
                Name = "Gestor de Inventario",
                Description = "Gestión del inventario de vehículos",
                Permissions = new[] { "inventory", "photos", "pricing" }
            },
            new()
            {
                Id = "Viewer",
                Name = "Solo Lectura",
                Description = "Acceso de solo visualización",
                Permissions = new[] { "view_only" }
            }
        };

        return Ok(roles);
    }
}

// Request DTOs
public class InviteEmployeeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Salesperson";
    public string? Permissions { get; set; }
    public Guid InvitedBy { get; set; }
}

public class UpdateEmployeeRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public string[]? Permissions { get; set; }
}

public class RoleDefinitionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = Array.Empty<string>();
}
