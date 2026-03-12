using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Persistence;
using CarDealer.Contracts.Events.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Controllers;

/// <summary>
/// Controller for admin user management operations
/// Used by AdminService to create admin users and manage security
/// All endpoints require Admin or SuperAdmin role (OWASP A01:2021)
/// Rate-limited to prevent abuse of admin creation endpoint (OWASP API4:2023)
/// </summary>
[ApiController]
[Route("api/auth/admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
[EnableRateLimiting("AuthPolicy")]
public class AdminManagementController : ControllerBase
{
    private readonly ILogger<AdminManagementController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    // Default admin email that was seeded
    private const string DefaultAdminEmail = "admin@okla.local";

    public AdminManagementController(
        ILogger<AdminManagementController> logger,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Register a new admin user (called by AdminService after invitation acceptance)
    /// Requires SuperAdmin role - creating admin accounts is a privileged operation
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(AdminRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRegistrationResponse>> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        _logger.LogInformation("Creating admin user {Email} with role {Role}", request.Email, request.Role);

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { error = "Ya existe un usuario con este email" });
        }

        // Validate role
        var validRoles = new[] { "SuperAdmin", "Admin", "Moderator", "Support", "Analyst", "Compliance" };
        if (!validRoles.Contains(request.Role))
        {
            return BadRequest(new { error = $"Rol inválido: {request.Role}" });
        }

        // Ensure role exists
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }

        // Create the admin user
        var user = new ApplicationUser(request.Email, request.Email, "temp-hash");

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to create admin user {Email}: {Errors}", request.Email, errors);
            return BadRequest(new { error = errors });
        }

        // Update user properties
        user.AccountType = AccountType.Admin;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.EmailConfirmed = true; // Admin emails are pre-verified via invitation

        await _userManager.UpdateAsync(user);

        // Assign role
        await _userManager.AddToRoleAsync(user, request.Role);

        // Also add to Admin role if they're SuperAdmin
        if (request.Role == "SuperAdmin" && !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }

        // Publish UserRegisteredEvent so UserService creates the user profile
        var userRegisteredEvent = new UserRegisteredEvent
        {
            UserId = Guid.Parse(user.Id),
            Email = user.Email!,
            FullName = $"{request.FirstName} {request.LastName}".Trim(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            RegisteredAt = user.CreatedAt,
            Metadata = new Dictionary<string, string>
            {
                { "AccountType", ((int)AccountType.Admin).ToString() },
                { "Role", request.Role },
                { "Source", "StaffInvitation" }
            }
        };

        try
        {
            await _eventPublisher.PublishAsync(userRegisteredEvent, CancellationToken.None);
            _logger.LogInformation("UserRegisteredEvent published for admin user {Email} → UserService sync", request.Email);
        }
        catch (Exception ex)
        {
            // Log but don't fail the registration - UserService sync is eventually consistent
            _logger.LogWarning(ex, "Failed to publish UserRegisteredEvent for admin user {Email}. UserService sync may be delayed.", request.Email);
        }

        _logger.LogInformation("Admin user {Email} created successfully with role {Role}", request.Email, request.Role);

        return CreatedAtAction(nameof(RegisterAdmin), new AdminRegistrationResponse
        {
            UserId = Guid.Parse(user.Id),
            Email = user.Email!,
            Role = request.Role
        });
    }

    /// <summary>
    /// Get platform security status
    /// Returns info about default admin and real admin counts
    /// Requires SuperAdmin role to prevent information disclosure (OWASP A01:2021)
    /// </summary>
    [HttpGet("security-status")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(SecurityStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SecurityStatusResponse>> GetSecurityStatus()
    {
        _logger.LogInformation("Getting platform security status");

        // Check if default admin exists
        var defaultAdmin = await _userManager.FindByEmailAsync(DefaultAdminEmail);
        var defaultAdminExists = defaultAdmin != null;

        // Count real SuperAdmins (excluding default admin)
        var superAdminRole = await _roleManager.FindByNameAsync("SuperAdmin");
        var realSuperAdminCount = 0;
        var totalAdminCount = 0;

        if (superAdminRole != null)
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            realSuperAdminCount = superAdmins.Count(u => u.Email != DefaultAdminEmail);
            totalAdminCount = superAdmins.Count;
        }

        return Ok(new SecurityStatusResponse
        {
            DefaultAdminExists = defaultAdminExists,
            RealSuperAdminCount = realSuperAdminCount,
            TotalAdminCount = totalAdminCount
        });
    }

    /// <summary>
    /// Delete the default admin account
    /// Only allowed if at least one real SuperAdmin exists
    /// </summary>
    [HttpDelete("default-admin")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDefaultAdmin([FromQuery] Guid requestedBy)
    {
        _logger.LogWarning("Request to delete default admin by {RequestedBy}", requestedBy);

        // Find default admin
        var defaultAdmin = await _userManager.FindByEmailAsync(DefaultAdminEmail);
        if (defaultAdmin == null)
        {
            return NotFound(new { error = "El usuario admin por defecto no existe" });
        }

        // Verify requesting user is not the default admin
        if (defaultAdmin.Id == requestedBy.ToString())
        {
            return BadRequest(new { error = "No puedes eliminar tu propia cuenta mientras estás autenticado con ella" });
        }

        // Count real SuperAdmins
        var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
        var realSuperAdminCount = superAdmins.Count(u => u.Email != DefaultAdminEmail);

        if (realSuperAdminCount == 0)
        {
            return BadRequest(new { error = "Debes crear al menos un SuperAdmin real antes de eliminar el admin por defecto" });
        }

        // Delete the default admin
        var result = await _userManager.DeleteAsync(defaultAdmin);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to delete default admin: {Errors}", errors);
            return BadRequest(new { error = errors });
        }

        _logger.LogWarning("Default admin account {Email} has been deleted by {RequestedBy}", DefaultAdminEmail, requestedBy);

        return NoContent();
    }
}

// ============================================================================
// REQUEST/RESPONSE DTOs
// ============================================================================

public class RegisterAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Admin";
}

public class AdminRegistrationResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}

public class SecurityStatusResponse
{
    public bool DefaultAdminExists { get; set; }
    public int RealSuperAdminCount { get; set; }
    public int TotalAdminCount { get; set; }
}
