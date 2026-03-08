using ContactService.Application.Features.ContactRequests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ContactService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactMessagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactMessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Mark a message as read (only the message recipient can mark it)
    /// </summary>
    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        await _mediator.Send(new MarkMessageAsReadCommand
        {
            MessageId = id,
            CurrentUserId = currentUserId
        });
        return NoContent();
    }

    /// <summary>
    /// Get unread message count for current user
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _mediator.Send(new GetUnreadCountQuery { UserId = userId });
        return Ok(new { Count = count });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID claim not found"));
    }
}