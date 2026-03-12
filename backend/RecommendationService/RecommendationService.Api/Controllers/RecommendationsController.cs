using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecommendationService.Application.DTOs;
using RecommendationService.Application.Features.Recommendations.Commands;
using RecommendationService.Application.Features.Recommendations.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecommendationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(IMediator mediator, ILogger<RecommendationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtener recomendaciones personalizadas para un usuario
    /// </summary>
    [HttpGet("for-you")]
    [Authorize]
    public async Task<ActionResult<List<RecommendationDto>>> GetForYou([FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var query = new GetRecommendationsForUserQuery(userId, limit);
        var recommendations = await _mediator.Send(query);
        return Ok(recommendations);
    }

    /// <summary>
    /// Obtener vehículos similares a uno específico
    /// </summary>
    [HttpGet("similar/{vehicleId}")]
    public async Task<ActionResult<List<RecommendationDto>>> GetSimilar(Guid vehicleId, [FromQuery] int limit = 10)
    {
        var query = new GetSimilarVehiclesQuery(vehicleId, limit);
        var recommendations = await _mediator.Send(query);
        return Ok(recommendations);
    }

    /// <summary>
    /// Generar nuevas recomendaciones para un usuario
    /// </summary>
    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult<List<RecommendationDto>>> Generate([FromBody] GenerateRecommendationsRequest request)
    {
        var userId = GetUserId();
        var command = new GenerateRecommendationsCommand(userId, request.Limit);
        var recommendations = await _mediator.Send(command);
        return Ok(recommendations);
    }

    /// <summary>
    /// Marcar recomendación como vista
    /// </summary>
    [HttpPost("{recommendationId}/viewed")]
    [Authorize]
    public async Task<ActionResult> MarkViewed(Guid recommendationId)
    {
        var command = new MarkRecommendationViewedCommand(recommendationId);
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound();

        return Ok(new { message = "Recommendation marked as viewed" });
    }

    /// <summary>
    /// Marcar recomendación como clickeada
    /// </summary>
    [HttpPost("{recommendationId}/clicked")]
    [Authorize]
    public async Task<ActionResult> MarkClicked(Guid recommendationId)
    {
        var command = new MarkRecommendationClickedCommand(recommendationId);
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound();

        return Ok(new { message = "Recommendation marked as clicked" });
    }

    /// <summary>
    /// Obtener preferencias del usuario
    /// </summary>
    [HttpGet("preferences")]
    [Authorize]
    public async Task<ActionResult<UserPreferenceDto>> GetPreferences()
    {
        var userId = GetUserId();
        var query = new GetUserPreferencesQuery(userId);
        var preferences = await _mediator.Send(query);

        if (preferences == null)
            return NotFound(new { message = "User preferences not found" });

        return Ok(preferences);
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
