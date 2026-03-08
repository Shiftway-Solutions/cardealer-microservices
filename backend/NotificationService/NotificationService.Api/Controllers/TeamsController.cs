using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "NotificationServiceAdmin")]
public class TeamsController : ControllerBase
{
    private readonly ITeamsProvider _teamsProvider;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(
        ITeamsProvider teamsProvider,
        ILogger<TeamsController> logger)
    {
        _teamsProvider = teamsProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sends a test alert to Microsoft Teams.
    /// </summary>
    /// <param name="request">The alert request</param>
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendTeamsAlert([FromBody] TeamsAlertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Title and Message are required" });
        }

        try
        {
            var success = await _teamsProvider.SendAdaptiveCardAsync(
                request.Title,
                request.Message,
                request.Severity ?? "Info",
                request.Metadata);

            if (success)
            {
                return Ok(new { message = "Teams alert sent successfully", title = request.Title });
            }

            return StatusCode(500, new { error = "Failed to send Teams alert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams alert");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check for Teams integration.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "TeamsNotificationService",
            timestamp = DateTime.UtcNow
        });
    }
}
