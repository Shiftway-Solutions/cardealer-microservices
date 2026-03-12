using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleIntelligenceService.Application.DTOs;
using VehicleIntelligenceService.Application.Features.Demand.Commands;
using VehicleIntelligenceService.Application.Features.Demand.Queries;

namespace VehicleIntelligenceService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DemandController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DemandController> _logger;

    public DemandController(IMediator mediator, ILogger<DemandController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Predice la demanda para un vehículo específico
    /// </summary>
    [HttpPost("predict")]
    public async Task<ActionResult<DemandPredictionDto>> PredictDemand([FromBody] CreateDemandPredictionRequest request)
    {
        _logger.LogInformation("Predicting demand for {Make} {Model} {Year}",
            request.Make, request.Model, request.Year);

        var command = new PredictDemandCommand(request);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la última predicción de demanda para un vehículo
    /// </summary>
    [HttpGet("{make}/{model}/{year}")]
    public async Task<ActionResult<DemandPredictionDto>> GetDemandPrediction(
        string make,
        string model,
        int year)
    {
        var query = new GetDemandPredictionQuery(make, model, year);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "No demand prediction found for this vehicle" });

        return Ok(result);
    }
}
