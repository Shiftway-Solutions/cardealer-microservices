using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.UseCases.Messages;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for admin messages endpoints (/admin/mensajes page)
/// </summary>
[ApiController]
[Route("api/admin/messages")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminMessagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminMessagesController> _logger;

    public AdminMessagesController(IMediator mediator, ILogger<AdminMessagesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get admin messages/inbox</summary>
    [HttpGet]
    public async Task<IActionResult> GetMessages(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        _logger.LogInformation("Getting admin messages");
        var result = await _mediator.Send(new GetAdminMessagesQuery(search, status, priority));
        return Ok(result);
    }

    /// <summary>Mark a message as read</summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        await _mediator.Send(new MarkMessageReadCommand(id));
        return Ok(new { message = "Mensaje marcado como leído" });
    }

    /// <summary>Reply to a message</summary>
    [HttpPost("{id}/reply")]
    public async Task<IActionResult> Reply(string id, [FromBody] ReplyRequest request)
    {
        await _mediator.Send(new ReplyToMessageCommand(id, request.Message));
        return Ok(new { message = "Respuesta enviada exitosamente" });
    }
}

public record ReplyRequest(string Message);
