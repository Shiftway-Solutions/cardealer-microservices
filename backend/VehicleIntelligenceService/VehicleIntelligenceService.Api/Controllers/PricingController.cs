using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleIntelligenceService.Application.DTOs;
using VehicleIntelligenceService.Application.Features.Pricing.Commands;
using VehicleIntelligenceService.Application.Features.Pricing.Queries;

namespace VehicleIntelligenceService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PricingController> _logger;

    public PricingController(IMediator mediator, ILogger<PricingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Analiza el precio de un vehículo y genera recomendaciones
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<PriceAnalysisDto>> AnalyzePrice([FromBody] CreatePriceAnalysisRequest request)
    {
        _logger.LogInformation("Analyzing price for vehicle {VehicleId}", request.VehicleId);
        
        var command = new AnalyzeVehiclePriceCommand(request);
        var result = await _mediator.Send(command);
        
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el último análisis de precio para un vehículo
    /// </summary>
    [HttpGet("vehicle/{vehicleId}/latest")]
    public async Task<ActionResult<PriceAnalysisDto>> GetLatestAnalysis(Guid vehicleId)
    {
        var query = new GetLatestPriceAnalysisQuery(vehicleId);
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound(new { message = "No price analysis found for this vehicle" });
        
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un análisis de precio específico por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PriceAnalysisDto>> GetById(Guid id)
    {
        var query = new GetPriceAnalysisByIdQuery(id);
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound(new { message = "Price analysis not found" });
        
        return Ok(result);
    }
}
