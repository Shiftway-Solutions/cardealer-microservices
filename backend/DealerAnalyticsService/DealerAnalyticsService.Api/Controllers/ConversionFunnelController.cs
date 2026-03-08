using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DealerAnalyticsService.Application.Features.Funnel.Queries;
using DealerAnalyticsService.Application.DTOs;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversionFunnelController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConversionFunnelController> _logger;
    
    public ConversionFunnelController(IMediator mediator, ILogger<ConversionFunnelController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener funnel de conversión para un dealer
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="fromDate">Fecha de inicio (opcional, default: últimos 30 días)</param>
    /// <param name="toDate">Fecha de fin (opcional, default: hoy)</param>
    /// <returns>Funnel de conversión</returns>
    [HttpGet("{dealerId:guid}")]
    public async Task<ActionResult<ConversionFunnelDto>> GetConversionFunnel(
        Guid dealerId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var endDate = toDate ?? DateTime.UtcNow;
            var startDate = fromDate ?? endDate.AddDays(-30);
            
            var query = new GetConversionFunnelQuery(dealerId, startDate, endDate);
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversion funnel for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving conversion funnel" });
        }
    }
    
    /// <summary>
    /// Obtener funnel simplificado para gráficos
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="fromDate">Fecha de inicio (opcional)</param>
    /// <param name="toDate">Fecha de fin (opcional)</param>
    /// <returns>Datos simplificados para visualización</returns>
    [HttpGet("{dealerId:guid}/visualization")]
    public async Task<ActionResult<object>> GetFunnelVisualization(
        Guid dealerId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var endDate = toDate ?? DateTime.UtcNow;
            var startDate = fromDate ?? endDate.AddDays(-30);
            
            var query = new GetConversionFunnelQuery(dealerId, startDate, endDate);
            var result = await _mediator.Send(query);
            
            var visualization = new
            {
                Steps = new[]
                {
                    new { 
                        Name = "Vistas", 
                        Value = result.TotalViews, 
                        Percentage = 100m,
                        Color = "#3B82F6" 
                    },
                    new { 
                        Name = "Contactos", 
                        Value = result.TotalContacts, 
                        Percentage = result.ViewToContactRate,
                        Color = "#10B981" 
                    },
                    new { 
                        Name = "Test Drives", 
                        Value = result.TestDriveRequests, 
                        Percentage = result.ContactToTestDriveRate,
                        Color = "#F59E0B" 
                    },
                    new { 
                        Name = "Ventas", 
                        Value = result.ActualSales, 
                        Percentage = result.TestDriveToSaleRate,
                        Color = "#EF4444" 
                    }
                },
                Overall = new
                {
                    ConversionRate = result.OverallConversionRate,
                    AverageTimeToSale = result.AverageTimeToSale
                },
                Period = new { From = startDate, To = endDate }
            };
            
            return Ok(visualization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting funnel visualization for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving funnel visualization" });
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
