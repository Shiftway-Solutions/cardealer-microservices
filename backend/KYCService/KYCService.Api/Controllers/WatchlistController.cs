using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KYCService.Application.Commands;
using KYCService.Application.Queries;
using KYCService.Application.DTOs;
using KYCService.Domain.Entities;

namespace KYCService.Api.Controllers;

/// <summary>
/// Controlador para listas de control (PEP, Sanciones, etc.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrCompliance")]
public class WatchlistController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WatchlistController> _logger;

    public WatchlistController(IMediator mediator, ILogger<WatchlistController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Buscar en listas de control
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<WatchlistEntryDto>>> Search(
        [FromQuery] string searchTerm,
        [FromQuery] WatchlistType? listType = null)
    {
        // SECURITY: Validate search term length and content
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return BadRequest("Search term must be at least 2 characters");
        if (searchTerm.Length > 200)
            return BadRequest("Search term cannot exceed 200 characters");
        var query = new SearchWatchlistQuery
        {
            SearchTerm = searchTerm,
            ListType = listType
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtener entrada por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WatchlistEntryDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetWatchlistEntryByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Agregar entrada a lista de control
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WatchlistEntryDto>> Add([FromBody] AddWatchlistEntryCommand command)
    {
        var result = await _mediator.Send(command);
        _logger.LogInformation("Watchlist entry added: {FullName} to {ListType}",
            command.FullName, command.ListType);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Realizar screening de una persona/entidad
    /// </summary>
    [HttpPost("screen")]
    [Authorize] // Cualquier usuario autenticado puede hacer screening
    public async Task<ActionResult<ScreeningResultDto>> Screen([FromBody] ScreenWatchlistCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.HasMatches)
        {
            _logger.LogWarning("Watchlist screening found {Count} matches for {Name}",
                result.TotalMatches, command.FullName);
        }

        return Ok(result);
    }
}
