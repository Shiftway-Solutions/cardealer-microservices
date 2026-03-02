using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoAgent.Application.DTOs;
using RecoAgent.Application.Features.Config.Commands;
using RecoAgent.Application.Features.Config.Queries;
using RecoAgent.Application.Features.Feedback.Commands;
using RecoAgent.Application.Features.Recommend.Queries;

namespace RecoAgent.Api.Controllers;

[ApiController]
[Route("api/reco-agent")]
public class RecoAgentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RecoAgentController> _logger;

    public RecoAgentController(IMediator mediator, ILogger<RecoAgentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generate personalized vehicle recommendations using Claude Sonnet 4.5.
    /// Accepts user profile + vehicle candidates, returns ranked recommendations with explanations.
    /// </summary>
    [HttpPost("recommend")]
    [AllowAnonymous]
    public async Task<IActionResult> Recommend([FromBody] RecoAgentRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var query = new GenerateRecommendationsQuery(request, userId, ipAddress);
        var result = await _mediator.Send(query, ct);

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Record feedback on a recommendation (thumbs up/down, dismiss, click).
    /// Used to improve future recommendations for this user.
    /// </summary>
    [HttpPost("feedback")]
    [AllowAnonymous]
    public async Task<IActionResult> Feedback([FromBody] RecommendationFeedbackRequest request, CancellationToken ct)
    {
        var command = new RecordFeedbackCommand(request);
        var result = await _mediator.Send(command, ct);

        return Ok(new { success = true, data = new { recorded = result } });
    }

    /// <summary>
    /// Get current RecoAgent configuration. Admin only.
    /// </summary>
    [HttpGet("config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var config = await _mediator.Send(new GetRecoAgentConfigQuery(), ct);
        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// Update RecoAgent configuration. Admin only.
    /// Allows partial updates — only send the fields you want to change.
    /// </summary>
    [HttpPut("config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateConfig(
        [FromBody] UpdateRecoAgentConfigRequest request,
        CancellationToken ct)
    {
        var updatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "admin";
        var command = new UpdateRecoAgentConfigCommand(request, updatedBy);
        var config = await _mediator.Send(command, ct);

        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// Health probe for the recommendation service.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status()
    {
        return Ok(new
        {
            service = "RecoAgent",
            status = "healthy",
            version = "1.0.0",
            model = "claude-sonnet-4-5-20251022",
            timestamp = DateTime.UtcNow
        });
    }
}
