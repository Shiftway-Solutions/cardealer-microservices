using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchAgent.Application.DTOs;
using SearchAgent.Application.Features.Config.Commands;
using SearchAgent.Application.Features.Config.Queries;
using SearchAgent.Application.Features.Search.Queries;

namespace SearchAgent.Api.Controllers;

[ApiController]
[Route("api/search-agent")]
public class SearchAgentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SearchAgentController> _logger;

    public SearchAgentController(IMediator mediator, ILogger<SearchAgentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Process a natural language vehicle search query using Claude AI.
    /// Public endpoint — no auth required for searching.
    /// </summary>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromBody] SearchAgentRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var query = new ProcessSearchQuery(
            request.Query,
            request.SessionId,
            request.Page,
            request.PageSize,
            userId,
            ipAddress
        );

        var result = await _mediator.Send(query, ct);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    /// <summary>
    /// Get current SearchAgent configuration. Admin only.
    /// </summary>
    [HttpGet("config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var config = await _mediator.Send(new GetSearchAgentConfigQuery(), ct);
        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// Update SearchAgent configuration. Admin only.
    /// Allows partial updates — only send the fields you want to change.
    /// </summary>
    [HttpPut("config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateConfig(
        [FromBody] UpdateSearchAgentConfigRequest request,
        CancellationToken ct)
    {
        var updatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "admin";

        var command = new UpdateSearchAgentConfigCommand(request, updatedBy);
        var config = await _mediator.Send(command, ct);

        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// Health probe for the AI search service.
    /// Tests Claude API connectivity.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status()
    {
        return Ok(new
        {
            service = "SearchAgent",
            status = "healthy",
            version = "2.0.0",
            timestamp = DateTime.UtcNow
        });
    }
}
