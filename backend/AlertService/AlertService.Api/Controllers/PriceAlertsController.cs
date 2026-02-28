using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AlertService.Domain.Entities;
using AlertService.Domain.Interfaces;
using System.Security.Claims;

namespace AlertService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PriceAlertsController : ControllerBase
{
    private readonly IPriceAlertRepository _repository;
    private readonly ILogger<PriceAlertsController> _logger;

    public PriceAlertsController(
        IPriceAlertRepository repository,
        ILogger<PriceAlertsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las alertas de precio del usuario (paginated format)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetMyAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("GetMyAlerts called for UserId: {UserId}", userId);
        var alerts = await _repository.GetByUserIdAsync(userId);
        _logger.LogInformation("Found {Count} alerts for UserId: {UserId}", alerts.Count, userId);

        var items = alerts.Select(MapToDto).ToList();
        var totalItems = items.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            items = paged,
            pagination = new
            {
                page,
                pageSize,
                totalItems,
                totalPages
            }
        });
    }

    /// <summary>
    /// Obtiene estadísticas de alertas del usuario
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var userId = GetCurrentUserId();
        var alerts = await _repository.GetByUserIdAsync(userId);

        var activePriceAlerts = alerts.Count(a => a.IsActive && !a.IsTriggered);
        var priceDropsThisMonth = alerts.Count(a => a.IsTriggered && a.TriggeredAt.HasValue &&
            a.TriggeredAt.Value >= DateTime.UtcNow.AddDays(-30));

        return Ok(new
        {
            totalPriceAlerts = alerts.Count,
            activePriceAlerts,
            priceDropsThisMonth,
            totalSavedSearches = 0,
            activeSavedSearches = 0,
            newMatchesThisWeek = 0
        });
    }

    /// <summary>
    /// Obtiene la alerta de precio para un vehículo específico
    /// </summary>
    [HttpGet("vehicle/{vehicleId:guid}")]
    public async Task<ActionResult<PriceAlertDto>> GetByVehicleId(Guid vehicleId)
    {
        var userId = GetCurrentUserId();
        var alerts = await _repository.GetActiveAlertsByVehicleIdAsync(vehicleId);
        var alert = alerts.FirstOrDefault(a => a.UserId == userId);

        if (alert == null)
            return NotFound();

        return Ok(MapToDto(alert));
    }

    /// <summary>
    /// Endpoint interno para procesar cambio de precio (llamado por VehiclesSaleService)
    /// </summary>
    [HttpPost("check-price")]
    [AllowAnonymous]
    public async Task<ActionResult> CheckPriceChange([FromBody] PriceChangeNotification notification)
    {
        _logger.LogInformation(
            "Price change notification received for vehicle {VehicleId}: {OldPrice} → {NewPrice}",
            notification.VehicleId, notification.OldPrice, notification.NewPrice);

        var alerts = await _repository.GetActiveAlertsByVehicleIdAsync(notification.VehicleId);

        if (alerts.Count == 0)
        {
            _logger.LogInformation("No active alerts for vehicle {VehicleId}", notification.VehicleId);
            return Ok(new { triggered = 0 });
        }

        var triggered = 0;
        foreach (var alert in alerts)
        {
            if (alert.ShouldTrigger(notification.NewPrice))
            {
                alert.Trigger();
                await _repository.UpdateAsync(alert);
                triggered++;

                _logger.LogInformation(
                    "Price alert {AlertId} triggered for user {UserId}, vehicle {VehicleId}. " +
                    "Target: {TargetPrice}, New price: {NewPrice}",
                    alert.Id, alert.UserId, alert.VehicleId, alert.TargetPrice, notification.NewPrice);
            }
        }

        _logger.LogInformation(
            "Processed {Total} alerts for vehicle {VehicleId}, triggered {Triggered}",
            alerts.Count, notification.VehicleId, triggered);

        return Ok(new { triggered, total = alerts.Count });
    }

    /// <summary>
    /// Obtiene una alerta específica
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PriceAlertDto>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var alert = await _repository.GetByIdAsync(id);

        if (alert == null)
            return NotFound();

        if (alert.UserId != userId)
            return Forbid();

        return Ok(MapToDto(alert));
    }

    /// <summary>
    /// Crea una nueva alerta de precio
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PriceAlertDto>> Create([FromBody] CreatePriceAlertRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if alert already exists
            if (await _repository.ExistsAsync(userId, request.VehicleId))
            {
                return BadRequest(new { error = "Ya existe una alerta para este vehículo" });
            }

            var alert = new PriceAlert(
                userId,
                request.VehicleId,
                request.TargetPrice,
                request.Condition);

            await _repository.CreateAsync(alert);

            _logger.LogInformation(
                "User {UserId} created price alert {AlertId} for vehicle {VehicleId}",
                userId, alert.Id, request.VehicleId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = alert.Id },
                MapToDto(alert));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza el precio objetivo de una alerta
    /// </summary>
    [HttpPut("{id:guid}/target-price")]
    public async Task<ActionResult<PriceAlertDto>> UpdateTargetPrice(
        Guid id,
        [FromBody] UpdateTargetPriceRequest request)
    {
        var userId = GetCurrentUserId();
        var alert = await _repository.GetByIdAsync(id);

        if (alert == null)
            return NotFound();

        if (alert.UserId != userId)
            return Forbid();

        try
        {
            alert.UpdateTargetPrice(request.TargetPrice);
            await _repository.UpdateAsync(alert);

            return Ok(MapToDto(alert));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Activa una alerta desactivada
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<PriceAlertDto>> Activate(Guid id)
    {
        var userId = GetCurrentUserId();
        var alert = await _repository.GetByIdAsync(id);

        if (alert == null)
            return NotFound();

        if (alert.UserId != userId)
            return Forbid();

        alert.Activate();
        await _repository.UpdateAsync(alert);

        return Ok(MapToDto(alert));
    }

    /// <summary>
    /// Desactiva una alerta
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<PriceAlertDto>> Deactivate(Guid id)
    {
        var userId = GetCurrentUserId();
        var alert = await _repository.GetByIdAsync(id);

        if (alert == null)
            return NotFound();

        if (alert.UserId != userId)
            return Forbid();

        alert.Deactivate();
        await _repository.UpdateAsync(alert);

        return Ok(MapToDto(alert));
    }

    /// <summary>
    /// Resetea una alerta disparada
    /// </summary>
    [HttpPost("{id:guid}/reset")]
    public async Task<ActionResult<PriceAlertDto>> Reset(Guid id)
    {
        var userId = GetCurrentUserId();
        var alert = await _repository.GetByIdAsync(id);

        if (alert == null)
            return NotFound();

        if (alert.UserId != userId)
            return Forbid();

        alert.Reset();
        await _repository.UpdateAsync(alert);

        return Ok(MapToDto(alert));
    }

    /// <summary>
    /// Elimina una alerta
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var alert = await _repository.GetByIdAsync(id);

        if (alert == null)
            return NotFound();

        if (alert.UserId != userId)
            return Forbid();

        await _repository.DeleteAsync(id);

        return NoContent();
    }

    // Helper methods
    private Guid GetCurrentUserId()
    {
        // Log all claims for debugging
        foreach (var claim in User.Claims)
        {
            _logger.LogInformation("JWT Claim: Type={ClaimType}, Value={ClaimValue}", claim.Type, claim.Value);
        }
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        _logger.LogInformation("Extracted userIdClaim: {UserIdClaim}", userIdClaim);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Failed to parse userId from claim: {UserIdClaim}", userIdClaim);
            throw new UnauthorizedAccessException("User not authenticated");
        }

        _logger.LogInformation("Parsed UserId: {UserId}", userId);
        return userId;
    }

    private static PriceAlertDto MapToDto(PriceAlert alert)
    {
        return new PriceAlertDto
        {
            Id = alert.Id,
            VehicleId = alert.VehicleId,
            VehicleTitle = $"Vehículo {alert.VehicleId.ToString()[..8]}",
            VehicleSlug = null,
            TargetPrice = alert.TargetPrice,
            CurrentPrice = alert.TargetPrice,
            Currency = "DOP",
            Condition = alert.Condition.ToString(),
            IsActive = alert.IsActive,
            IsTriggered = alert.IsTriggered,
            NotifyOnAnyChange = false,
            TriggeredAt = alert.TriggeredAt,
            LastCheckedAt = alert.UpdatedAt,
            LastNotifiedAt = alert.TriggeredAt,
            CreatedAt = alert.CreatedAt,
            UpdatedAt = alert.UpdatedAt
        };
    }
}

#region DTOs

public record CreatePriceAlertRequest
{
    public Guid VehicleId { get; init; }
    public decimal TargetPrice { get; init; }
    public AlertCondition Condition { get; init; }
}

public record UpdateTargetPriceRequest
{
    public decimal TargetPrice { get; init; }
}

public record PriceAlertDto
{
    public Guid Id { get; init; }
    public Guid VehicleId { get; init; }
    public string VehicleTitle { get; init; } = string.Empty;
    public string? VehicleSlug { get; init; }
    public decimal TargetPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public string Currency { get; init; } = "DOP";
    public string Condition { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsTriggered { get; init; }
    public bool NotifyOnAnyChange { get; init; }
    public DateTime? TriggeredAt { get; init; }
    public DateTime? LastCheckedAt { get; init; }
    public DateTime? LastNotifiedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record PriceChangeNotification
{
    public Guid VehicleId { get; init; }
    public string VehicleTitle { get; init; } = string.Empty;
    public string? VehicleSlug { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; } = "DOP";
    public Guid UpdatedBy { get; init; }
}

#endregion
