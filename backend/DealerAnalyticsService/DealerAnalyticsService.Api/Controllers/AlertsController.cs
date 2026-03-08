using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Application.Features.Alerts.Commands;
using DealerAnalyticsService.Application.Features.Alerts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

/// <summary>
/// Controller para gestión de alertas de dealers
/// </summary>
[ApiController]
[Route("api/dealer-analytics/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AlertsController> _logger;
    
    public AlertsController(IMediator mediator, ILogger<AlertsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener alertas activas del dealer
    /// </summary>
    [HttpGet("{dealerId:guid}")]
    [ProducesResponseType(typeof(AlertsSummaryDto), 200)]
    public async Task<ActionResult<AlertsSummaryDto>> GetActiveAlerts(
        Guid dealerId,
        [FromQuery] bool includeRead = false,
        [FromQuery] bool includeDismissed = false)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetActiveAlertsQuery(dealerId, includeRead, includeDismissed);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving alerts" });
        }
    }
    
    /// <summary>
    /// Obtener conteo de alertas sin leer
    /// </summary>
    [HttpGet("{dealerId:guid}/unread-count")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<ActionResult<int>> GetUnreadCount(Guid dealerId)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetUnreadAlertCountQuery(dealerId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving unread count" });
        }
    }
    
    /// <summary>
    /// Obtener alertas por tipo
    /// </summary>
    [HttpGet("{dealerId:guid}/by-type/{type}")]
    [ProducesResponseType(typeof(List<DealerAlertDto>), 200)]
    public async Task<ActionResult<List<DealerAlertDto>>> GetAlertsByType(
        Guid dealerId,
        string type)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetAlertsByTypeQuery(dealerId, type);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts by type for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving alerts by type" });
        }
    }
    
    /// <summary>
    /// Marcar alerta como leída
    /// </summary>
    [HttpPost("{alertId:guid}/read")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<ActionResult<bool>> MarkAsRead(
        Guid alertId,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId)
    {
        try
        {
            var command = new MarkAlertAsReadCommand(alertId, dealerId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking alert {AlertId} as read", alertId);
            return StatusCode(500, new { Message = "Error marking alert as read" });
        }
    }
    
    /// <summary>
    /// Marcar todas las alertas como leídas
    /// </summary>
    [HttpPost("{dealerId:guid}/read-all")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<ActionResult<int>> MarkAllAsRead(Guid dealerId)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var command = new MarkAllAlertsAsReadCommand(dealerId);
            var result = await _mediator.Send(command);
            return Ok(new { MarkedCount = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all alerts as read for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error marking alerts as read" });
        }
    }
    
    /// <summary>
    /// Descartar una alerta
    /// </summary>
    [HttpPost("{alertId:guid}/dismiss")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<ActionResult<bool>> DismissAlert(
        Guid alertId,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId)
    {
        try
        {
            var command = new DismissAlertCommand(alertId, dealerId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert {AlertId}", alertId);
            return StatusCode(500, new { Message = "Error dismissing alert" });
        }
    }
    
    /// <summary>
    /// Marcar alerta como actuada
    /// </summary>
    [HttpPost("{alertId:guid}/acted-upon")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<ActionResult<bool>> MarkAsActedUpon(
        Guid alertId,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId)
    {
        try
        {
            var command = new MarkAlertAsActedUponCommand(alertId, dealerId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking alert {AlertId} as acted upon", alertId);
            return StatusCode(500, new { Message = "Error marking alert as acted upon" });
        }
    }
    
    /// <summary>
    /// Crear una alerta (usado por admin o sistema)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    public async Task<ActionResult<Guid>> CreateAlert([FromBody] CreateAlertRequest request)
    {
        try
        {
            var command = new CreateAlertCommand(
                request.DealerId,
                request.Type,
                request.Severity,
                request.Title,
                request.Message,
                request.ActionUrl,
                request.ActionLabel,
                request.ExpiresInDays
            );
            
            var result = await _mediator.Send(command);
            return Created($"/api/dealer-analytics/alerts/{result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert for dealer {DealerId}", request.DealerId);
            return StatusCode(500, new { Message = "Error creating alert" });
        }
    }

    private bool IsAuthorizedForDealer(Guid dealerId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        if (role is "Admin" or "SuperAdmin")
            return true;

        var claimDealerId = User.FindFirst("dealerId")?.Value;
        return Guid.TryParse(claimDealerId, out var id) && id == dealerId;
    }
}

public class CreateAlertRequest
{
    public Guid DealerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
    public int? ExpiresInDays { get; set; }
}
