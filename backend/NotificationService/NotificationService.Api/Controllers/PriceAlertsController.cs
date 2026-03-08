using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for price alert management.
/// Allows users to set price drop alerts on vehicles they're interested in.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PriceAlertsController : ControllerBase
{
    private readonly ILogger<PriceAlertsController> _logger;
    private readonly IPriceAlertRepository _repository;

    public PriceAlertsController(
        ILogger<PriceAlertsController> logger,
        IPriceAlertRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Get all price alerts for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        _logger.LogInformation("Getting price alerts for user {UserId}, isActive={IsActive}", userId, isActive);

        var items = await _repository.GetByUserIdAsync(userId, isActive, page, pageSize);
        var totalCount = await _repository.GetCountByUserIdAsync(userId, isActive);

        var response = new
        {
            Items = items.Select(MapToResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific price alert by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var alert = await _repository.GetByIdAndUserAsync(id, userId);
        if (alert == null)
            return NotFound(new { error = "Price alert not found." });

        return Ok(MapToResponse(alert));
    }

    /// <summary>
    /// Create a new price alert
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePriceAlertRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        _logger.LogInformation("Creating price alert for vehicle {VehicleId}, target price {TargetPrice}",
            request.VehicleId, request.TargetPrice);

        var alert = PriceAlert.Create(
            userId: userId,
            vehicleId: request.VehicleId,
            vehicleTitle: request.VehicleTitle ?? string.Empty,
            currentPrice: request.CurrentPrice ?? 0m,
            targetPrice: request.TargetPrice,
            priceDropPercentage: request.PriceDropPercentage,
            notifyByEmail: request.NotifyByEmail ?? true,
            notifyByPush: request.NotifyByPush ?? true,
            notifyBySms: request.NotifyBySms ?? false);

        await _repository.AddAsync(alert);

        return CreatedAtAction(nameof(GetById), new { id = alert.Id }, MapToResponse(alert));
    }

    /// <summary>
    /// Update an existing price alert
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriceAlertRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var alert = await _repository.GetByIdAndUserAsync(id, userId);
        if (alert == null)
            return NotFound(new { error = "Price alert not found." });

        _logger.LogInformation("Updating price alert {AlertId}", id);

        if (request.TargetPrice.HasValue) alert.TargetPrice = request.TargetPrice.Value;
        if (request.PriceDropPercentage.HasValue) alert.PriceDropPercentage = request.PriceDropPercentage.Value;
        if (request.IsActive.HasValue) alert.IsActive = request.IsActive.Value;
        if (request.NotifyByEmail.HasValue) alert.NotifyByEmail = request.NotifyByEmail.Value;
        if (request.NotifyByPush.HasValue) alert.NotifyByPush = request.NotifyByPush.Value;
        if (request.NotifyBySms.HasValue) alert.NotifyBySms = request.NotifyBySms.Value;
        alert.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(alert);

        return Ok(MapToResponse(alert));
    }

    /// <summary>
    /// Delete a price alert
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        if (!await _repository.ExistsAsync(id, userId))
            return NotFound(new { error = "Price alert not found." });

        _logger.LogInformation("Deleting price alert {AlertId}", id);
        await _repository.DeleteAsync(id);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static PriceAlertResponse MapToResponse(PriceAlert alert) => new(
        Id: alert.Id,
        UserId: alert.UserId,
        VehicleId: alert.VehicleId,
        VehicleTitle: alert.VehicleTitle,
        VehicleImageUrl: alert.VehicleImageUrl,
        CurrentPrice: alert.CurrentPrice,
        TargetPrice: alert.TargetPrice,
        PriceDropPercentage: alert.PriceDropPercentage,
        IsActive: alert.IsActive,
        NotifyByEmail: alert.NotifyByEmail,
        NotifyByPush: alert.NotifyByPush,
        NotifyBySms: alert.NotifyBySms,
        TriggeredCount: alert.TriggeredCount,
        LastNotifiedAt: alert.LastNotifiedAt,
        CreatedAt: alert.CreatedAt,
        UpdatedAt: alert.UpdatedAt
    );
}
