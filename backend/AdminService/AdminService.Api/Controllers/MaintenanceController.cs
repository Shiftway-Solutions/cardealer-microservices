using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for platform maintenance mode management.
/// Provides full CRUD for maintenance windows, status checks, and immediate toggle.
/// In-memory storage — state is lost on pod restart (acceptable for maintenance windows).
/// </summary>
[ApiController]
[Route("api/maintenance")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class MaintenanceController : ControllerBase
{
    private readonly ILogger<MaintenanceController> _logger;

    // In-memory storage for maintenance windows
    private static readonly List<MaintenanceWindowDto> _windows = new();
    private static readonly object _lock = new();

    public MaintenanceController(ILogger<MaintenanceController> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // PUBLIC ENDPOINTS (no auth required)
    // =========================================================================

    /// <summary>
    /// Get current maintenance mode status (public — called by middleware on every request)
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MaintenanceStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        lock (_lock)
        {
            var active = _windows.FirstOrDefault(w => w.Status == "InProgress");
            var response = new MaintenanceStatusResponse
            {
                IsMaintenanceMode = active != null,
                MaintenanceWindow = active
            };

            return Ok(response);
        }
    }

    /// <summary>
    /// Get upcoming maintenance windows (public)
    /// </summary>
    [HttpGet("upcoming")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MaintenanceWindowDto>), StatusCodes.Status200OK)]
    public IActionResult GetUpcoming([FromQuery] int days = 7)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddDays(days);
            var upcoming = _windows
                .Where(w => w.Status == "Scheduled" && w.ScheduledStart <= cutoff)
                .OrderBy(w => w.ScheduledStart)
                .ToList();
            return Ok(upcoming);
        }
    }

    // =========================================================================
    // ADMIN ENDPOINTS (auth required)
    // =========================================================================

    /// <summary>
    /// Get all maintenance windows
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MaintenanceWindowDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        lock (_lock)
        {
            return Ok(_windows.OrderByDescending(w => w.CreatedAt).ToList());
        }
    }

    /// <summary>
    /// Get a specific maintenance window by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });
            return Ok(window);
        }
    }

    /// <summary>
    /// Create a new maintenance window
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateMaintenanceWindowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        var window = new MaintenanceWindowDto
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Title = request.Title,
            Description = request.Description ?? "",
            Type = MapType(request.Type),
            Status = "Scheduled",
            ScheduledStart = request.ScheduledStart,
            ScheduledEnd = request.ScheduledEnd,
            ActualStart = null,
            ActualEnd = null,
            CreatedBy = User.Identity?.Name ?? "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            Notes = null,
            NotifyUsers = request.NotifyUsers,
            NotifyMinutesBefore = request.NotifyMinutesBefore,
            AffectedServices = request.AffectedServices ?? new List<string> { "all" },
            IsActive = false,
            IsUpcoming = request.ScheduledStart > DateTime.UtcNow
        };

        lock (_lock)
        {
            _windows.Add(window);
        }

        _logger.LogInformation("Maintenance window created: {Id} - {Title}", window.Id, window.Title);
        return CreatedAtAction(nameof(GetById), new { id = window.Id }, window);
    }

    /// <summary>
    /// Start a maintenance window (activates maintenance mode)
    /// </summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Start(string id)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });

            // Check if another maintenance is already in progress
            var existing = _windows.FirstOrDefault(w => w.Status == "InProgress" && w.Id != id);
            if (existing != null)
                return Conflict(new { message = $"Another maintenance is already in progress: {existing.Title}" });

            window.Status = "InProgress";
            window.ActualStart = DateTime.UtcNow;
            window.IsActive = true;
            window.IsUpcoming = false;
            window.UpdatedAt = DateTime.UtcNow;

            _logger.LogWarning("MAINTENANCE MODE ACTIVATED: {Id} - {Title}", window.Id, window.Title);
            return Ok(window);
        }
    }

    /// <summary>
    /// Complete a maintenance window (deactivates maintenance mode)
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Complete(string id)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });

            window.Status = "Completed";
            window.ActualEnd = DateTime.UtcNow;
            window.IsActive = false;
            window.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("MAINTENANCE MODE DEACTIVATED: {Id} - {Title}", window.Id, window.Title);
            return Ok(window);
        }
    }

    /// <summary>
    /// Cancel a maintenance window
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Cancel(string id, [FromBody] CancelMaintenanceRequest request)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });

            window.Status = "Cancelled";
            window.IsActive = false;
            window.Notes = (window.Notes ?? "") + $"\nCancelled: {request.Reason}";
            window.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Maintenance cancelled: {Id} - Reason: {Reason}", window.Id, request.Reason);
            return Ok(window);
        }
    }

    /// <summary>
    /// Update maintenance window schedule
    /// </summary>
    [HttpPut("{id}/schedule")]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateSchedule(string id, [FromBody] UpdateScheduleRequest request)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });

            window.ScheduledStart = request.NewStart;
            window.ScheduledEnd = request.NewEnd;
            window.UpdatedAt = DateTime.UtcNow;

            return Ok(window);
        }
    }

    /// <summary>
    /// Update maintenance window notes
    /// </summary>
    [HttpPut("{id}/notes")]
    [ProducesResponseType(typeof(MaintenanceWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateNotes(string id, [FromBody] UpdateNotesRequest request)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });

            window.Notes = request.Notes;
            window.UpdatedAt = DateTime.UtcNow;

            return Ok(window);
        }
    }

    /// <summary>
    /// Delete a maintenance window
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        lock (_lock)
        {
            var window = _windows.FirstOrDefault(w => w.Id == id);
            if (window == null) return NotFound(new { message = "Maintenance window not found" });

            if (window.Status == "InProgress")
                return BadRequest(new { message = "Cannot delete an active maintenance window. Complete or cancel it first." });

            _windows.Remove(window);
            _logger.LogInformation("Maintenance window deleted: {Id}", id);
            return NoContent();
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static string MapType(int type) => type switch
    {
        1 => "Scheduled",
        2 => "Emergency",
        3 => "Database",
        4 => "Deployment",
        5 => "Infrastructure",
        _ => "Other"
    };
}

// =============================================================================
// DTOs — Response models matching frontend expectations
// =============================================================================

public class MaintenanceStatusResponse
{
    public bool IsMaintenanceMode { get; set; }
    public MaintenanceWindowDto? MaintenanceWindow { get; set; }
}

public class MaintenanceWindowDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "Scheduled";
    public string Status { get; set; } = "Scheduled";
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }
    public bool NotifyUsers { get; set; }
    public int NotifyMinutesBefore { get; set; }
    public List<string> AffectedServices { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsUpcoming { get; set; }
}

public record CreateMaintenanceWindowRequest(
    string Title,
    string? Description,
    int Type,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    bool NotifyUsers = true,
    int NotifyMinutesBefore = 30,
    List<string>? AffectedServices = null
);

public record CancelMaintenanceRequest(string Reason);

public record UpdateScheduleRequest(DateTime NewStart, DateTime NewEnd);

public record UpdateNotesRequest(string Notes);
