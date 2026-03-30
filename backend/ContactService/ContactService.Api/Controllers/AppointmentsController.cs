using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ContactService.Api.Controllers;

/// <summary>
/// Controller for appointment scheduling and management (consolidated from AppointmentService).
/// TODO: Implement full CQRS handlers with MediatR when AppointmentService logic is migrated.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(ILogger<AppointmentsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all appointments for the current user
    /// </summary>
    /// <param name="status">Filter by status (pending, confirmed, completed, cancelled)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <returns>Paginated list of appointments</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting appointments for user {UserId}, status={Status}, page={Page}", userId, status, page);

        // TODO: Replace with MediatR query when AppointmentService logic is migrated
        var stubData = new
        {
            Items = new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    VehicleId = Guid.NewGuid(),
                    VehicleTitle = "2024 Toyota Corolla",
                    BuyerId = userId,
                    SellerId = Guid.NewGuid(),
                    SellerName = "Auto Premium RD",
                    ScheduledDate = DateTime.UtcNow.AddDays(3),
                    Duration = TimeSpan.FromMinutes(30),
                    Status = status ?? "pending",
                    Type = "test_drive",
                    Location = "Santo Domingo, DN",
                    Notes = "Customer interested in test drive",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            },
            TotalCount = 1,
            Page = page,
            PageSize = pageSize,
            TotalPages = 1
        };

        return Ok(stubData);
    }

    /// <summary>
    /// Get a specific appointment by ID
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <returns>Appointment details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetById(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting appointment {AppointmentId} for user {UserId}", id, currentUserId);

        // TODO: Replace with MediatR query when AppointmentService logic is migrated
        // NOTE: When implemented, MUST verify the appointment belongs to the current user
        // (either as buyer or seller) before returning. Example:
        //   var appointment = await repo.GetByIdAsync(id);
        //   if (appointment.BuyerId != currentUserId && appointment.SellerId != currentUserId)
        //       return Forbid();

        var stubBuyerId = currentUserId; // Stub uses current user as buyer for demo
        var stubSellerId = Guid.NewGuid();

        var stubData = new
        {
            Id = id,
            VehicleId = Guid.NewGuid(),
            VehicleTitle = "2024 Toyota Corolla",
            BuyerId = stubBuyerId,
            BuyerName = "John Doe",
            BuyerEmail = "john@example.com",
            BuyerPhone = "809-555-0100",
            SellerId = stubSellerId,
            SellerName = "Auto Premium RD",
            SellerPhone = "809-555-0200",
            ScheduledDate = DateTime.UtcNow.AddDays(3),
            Duration = TimeSpan.FromMinutes(30),
            Status = "pending",
            Type = "test_drive",
            Location = "Av. Abraham Lincoln 1001, Santo Domingo",
            Notes = "Customer interested in test drive",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(stubData);
    }

    /// <summary>
    /// Create a new appointment
    /// </summary>
    /// <param name="request">Appointment creation data</param>
    /// <returns>Created appointment</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateAppointmentRequest request)
    {
        _logger.LogInformation("Creating appointment for vehicle {VehicleId}", request.VehicleId);

        if (request.VehicleId == Guid.Empty || request.SellerId == Guid.Empty)
            return BadRequest(new { error = "VehicleId and SellerId are required." });

        if (request.ScheduledDate <= DateTime.UtcNow)
            return BadRequest(new { error = "Scheduled date must be in the future." });

        // Security validation - check for XSS/SQL injection patterns
        var textFields = new[] { request.Type ?? "", request.Location ?? "", request.Notes ?? "" };
        foreach (var field in textFields)
        {
            if (ContainsDangerousPatterns(field))
                return BadRequest(new { error = "Input contains potentially dangerous content." });
        }

        if (request.Location != null && request.Location.Length > 500)
            return BadRequest(new { error = "Location must be 500 characters or less." });
        if (request.Notes != null && request.Notes.Length > 2000)
            return BadRequest(new { error = "Notes must be 2000 characters or less." });

        // TODO: Replace with MediatR command when AppointmentService logic is migrated
        var stubData = new
        {
            Id = Guid.NewGuid(),
            request.VehicleId,
            BuyerId = GetCurrentUserId(),
            request.SellerId,
            request.ScheduledDate,
            Duration = TimeSpan.FromMinutes(request.DurationMinutes ?? 30),
            Status = "pending",
            request.Type,
            request.Location,
            request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetById), new { id = stubData.Id }, stubData);
    }

    /// <summary>
    /// Update an existing appointment (reschedule, change status, etc.)
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Updated appointment data</param>
    /// <returns>Updated appointment</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Update(Guid id, [FromBody] UpdateAppointmentRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Updating appointment {AppointmentId} by user {UserId}", id, currentUserId);

        // Input validation for string fields
        var textFields = new[] { request.Status ?? "", request.Location ?? "", request.Notes ?? "" };
        foreach (var field in textFields)
        {
            if (ContainsDangerousPatterns(field))
                return BadRequest(new { error = "Input contains potentially dangerous content." });
        }

        if (request.Location != null && request.Location.Length > 500)
            return BadRequest(new { error = "Location must be 500 characters or less." });
        if (request.Notes != null && request.Notes.Length > 2000)
            return BadRequest(new { error = "Notes must be 2000 characters or less." });

        // TODO: Replace with MediatR command when AppointmentService logic is migrated
        // NOTE: When implemented, MUST verify the appointment belongs to the current user
        // (either as buyer or seller) before allowing update. Example:
        //   var appointment = await repo.GetByIdAsync(id);
        //   if (appointment.BuyerId != currentUserId && appointment.SellerId != currentUserId)
        //       return Forbid();

        var stubData = new
        {
            Id = id,
            request.ScheduledDate,
            Duration = request.DurationMinutes.HasValue ? TimeSpan.FromMinutes(request.DurationMinutes.Value) : TimeSpan.FromMinutes(30),
            request.Status,
            request.Location,
            request.Notes,
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(stubData);
    }

    /// <summary>
    /// Cancel/delete an appointment
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Delete(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Cancelling appointment {AppointmentId} by user {UserId}", id, currentUserId);

        // TODO: Replace with MediatR command when AppointmentService logic is migrated
        // NOTE: When implemented, MUST verify the appointment belongs to the current user
        // (either as buyer or seller) before allowing deletion. Example:
        //   var appointment = await repo.GetByIdAsync(id);
        //   if (appointment.BuyerId != currentUserId && appointment.SellerId != currentUserId)
        //       return Forbid();

        return NoContent();
    }

    // ========================================
    // TIMESLOTS ENDPOINT
    // ========================================

    /// <summary>
    /// Get available time slots for a seller on a specific date
    /// </summary>
    /// <param name="sellerId">Seller ID</param>
    /// <param name="date">Date to check availability</param>
    /// <returns>List of available time slots</returns>
    [HttpGet("/api/timeslots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetTimeSlots(
        [FromQuery] Guid sellerId,
        [FromQuery] DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow.Date.AddDays(1);

        _logger.LogInformation("Getting time slots for seller {SellerId} on {Date}", sellerId, targetDate);

        if (sellerId == Guid.Empty)
            return BadRequest(new { error = "SellerId is required." });

        // TODO: Replace with MediatR query when AppointmentService logic is migrated
        var stubData = new
        {
            SellerId = sellerId,
            Date = targetDate,
            TimeSlots = new[]
            {
                new { StartTime = "09:00", EndTime = "09:30", IsAvailable = true },
                new { StartTime = "09:30", EndTime = "10:00", IsAvailable = true },
                new { StartTime = "10:00", EndTime = "10:30", IsAvailable = false },
                new { StartTime = "10:30", EndTime = "11:00", IsAvailable = true },
                new { StartTime = "11:00", EndTime = "11:30", IsAvailable = true },
                new { StartTime = "11:30", EndTime = "12:00", IsAvailable = true },
                new { StartTime = "14:00", EndTime = "14:30", IsAvailable = true },
                new { StartTime = "14:30", EndTime = "15:00", IsAvailable = false },
                new { StartTime = "15:00", EndTime = "15:30", IsAvailable = true },
                new { StartTime = "15:30", EndTime = "16:00", IsAvailable = true },
                new { StartTime = "16:00", EndTime = "16:30", IsAvailable = true },
                new { StartTime = "16:30", EndTime = "17:00", IsAvailable = true }
            }
        };

        return Ok(stubData);
    }

    // ========================================
    // DEALER APPOINTMENTS ENDPOINT (BUG#3 fix)
    // ========================================

    /// <summary>
    /// Get all appointments for a specific dealer
    /// </summary>
    /// <param name="dealerId">Dealer ID</param>
    /// <param name="status">Filter by status (pending, confirmed, completed, cancelled)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <returns>Paginated list of dealer appointments</returns>
    [HttpGet("dealer/{dealerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetByDealer(
        Guid dealerId,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting appointments for dealer {DealerId} by user {UserId}", dealerId, currentUserId);

        // TODO: When implementing with MediatR, verify the current user owns this dealer profile
        // TODO: Query appointments where SellerId = dealerId and optionally filter by status

        // Stub: return empty appointments list (dealer has no appointments yet)
        var stubData = new
        {
            Items = Array.Empty<object>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize,
            TotalPages = 0
        };

        return Ok(stubData);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID claim not found"));
    }

    private static bool ContainsDangerousPatterns(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var lower = input.ToLowerInvariant();
        var upper = input.ToUpperInvariant();

        // XSS patterns
        string[] xssPatterns = { "<script", "javascript:", "onerror=", "onload=", "onclick=",
            "<iframe", "eval(", "expression(", "vbscript:", "data:text/html" };
        if (xssPatterns.Any(p => lower.Contains(p))) return true;

        // SQL injection patterns
        string[] sqlPatterns = { "DROP ", "DELETE ", "INSERT ", "UPDATE ", "--", "/*", "*/",
            "EXEC ", "UNION ", "xp_", "sp_", "OR 1=1", "OR '1'='1'" };
        if (sqlPatterns.Any(p => upper.Contains(p))) return true;

        return false;
    }
}

/// <summary>
/// Request to create an appointment
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record CreateAppointmentRequest(
    Guid VehicleId,
    Guid SellerId,
    DateTime ScheduledDate,
    string Type = "test_drive",
    int? DurationMinutes = 30,
    string? Location = null,
    string? Notes = null
);

/// <summary>
/// Request to update an appointment
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record UpdateAppointmentRequest(
    DateTime? ScheduledDate = null,
    string? Status = null,
    int? DurationMinutes = null,
    string? Location = null,
    string? Notes = null
);
