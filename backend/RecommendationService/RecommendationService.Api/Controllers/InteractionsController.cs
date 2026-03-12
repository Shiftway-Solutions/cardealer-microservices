using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecommendationService.Application.DTOs;
using RecommendationService.Application.Features.Recommendations.Commands;
using System;
using System.Threading.Tasks;

namespace RecommendationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InteractionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InteractionsController> _logger;

    public InteractionsController(IMediator mediator, ILogger<InteractionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Registrar interacción de usuario con vehículo
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<VehicleInteractionDto>> TrackInteraction([FromBody] TrackInteractionRequest request)
    {
        try
        {
            var userId = GetUserId();

            var command = new TrackInteractionCommand(
                userId,
                request.VehicleId,
                request.Type,
                request.DurationSeconds,
                request.Source
            );

            var interaction = await _mediator.Send(command);
            return Ok(interaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registrar interacción anónima (sin autenticación)
    /// </summary>
    [HttpPost("anonymous")]
    public async Task<ActionResult> TrackAnonymousInteraction([FromBody] TrackInteractionRequest request)
    {
        // Para usuarios no autenticados, usamos un ID temporal basado en IP/sesión
        // En producción, esto se complementaría con cookies/fingerprinting

        _logger.LogInformation("Anonymous interaction tracked: VehicleId={VehicleId}, Type={Type}",
            request.VehicleId, request.Type);

        return Ok(new { message = "Interaction tracked" });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
