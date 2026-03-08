using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DealerAnalyticsService.Application.Features.Insights.Commands;
using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsightsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDealerInsightRepository _insightRepository;
    private readonly ILogger<InsightsController> _logger;
    
    public InsightsController(
        IMediator mediator,
        IDealerInsightRepository insightRepository,
        ILogger<InsightsController> logger)
    {
        _mediator = mediator;
        _insightRepository = insightRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener insights para un dealer
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="onlyUnread">Solo insights no leídos (default: false)</param>
    /// <returns>Lista de insights</returns>
    [HttpGet("{dealerId:guid}")]
    public async Task<ActionResult<List<DealerInsightDto>>> GetDealerInsights(
        Guid dealerId,
        [FromQuery] bool onlyUnread = false)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var insights = await _insightRepository.GetDealerInsightsAsync(dealerId, onlyUnread);
            
            var dtos = insights.Select(insight => new DealerInsightDto
            {
                Id = insight.Id,
                DealerId = insight.DealerId,
                Type = insight.Type.ToString(),
                Priority = insight.Priority.ToString(),
                Title = insight.Title,
                Description = insight.Description,
                ActionRecommendation = insight.ActionRecommendation,
                PotentialImpact = insight.PotentialImpact,
                Confidence = insight.Confidence,
                IsRead = insight.IsRead,
                IsActedUpon = insight.IsActedUpon,
                ActionDate = insight.ActionDate,
                CreatedAt = insight.CreatedAt,
                UpdatedAt = insight.UpdatedAt,
                ExpiresAt = insight.ExpiresAt
            }).ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving insights" });
        }
    }
    
    /// <summary>
    /// Generar nuevos insights para un dealer
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <returns>Lista de insights generados</returns>
    [HttpPost("{dealerId:guid}/generate")]
    public async Task<ActionResult<List<DealerInsightDto>>> GenerateInsights(Guid dealerId)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var command = new GenerateInsightsCommand(dealerId);
            var result = await _mediator.Send(command);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating insights for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error generating insights" });
        }
    }
    
    /// <summary>
    /// Marcar insights como leídos
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="insightIds">IDs de los insights a marcar</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{dealerId:guid}/mark-read")]
    public async Task<ActionResult> MarkInsightsAsRead(
        Guid dealerId,
        [FromBody] List<Guid> insightIds)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            await _insightRepository.MarkInsightsAsReadAsync(dealerId, insightIds);
            return Ok(new { Message = "Insights marked as read", Count = insightIds.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking insights as read for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error marking insights as read" });
        }
    }
    
    /// <summary>
    /// Marcar insight como implementado
    /// </summary>
    /// <param name="insightId">ID del insight</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{insightId:guid}/mark-acted")]
    public async Task<ActionResult> MarkInsightAsActedUpon(Guid insightId)
    {
        try
        {
            await _insightRepository.MarkInsightAsActedUponAsync(insightId, DateTime.UtcNow);
            return Ok(new { Message = "Insight marked as acted upon" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking insight {InsightId} as acted upon", insightId);
            return StatusCode(500, new { Message = "Error marking insight as acted upon" });
        }
    }
    
    /// <summary>
    /// Obtener resumen de insights para dashboard
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <returns>Resumen de insights</returns>
    [HttpGet("{dealerId:guid}/summary")]
    public async Task<ActionResult<object>> GetInsightsSummary(Guid dealerId)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var insights = await _insightRepository.GetDealerInsightsAsync(dealerId);
            
            var summary = new
            {
                Total = insights.Count(),
                Unread = insights.Count(i => !i.IsRead),
                ByPriority = new
                {
                    Critical = insights.Count(i => i.Priority == Domain.Entities.InsightPriority.Critical),
                    High = insights.Count(i => i.Priority == Domain.Entities.InsightPriority.High),
                    Medium = insights.Count(i => i.Priority == Domain.Entities.InsightPriority.Medium),
                    Low = insights.Count(i => i.Priority == Domain.Entities.InsightPriority.Low)
                },
                ByType = new
                {
                    PricingRecommendation = insights.Count(i => i.Type == Domain.Entities.InsightType.PricingRecommendation),
                    InventoryOptimization = insights.Count(i => i.Type == Domain.Entities.InsightType.InventoryOptimization),
                    MarketingStrategy = insights.Count(i => i.Type == Domain.Entities.InsightType.MarketingStrategy),
                    SeasonalTrend = insights.Count(i => i.Type == Domain.Entities.InsightType.SeasonalTrend),
                    CompetitorAnalysis = insights.Count(i => i.Type == Domain.Entities.InsightType.CompetitorAnalysis)
                },
                ActedUpon = insights.Count(i => i.IsActedUpon),
                AvgPotentialImpact = insights.Any() ? insights.Average(i => i.PotentialImpact) : 0,
                AvgConfidence = insights.Any() ? insights.Average(i => i.Confidence) : 0
            };
            
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights summary for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving insights summary" });
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
