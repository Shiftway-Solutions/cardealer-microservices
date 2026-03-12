using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.UseCases.AdminUsers;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for managing admin users and platform administration
/// </summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMediator mediator, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ========================================
    // ADMIN STAFF MANAGEMENT (internal admin users, not platform users)
    // ========================================

    /// <summary>
    /// Get all admin staff users (internal admins, not platform customers)
    /// </summary>
    [HttpGet("staff")]
    [ProducesResponseType(typeof(PaginatedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<AdminUserDto>>> GetAdminUsers(
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting admin users with filters: role={Role}, isActive={IsActive}", role, isActive);

        var result = await _mediator.Send(new GetAdminUsersQuery(role, isActive, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Get admin staff user by ID
    /// </summary>
    [HttpGet("staff/{userId}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailDto>> GetAdminUser(Guid userId)
    {
        _logger.LogInformation("Getting admin user {UserId}", userId);

        var result = await _mediator.Send(new GetAdminUserQuery(userId));

        if (result == null)
            return NotFound(new { error = "Admin user not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get current admin user profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserDetailDto>> GetCurrentAdmin()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting current admin profile for {UserId}", userId);

        var result = await _mediator.Send(new GetAdminUserQuery(userId));

        if (result == null)
            return NotFound(new { error = "Admin profile not found" });

        return Ok(result);
    }

    /// <summary>
    /// Update admin staff role (SuperAdmin only)
    /// </summary>
    [HttpPut("staff/{userId}/role")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateAdminRole(Guid userId, [FromBody] UpdateAdminRoleRequest request)
    {
        _logger.LogInformation("Updating role for admin user {UserId} to {Role}", userId, request.Role);

        await _mediator.Send(new UpdateAdminRoleCommand(userId, request.Role, request.Permissions));

        return NoContent();
    }

    /// <summary>
    /// Suspend admin staff user (SuperAdmin only)
    /// </summary>
    [HttpPost("staff/{userId}/suspend")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SuspendAdmin(Guid userId, [FromBody] SuspendAdminRequest? request = null)
    {
        _logger.LogInformation("Suspending admin user {UserId}", userId);

        await _mediator.Send(new SuspendAdminUserCommand(userId, request?.Reason));

        return NoContent();
    }

    /// <summary>
    /// Reactivate admin staff user (SuperAdmin only)
    /// </summary>
    [HttpPost("staff/{userId}/reactivate")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ReactivateAdmin(Guid userId)
    {
        _logger.LogInformation("Reactivating admin user {UserId}", userId);

        await _mediator.Send(new ReactivateAdminUserCommand(userId));

        return NoContent();
    }

    // ========================================
    // DASHBOARD & OVERVIEW → Use DashboardController (/api/admin/dashboard/*)
    // These endpoints were consolidated into DashboardController to avoid duplication
    // ========================================

    // ========================================
    // SYSTEM SETTINGS (SuperAdmin only)
    // ========================================

    /// <summary>
    /// Get platform settings
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(PlatformSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformSettingsDto>> GetSettings()
    {
        _logger.LogInformation("Getting platform settings");

        var result = await _mediator.Send(new GetPlatformSettingsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Update platform settings
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateSettings([FromBody] UpdatePlatformSettingsRequest request)
    {
        _logger.LogInformation("Updating platform settings");

        await _mediator.Send(new UpdatePlatformSettingsCommand(request.Settings));

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }
}

// ============================================================================
// REQUEST DTOs
// ============================================================================

public class UpdateAdminRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public string[]? Permissions { get; set; }
}

public class SuspendAdminRequest
{
    public string? Reason { get; set; }
}

public class UpdatePlatformSettingsRequest
{
    public Dictionary<string, object> Settings { get; set; } = new();
}

// ============================================================================
// HELPER CLASSES
// ============================================================================

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
