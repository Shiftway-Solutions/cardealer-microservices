using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Application.Features.Inventory.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

/// <summary>
/// Controller para métricas de inventario de dealers
/// </summary>
[ApiController]
[Route("api/dealer-analytics/inventory")]
[Authorize]
public class InventoryAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InventoryAnalyticsController> _logger;
    
    public InventoryAnalyticsController(IMediator mediator, ILogger<InventoryAnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener estadísticas del inventario
    /// </summary>
    [HttpGet("{dealerId:guid}/stats")]
    [ProducesResponseType(typeof(InventoryStatsDto), 200)]
    public async Task<ActionResult<InventoryStatsDto>> GetInventoryStats(
        Guid dealerId,
        [FromQuery] DateTime? asOfDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetInventoryStatsQuery(dealerId, asOfDate);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory stats for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving inventory stats" });
        }
    }
    
    /// <summary>
    /// Obtener análisis de antigüedad del inventario
    /// </summary>
    [HttpGet("{dealerId:guid}/aging")]
    [ProducesResponseType(typeof(InventoryAgingDto), 200)]
    public async Task<ActionResult<InventoryAgingDto>> GetInventoryAging(Guid dealerId)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetInventoryAgingQuery(dealerId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory aging for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving inventory aging" });
        }
    }
    
    /// <summary>
    /// Obtener métricas de rotación de inventario
    /// </summary>
    [HttpGet("{dealerId:guid}/turnover")]
    [ProducesResponseType(typeof(InventoryTurnoverDto), 200)]
    public async Task<ActionResult<InventoryTurnoverDto>> GetInventoryTurnover(
        Guid dealerId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var end = toDate ?? DateTime.UtcNow;
            var start = fromDate ?? end.AddDays(-30);
            
            var query = new GetInventoryTurnoverQuery(dealerId, start, end);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory turnover for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving inventory turnover" });
        }
    }
    
    /// <summary>
    /// Obtener performance por vehículo
    /// </summary>
    [HttpGet("{dealerId:guid}/performance")]
    [ProducesResponseType(typeof(List<VehiclePerformanceDto>), 200)]
    public async Task<ActionResult<List<VehiclePerformanceDto>>> GetVehiclePerformance(
        Guid dealerId,
        [FromQuery] int limit = 10,
        [FromQuery] string sortBy = "engagement",
        [FromQuery] bool ascending = false)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetVehiclePerformanceQuery(dealerId, limit, sortBy, ascending);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle performance for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving vehicle performance" });
        }
    }
    
    /// <summary>
    /// Obtener vehículos con bajo rendimiento que necesitan atención
    /// </summary>
    [HttpGet("{dealerId:guid}/low-performers")]
    [ProducesResponseType(typeof(List<VehiclePerformanceDto>), 200)]
    public async Task<ActionResult<List<VehiclePerformanceDto>>> GetLowPerformers(
        Guid dealerId,
        [FromQuery] int limit = 5)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetLowPerformersQuery(dealerId, limit);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low performers for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving low performers" });
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
