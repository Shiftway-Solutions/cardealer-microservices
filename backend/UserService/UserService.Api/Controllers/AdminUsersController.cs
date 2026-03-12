using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.UseCases.Users.UpdateAccountType;
using UserService.Domain.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Api.Controllers;

/// <summary>
/// Admin endpoints for managing platform users
/// Called by AdminService for the admin panel
/// All endpoints require Admin or SuperAdmin role (OWASP A01:2021)
/// </summary>
[ApiController]
[Route("api/users/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminUsersController> _logger;
    private readonly IUserRepository _userRepository;

    public AdminUsersController(
        IMediator mediator,
        ILogger<AdminUsersController> logger,
        IUserRepository userRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get all platform users with filtering and pagination (for admin panel)
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<AdminUserListResponse>> GetUsersList(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? verified = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Admin: Getting users list - search={Search}, type={Type}, status={Status}, page={Page}",
            search, type, status, page);

        try
        {
            // Try with filters first, fall back to basic list
            IEnumerable<User> users;
            int totalCount;

            try
            {
                users = await _userRepository.GetAllWithFiltersAsync(
                    search, type, status, verified, page, pageSize);
                totalCount = await _userRepository.CountWithFiltersAsync(search, type, status, verified);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetAllWithFiltersAsync failed, falling back to GetAllAsync");
                users = await _userRepository.GetAllAsync(page, pageSize);
                totalCount = await _userRepository.CountAsync();
            }

            // Filter out platform admin/employee accounts from the regular users list
            var filteredUsers = users.Where(u => !ExcludedAccountTypes.Contains(u.AccountType));

            var userDtos = filteredUsers.Select(u => new AdminUserDto
            {
                Id = u.Id.ToString(),
                Name = u.FullName,
                Email = u.Email,
                Phone = u.PhoneNumber,
                Avatar = u.ProfilePicture,
                Type = MapUserType(u.AccountType, u.UserIntent),
                UserIntent = MapUserIntent(u.UserIntent),
                Status = u.IsActive ? "active" : "pending",
                Verified = u.IsEmailVerified,
                EmailVerified = u.EmailConfirmed,
                CreatedAt = u.CreatedAt,
                LastActiveAt = u.LastLoginAt ?? u.CreatedAt, // Fallback to CreatedAt if never logged in
                VehiclesCount = 0, // Will be populated from VehiclesSaleService later
                FavoritesCount = 0,
                DealsCount = 0
            }).ToList();

            // Use CountWithFiltersAsync which already excludes admin accounts
            var platformUserCount = await _userRepository.CountWithFiltersAsync(search, type, status, verified);

            return Ok(new AdminUserListResponse
            {
                Data = userDtos,
                TotalCount = platformUserCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users list for admin");
            return Ok(new AdminUserListResponse
            {
                Data = new List<AdminUserDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            });
        }
    }

    /// <summary>
    /// Get user statistics for admin dashboard
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AdminUserStatsResponse>> GetUserStats()
    {
        _logger.LogInformation("Admin: Getting user statistics");

        try
        {
            // Use CountWithFiltersAsync which already excludes Admin/PlatformEmployee accounts
            var total = await _userRepository.CountWithFiltersAsync(null, null, null, null);

            int active = 0;
            int newThisMonth = 0;

            try
            {
                // Count only active platform users (excludes admin/staff)
                active = await _userRepository.CountWithFiltersAsync(null, null, "active", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CountWithFiltersAsync for active failed, using total as active");
                active = total;
            }

            try
            {
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                newThisMonth = await _userRepository.CountCreatedSinceAsync(startOfMonth);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CountCreatedSinceAsync failed");
            }

            return Ok(new AdminUserStatsResponse
            {
                Total = total,
                Active = active,
                Suspended = 0,
                Pending = total - active,
                NewThisMonth = newThisMonth
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats for admin");
            return Ok(new AdminUserStatsResponse
            {
                Total = 0,
                Active = 0,
                Suspended = 0,
                Pending = 0,
                NewThisMonth = 0
            });
        }
    }

    /// <summary>
    /// Update user status (activate, suspend, ban)
    /// </summary>
    [HttpPatch("{userId}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateStatusRequest request)
    {
        _logger.LogInformation("Admin: Updating user {UserId} status to {Status}", userId, request.Status);

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Only IsActive is supported in current schema
            user.IsActive = request.Status.ToLower() == "active";
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} status", userId);
            return StatusCode(500, new { error = "Error updating user status" });
        }
    }

    /// <summary>
    /// Verify a user
    /// </summary>
    [HttpPost("{userId}/verify")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult> VerifyUser(Guid userId)
    {
        _logger.LogInformation("Admin: Verifying user {UserId}", userId);

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            user.IsEmailVerified = true;
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying user {UserId}", userId);
            return StatusCode(500, new { error = "Error verifying user" });
        }
    }

    /// <summary>
    /// Update user's AccountType (e.g., Individual → Admin)
    /// Requires Admin or SuperAdmin role. Creates audit trail.
    /// </summary>
    [HttpPatch("{userId}/account-type")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult> UpdateAccountType(Guid userId, [FromBody] UpdateAccountTypeRequest request)
    {
        _logger.LogInformation("Admin: Updating AccountType for user {UserId} to {AccountType}", userId, request.AccountType);

        var performedBy = User.FindFirst("sub")?.Value
            ?? User.FindFirst("email")?.Value
            ?? "system";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new UpdateAccountTypeCommand(
            UserId: userId,
            AccountType: request.AccountType,
            PerformedBy: performedBy,
            Reason: request.Reason,
            IpAddress: ipAddress
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.Error?.Contains("not found") == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            message = "AccountType updated successfully",
            previousAccountType = result.PreviousAccountType,
            newAccountType = result.NewAccountType
        });
    }

    /// <summary>
    /// Delete user (deactivate)
    /// </summary>
    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult> DeleteUser(Guid userId)
    {
        _logger.LogInformation("Admin: Deleting user {UserId}", userId);

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Soft delete by deactivating
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new { error = "Error deleting user" });
        }
    }

    private static string MapUserType(AccountType accountType, UserIntent userIntent)
    {
        return accountType switch
        {
            AccountType.Dealer => "dealer",
            AccountType.DealerEmployee => "dealer",
            AccountType.Admin => "admin",
            AccountType.PlatformEmployee => "admin",
            AccountType.Seller => "seller",
            AccountType.Buyer => userIntent switch
            {
                UserIntent.Sell or UserIntent.BuyAndSell => "seller",
                UserIntent.Buy => "buyer",
                _ => "buyer"
            },
            AccountType.Guest => "buyer",
            _ => "buyer"
        };
    }

    private static string MapUserIntent(UserIntent userIntent)
    {
        return userIntent switch
        {
            UserIntent.Browse => "browse",
            UserIntent.Buy => "buy",
            UserIntent.Sell => "sell",
            UserIntent.BuyAndSell => "buy_and_sell",
            _ => "browse"
        };
    }

    /// <summary>
    /// Exclude platform admin accounts from regular user listings
    /// </summary>
    private static readonly AccountType[] ExcludedAccountTypes = new[]
    {
        AccountType.Admin,
        AccountType.PlatformEmployee
    };
}

// DTOs for admin endpoints
public class AdminUserListResponse
{
    public List<AdminUserDto> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string Type { get; set; } = "buyer";
    public string UserIntent { get; set; } = "browse";
    public string Status { get; set; } = "active";
    public bool Verified { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public int VehiclesCount { get; set; }
    public int FavoritesCount { get; set; }
    public int DealsCount { get; set; }
}

public class AdminUserStatsResponse
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Suspended { get; set; }
    public int Pending { get; set; }
    public int NewThisMonth { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class UpdateAccountTypeRequest
{
    public string AccountType { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
