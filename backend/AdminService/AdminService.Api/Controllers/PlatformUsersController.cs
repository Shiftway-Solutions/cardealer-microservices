using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.UseCases.PlatformUsers;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for managing platform users (customers, buyers, sellers)
/// This is for admin management of regular platform users, not admin users
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class PlatformUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PlatformUsersController> _logger;

    public PlatformUsersController(IMediator mediator, ILogger<PlatformUsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all platform users with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedUserResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedUserResult>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? verified = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting platform users - search={Search}, type={Type}, status={Status}, page={Page}",
            search, type, status, page);

        var result = await _mediator.Send(new GetPlatformUsersQuery(search, type, status, verified, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Get platform user statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PlatformUserStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformUserStatsDto>> GetUserStats()
    {
        _logger.LogInformation("Getting platform user statistics");

        var result = await _mediator.Send(new GetPlatformUserStatsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get platform user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PlatformUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlatformUserDetailDto>> GetUser(Guid id)
    {
        _logger.LogInformation("Getting platform user {UserId}", id);

        var result = await _mediator.Send(new GetPlatformUserQuery(id));

        if (result == null)
            return NotFound(new { error = "User not found" });

        return Ok(result);
    }

    /// <summary>
    /// Update platform user status (active, suspended, banned)
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var allowedStatuses = new[] { "active", "suspended", "banned" };
        if (string.IsNullOrWhiteSpace(request.Status) || !allowedStatuses.Contains(request.Status.ToLowerInvariant()))
            return BadRequest(new { Error = $"Invalid status. Allowed: {string.Join(", ", allowedStatuses)}" });

        if (request.Status.ToLowerInvariant() != "active" && string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { Error = "A reason is required when suspending or banning a user" });

        _logger.LogInformation("Updating platform user {UserId} status to {Status}", id, request.Status);

        await _mediator.Send(new UpdatePlatformUserStatusCommand(id, request.Status.ToLowerInvariant(), request.Reason));
        return NoContent();
    }

    /// <summary>
    /// Verify a platform user
    /// </summary>
    [HttpPost("{id}/verify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerifyUser(Guid id)
    {
        _logger.LogInformation("Verifying platform user {UserId}", id);

        await _mediator.Send(new VerifyPlatformUserCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Delete a platform user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting platform user {UserId}", id);

        await _mediator.Send(new DeletePlatformUserCommand(id));
        return NoContent();
    }
}

/// <summary>
/// Request model for updating user status
/// </summary>
public class UpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty; // active, suspended, banned
    public string? Reason { get; set; }
}
