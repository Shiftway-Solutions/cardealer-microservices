using ContactService.Application.Features.ContactRequests.Commands;
using ContactService.Application.Features.ContactRequests.Queries;
using ContactService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ContactService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new contact request (inquiry)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateContactRequest(CreateContactRequestInputDto dto)
    {
        var buyerId = GetCurrentUserId();

        var command = new CreateContactRequestCommand
        {
            VehicleId = dto.VehicleId,
            SellerId = dto.SellerId,
            BuyerId = buyerId,
            Subject = dto.Subject,
            BuyerName = dto.BuyerName,
            BuyerEmail = dto.BuyerEmail,
            BuyerPhone = dto.BuyerPhone,
            Message = dto.Message
        };

        var result = await _mediator.Send(command);
        return StatusCode(201, ApiResponse<object>.SuccessResponse(result, "Solicitud de contacto creada exitosamente"));
    }

    /// <summary>
    /// Get contact requests for the current user (buyer perspective)
    /// </summary>
    [HttpGet("my-inquiries")]
    public async Task<IActionResult> GetMyInquiries()
    {
        var buyerId = GetCurrentUserId();
        var result = await _mediator.Send(new GetContactRequestsByBuyerQuery { BuyerId = buyerId });
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    /// <summary>
    /// Get contact requests received (seller perspective)
    /// </summary>
    [HttpGet("received")]
    public async Task<IActionResult> GetReceivedInquiries()
    {
        var sellerId = GetCurrentUserId();
        var result = await _mediator.Send(new GetContactRequestsBySellerQuery { SellerId = sellerId });
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    /// <summary>
    /// Get specific contact request with messages
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetContactRequest(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _mediator.Send(new GetContactRequestDetailQuery
        {
            ContactRequestId = id,
            CurrentUserId = currentUserId
        });

        if (result == null) return NotFound(ApiResponse<object>.ErrorResponse("Solicitud de contacto no encontrada"));
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    /// <summary>
    /// Reply to a contact request
    /// </summary>
    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToContactRequest(Guid id, ReplyInputDto dto)
    {
        var currentUserId = GetCurrentUserId();

        var result = await _mediator.Send(new ReplyToContactRequestCommand
        {
            ContactRequestId = id,
            CurrentUserId = currentUserId,
            Message = dto.Message
        });

        return StatusCode(201, ApiResponse<object>.SuccessResponse(result, "Respuesta enviada exitosamente"));
    }

    /// <summary>
    /// Mark a contact request as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        await _mediator.Send(new UpdateContactRequestStatusCommand
        {
            ContactRequestId = id,
            CurrentUserId = currentUserId,
            NewStatus = "Read"
        });
        return NoContent();
    }

    /// <summary>
    /// Archive a contact request
    /// </summary>
    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> ArchiveContactRequest(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        await _mediator.Send(new UpdateContactRequestStatusCommand
        {
            ContactRequestId = id,
            CurrentUserId = currentUserId,
            NewStatus = "Archived"
        });
        return NoContent();
    }

    /// <summary>
    /// Delete a contact request
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContactRequest(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        await _mediator.Send(new DeleteContactRequestCommand
        {
            ContactRequestId = id,
            CurrentUserId = currentUserId
        });
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID claim not found"));
    }
}

/// <summary>Input DTO for creating a contact request.</summary>
public record CreateContactRequestInputDto(
    Guid VehicleId,
    Guid SellerId,
    string Subject,
    string BuyerName,
    string BuyerEmail,
    string? BuyerPhone,
    string Message
);

/// <summary>Input DTO for replying to a contact request.</summary>
public record ReplyInputDto(string Message);