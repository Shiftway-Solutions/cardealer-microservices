using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Application.Features.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

/// <summary>
/// Controller principal para el overview de analytics
/// </summary>
[ApiController]
[Route("api/dealer-analytics")]
[Authorize]
public class OverviewController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OverviewController> _logger;
    
    public OverviewController(IMediator mediator, ILogger<OverviewController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener overview completo del dashboard
    /// ANAL-001: Dashboard Overview
    /// </summary>
    [HttpGet("{dealerId:guid}/overview")]
    [ProducesResponseType(typeof(AnalyticsOverviewDto), 200)]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview(
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
            
            var query = new GetAnalyticsOverviewQuery(dealerId, start, end);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overview for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving overview" });
        }
    }
    
    /// <summary>
    /// Obtener KPIs principales
    /// </summary>
    [HttpGet("{dealerId:guid}/kpis")]
    [ProducesResponseType(typeof(KpiSummaryDto), 200)]
    public async Task<ActionResult<KpiSummaryDto>> GetKpis(
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
            
            var query = new GetKpisQuery(dealerId, start, end);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KPIs for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving KPIs" });
        }
    }
    
    /// <summary>
    /// Obtener snapshot actual
    /// </summary>
    [HttpGet("{dealerId:guid}/snapshot")]
    [ProducesResponseType(typeof(DealerSnapshotDto), 200)]
    public async Task<ActionResult<DealerSnapshotDto>> GetSnapshot(
        Guid dealerId,
        [FromQuery] DateTime? date = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var snapshotDate = date ?? DateTime.UtcNow.Date;
            var query = new GetSnapshotComparisonQuery(dealerId, snapshotDate);
            var result = await _mediator.Send(query);
            return Ok(result.Current);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting snapshot for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving snapshot" });
        }
    }
    
    /// <summary>
    /// Obtener comparación con período anterior
    /// </summary>
    [HttpGet("{dealerId:guid}/comparison")]
    [ProducesResponseType(typeof(SnapshotComparisonDto), 200)]
    public async Task<ActionResult<SnapshotComparisonDto>> GetComparison(
        Guid dealerId,
        [FromQuery] int compareDays = 30)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetSnapshotComparisonQuery(dealerId, DateTime.UtcNow.Date, compareDays);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comparison for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving comparison" });
        }
    }
    
    /// <summary>
    /// Obtener tendencias de una métrica específica
    /// </summary>
    [HttpGet("{dealerId:guid}/trends/{metricType}")]
    [ProducesResponseType(typeof(List<TrendDataPointDto>), 200)]
    public async Task<ActionResult<List<TrendDataPointDto>>> GetTrends(
        Guid dealerId,
        string metricType,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var validMetrics = new[] { "views", "contacts", "sales", "revenue", "conversion", "leads" };
            if (!validMetrics.Contains(metricType.ToLower()))
            {
                return BadRequest(new { Message = $"Invalid metric type. Use: {string.Join(", ", validMetrics)}" });
            }
            
            var end = toDate ?? DateTime.UtcNow;
            var start = fromDate ?? end.AddDays(-30);
            
            var query = new GetTrendsQuery(dealerId, start, end, metricType.ToLower());
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trends for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving trends" });
        }
    }
    
    /// <summary>
    /// Obtener métricas de engagement (vistas, contactos, favoritos)
    /// </summary>
    [HttpGet("{dealerId:guid}/engagement")]
    [ProducesResponseType(typeof(EngagementMetricsDto), 200)]
    public async Task<ActionResult<EngagementMetricsDto>> GetEngagement(
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
            
            // Utiliza el overview query y extrae solo engagement
            var query = new GetAnalyticsOverviewQuery(dealerId, start, end);
            var result = await _mediator.Send(query);
            
            var engagement = new EngagementMetricsDto
            {
                TotalViews = result.Kpis.TotalViews,
                ViewsChange = result.Kpis.ViewsChange,
                TotalContacts = result.Kpis.TotalContacts,
                ContactsChange = result.Kpis.ContactsChange,
                TotalFavorites = result.CurrentSnapshot.TotalFavorites,
                ContactRate = result.CurrentSnapshot.ContactRate,
                FavoriteRate = result.CurrentSnapshot.FavoriteRate,
                ClickThroughRate = result.CurrentSnapshot.ClickThroughRate,
                ViewsTrend = result.ViewsTrend,
                ContactsTrend = result.ContactsTrend
            };
            
            return Ok(engagement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engagement for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving engagement metrics" });
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

public record EngagementMetricsDto
{
    public int TotalViews { get; init; }
    public double ViewsChange { get; init; }
    public int TotalContacts { get; init; }
    public double ContactsChange { get; init; }
    public int TotalFavorites { get; init; }
    public double ContactRate { get; init; }
    public double FavoriteRate { get; init; }
    public double ClickThroughRate { get; init; }
    public List<TrendDataPointDto> ViewsTrend { get; init; } = new();
    public List<TrendDataPointDto> ContactsTrend { get; init; } = new();
}
