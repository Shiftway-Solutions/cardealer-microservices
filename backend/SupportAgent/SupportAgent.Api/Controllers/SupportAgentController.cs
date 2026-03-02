using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupportAgent.Application.DTOs;
using SupportAgent.Application.Features.Chat.Commands;
using SupportAgent.Application.Features.Chat.Queries;

namespace SupportAgent.Api.Controllers;

[ApiController]
[Route("api/support")]
public class SupportAgentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SupportAgentController> _logger;

    public SupportAgentController(IMediator mediator, ILogger<SupportAgentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the SupportAgent chatbot.
    /// Supports both authenticated and anonymous users.
    /// </summary>
    [HttpPost("message")]
    [AllowAnonymous]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> SendMessage([FromBody] SupportChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, error = "El mensaje no puede estar vacío." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new SendMessageCommand(
            request.Message,
            request.SessionId,
            userId,
            ipAddress);

        var result = await _mediator.Send(command, ct);

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get conversation history for a given session.
    /// </summary>
    [HttpGet("session/{sessionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSessionHistory(string sessionId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSessionHistoryQuery(sessionId), ct);

        if (result == null)
            return NotFound(new { success = false, error = "Sesión no encontrada." });

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Health/status endpoint for the SupportAgent service.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status()
    {
        return Ok(new
        {
            service = "SupportAgent",
            description = "AI Support & Buyer Protection Agent for OKLA Marketplace",
            status = "healthy",
            version = "1.0.0",
            model = "claude-haiku-4-5-20251001",
            modules = new[] { "soporte_tecnico", "orientacion_comprador" },
            timestamp = DateTime.UtcNow
        });
    }
}
