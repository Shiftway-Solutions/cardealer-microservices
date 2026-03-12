using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIProcessingService.Application.DTOs;
using AIProcessingService.Application.Features.Queries;

namespace AIProcessingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackgroundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BackgroundsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get available backgrounds for the current user
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<AvailableBackgroundsResponse>> GetAvailableBackgrounds(CancellationToken ct)
    {
        var accountType = User.IsInRole("Dealer") ? "Dealer" : User.IsInRole("Admin") ? "Admin" : "Individual";
        var hasSubscription = User.HasClaim("subscription", "active");
        var query = new GetAvailableBackgroundsQuery(accountType, hasSubscription);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get all backgrounds (admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AvailableBackgroundsResponse>> GetAllBackgrounds(CancellationToken ct)
    {
        var query = new GetAvailableBackgroundsQuery("Admin", true);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific background by code
    /// </summary>
    [HttpGet("{code}")]
    public async Task<ActionResult<BackgroundDto>> GetBackgroundByCode(string code, CancellationToken ct)
    {
        var accountType = User.IsInRole("Dealer") ? "Dealer" : User.IsInRole("Admin") ? "Admin" : "Individual";
        var hasSubscription = User.Identity?.IsAuthenticated == true && User.HasClaim("subscription", "active");

        var query = new GetAvailableBackgroundsQuery(accountType, hasSubscription);
        var result = await _mediator.Send(query, ct);

        var background = result.Backgrounds.FirstOrDefault(b => b.Code == code);
        if (background == null)
            return NotFound();


        return Ok(background);
    }
}
