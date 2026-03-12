using Microsoft.AspNetCore.Mvc;
using MediatR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.UseCases.Users.CreateUser;
using UserService.Application.UseCases.Users.UpdateUser;
using UserService.Application.UseCases.Users.DeleteUser;
using UserService.Application.UseCases.Users.GetUser;
using UserService.Application.UseCases.Users.GetUsers;
using UserService.Application.UseCases.Users.GetOrCreateUser;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UsersController> _logger;
        private readonly IVehiclesSaleServiceClient _vehiclesClient;

        public UsersController(
            IMediator mediator,
            ILogger<UsersController> logger,
            IVehiclesSaleServiceClient vehiclesClient)
        {
            _mediator = mediator;
            _logger = logger;
            _vehiclesClient = vehiclesClient;
        }

        /// <summary>
        /// Obtener perfil del usuario actual (autenticado).
        /// Si el usuario no existe en UserService (OAuth users), se crea automáticamente.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            // Extract user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated or invalid user ID" });
            }

            // Extract additional user info from JWT claims for OAuth sync
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? string.Empty;
            var firstName = User.FindFirst(ClaimTypes.GivenName)?.Value
                ?? User.FindFirst("given_name")?.Value
                ?? User.FindFirst("firstName")?.Value
                ?? string.Empty;
            var lastName = User.FindFirst(ClaimTypes.Surname)?.Value
                ?? User.FindFirst("family_name")?.Value
                ?? User.FindFirst("lastName")?.Value
                ?? string.Empty;
            var avatarUrl = User.FindFirst("picture")?.Value
                ?? User.FindFirst("avatar")?.Value
                ?? User.FindFirst("avatarUrl")?.Value;

            try
            {
                // Use GetOrCreateUser command to handle OAuth user sync automatically
                var command = new GetOrCreateUserCommand(
                    userId,
                    email,
                    firstName,
                    lastName,
                    avatarUrl
                );
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating user profile for {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving user profile" });
            }
        }

        /// <summary>
        /// Actualizar perfil del usuario actual (autenticado)
        /// </summary>
        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateCurrentUserProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated or invalid user ID" });
            }

            // Extract email from JWT to find the real user (handles OAuth ID mismatch)
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? string.Empty;
            var firstName = User.FindFirst(ClaimTypes.GivenName)?.Value
                ?? User.FindFirst("given_name")?.Value
                ?? User.FindFirst("firstName")?.Value
                ?? string.Empty;
            var lastName = User.FindFirst(ClaimTypes.Surname)?.Value
                ?? User.FindFirst("family_name")?.Value
                ?? User.FindFirst("lastName")?.Value
                ?? string.Empty;
            var avatarUrl = User.FindFirst("picture")?.Value
                ?? User.FindFirst("avatar")?.Value
                ?? User.FindFirst("avatarUrl")?.Value;

            try
            {
                // First, get or create user to ensure we have the correct user ID from DB
                var getOrCreateCommand = new GetOrCreateUserCommand(
                    userId,
                    email,
                    firstName,
                    lastName,
                    avatarUrl
                );
                var existingUser = await _mediator.Send(getOrCreateCommand);

                // Now update using the actual user ID from the database
                var command = new UserService.Application.UseCases.Users.UpdateProfile.UpdateProfileCommand(
                    existingUser.Id, // Use the real ID from DB, not the token ID
                    request.FirstName,
                    request.LastName,
                    request.Phone,
                    request.Bio,
                    request.Location,
                    request.City,
                    request.Province,
                    request.PreferredLocale,
                    request.PreferredCurrency
                );
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for {UserId}", userId);
                return StatusCode(500, new { message = "Error updating user profile" });
            }
        }

        /// <summary>
        /// Obtener estadísticas del usuario actual (autenticado)
        /// </summary>
        [HttpGet("me/stats")]
        [Authorize]
        public async Task<ActionResult<UserStatsResponse>> GetCurrentUserStats()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated or invalid user ID" });
            }

            try
            {
                var listingStats = await _vehiclesClient.GetSellerListingStatsAsync(userId);

                return Ok(new UserStatsResponse
                {
                    VehiclesPublished = listingStats?.TotalListings ?? 0,
                    VehiclesSold = listingStats?.SoldListings ?? 0,
                    TotalViews = listingStats?.TotalViews ?? 0,
                    TotalInquiries = 0, // TODO: Integrate with ContactService
                    ResponseRate = 0,
                    AverageResponseTime = "N/A",
                    ReviewCount = 0, // TODO: Integrate with ReviewService
                    AverageRating = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for user {UserId}", userId);
                // Return zeros instead of error to avoid breaking the UI
                return Ok(new UserStatsResponse
                {
                    VehiclesPublished = 0,
                    VehiclesSold = 0,
                    TotalViews = 0,
                    TotalInquiries = 0,
                    ResponseRate = 0,
                    AverageResponseTime = "N/A",
                    ReviewCount = 0,
                    AverageRating = 0
                });
            }
        }

        /// <summary>
        /// Obtener vehículos del usuario actual (autenticado)
        /// </summary>
        [HttpGet("me/vehicles")]
        [Authorize]
        public async Task<ActionResult<UserVehiclesResponse>> GetCurrentUserVehicles(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 12,
            [FromQuery] string? status = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated or invalid user ID" });
            }

            try
            {
                _logger.LogInformation("Getting vehicles for user {UserId}, page={Page}, limit={Limit}, status={Status}",
                    userId, page, limit, status);

                var result = await _vehiclesClient.GetSellerListingsAsync(userId, page, limit, status);

                return Ok(new UserVehiclesResponse
                {
                    Vehicles = result.Listings.Select(l => new UserVehicleDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Slug = l.Slug,
                        Price = l.Price,
                        Currency = l.Currency,
                        Status = l.Status,
                        ImageUrl = l.MainImageUrl,
                        Year = l.Year,
                        Make = l.Make,
                        Model = l.Model,
                        Mileage = l.Mileage,
                        Transmission = l.Transmission,
                        FuelType = l.FuelType,
                        ViewCount = l.Views,
                        InquiryCount = l.Favorites,
                        IsFeatured = false,
                        CreatedAt = l.CreatedAt,
                        ExpiresAt = null
                    }).ToList(),
                    Total = result.TotalCount,
                    Page = result.Page,
                    TotalPages = result.TotalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles for user {UserId}", userId);
                // Return empty list instead of error to avoid breaking the UI
                return Ok(new UserVehiclesResponse
                {
                    Vehicles = new List<UserVehicleDto>(),
                    Total = 0,
                    Page = 1,
                    TotalPages = 0
                });
            }
        }

        /// <summary>
        /// Listar usuarios con paginación
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedUsersResponse>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetUsersQuery(page, pageSize);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Obtener usuario por ID
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid userId)
        {
            var query = new GetUserQuery(userId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Crear nuevo usuario
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var command = new CreateUserCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.PhoneNumber
            );

            var userId = await _mediator.Send(command);

            return CreatedAtAction(
                nameof(GetUser),
                new { userId },
                new { id = userId, message = "User created successfully" }
            );
        }

        /// <summary>
        /// Actualizar usuario
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<ActionResult> UpdateUser(
            Guid userId,
            [FromBody] UpdateUserRequest request)
        {
            var command = new UpdateUserCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.IsActive
            );

            await _mediator.Send(command);

            return NoContent();
        }

        /// <summary>
        /// Eliminar usuario
        /// </summary>
        [HttpDelete("{userId}")]
        public async Task<ActionResult> DeleteUser(Guid userId)
        {
            var command = new DeleteUserCommand(userId);
            await _mediator.Send(command);
            return NoContent();
        }
    }
}
