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
            Message = dto.Message,
            // SEM FIX: Forward UTM attribution from frontend
            UtmSource = dto.UtmSource,
            UtmMedium = dto.UtmMedium,
            UtmCampaign = dto.UtmCampaign,
            UtmTerm = dto.UtmTerm,
            UtmContent = dto.UtmContent,
            Gclid = dto.Gclid,
            Fbclid = dto.Fbclid,
            LandingPage = dto.LandingPage
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

    // ========================================================================
    // MONTHLY USAGE — CONTRA #5 / OVERAGE BILLING FIX
    // ========================================================================

    /// <summary>
    /// Get the current-month conversation usage for the authenticated dealer.
    /// Returns the count, plan limit, remaining quota, and usage percentage.
    /// Used by the dealer dashboard to display real-time ChatAgent usage.
    /// </summary>
    [HttpGet("usage/monthly-count")]
    public async Task<IActionResult> GetMonthlyUsageCount()
    {
        var dealerId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMonthlyConversationUsageQuery
        {
            DealerId = dealerId
        });
        return Ok(result);
    }

    // ========================================================================
    // OVERAGE REPORT — CONTRA #5 / OVERAGE BILLING FIX
    // ========================================================================

    /// <summary>
    /// Get the conversation overage report for the current dealer.
    /// Returns a detailed breakdown of each overage conversation with date/time.
    /// Downloadable by the dealer for billing reconciliation.
    /// </summary>
    /// <param name="period">Billing period in YYYY-MM format. Defaults to current month.</param>
    [HttpGet("overage-report")]
    public async Task<IActionResult> GetOverageReport([FromQuery] string? period = null)
    {
        var dealerId = GetCurrentUserId();
        var result = await _mediator.Send(new GetConversationOverageReportQuery
        {
            DealerId = dealerId,
            BillingPeriod = period
        });
        return Ok(result);
    }

    /// <summary>
    /// Download the conversation overage report as CSV.
    /// Contains: ConversationNumber, DateTime, ContactRequestId, Subject, UnitCost
    /// </summary>
    /// <param name="period">Billing period in YYYY-MM format. Defaults to current month.</param>
    [HttpGet("overage-report/csv")]
    public async Task<IActionResult> DownloadOverageReportCsv([FromQuery] string? period = null)
    {
        var dealerId = GetCurrentUserId();
        var report = await _mediator.Send(new GetConversationOverageReportQuery
        {
            DealerId = dealerId,
            BillingPeriod = period
        });

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ConversationNumber,DateTime(UTC),ContactRequestId,Subject,UnitCost(USD)");
        foreach (var detail in report.Details)
        {
            var subject = detail.Subject.Replace(",", " ").Replace("\"", "'");
            csv.AppendLine($"{detail.ConversationNumber},{detail.OccurredAtUtc:yyyy-MM-dd HH:mm:ss},{detail.ContactRequestId},{subject},{detail.UnitCost:F2}");
        }
        csv.AppendLine();
        csv.AppendLine($"# Period: {report.BillingPeriod}");
        csv.AppendLine($"# Plan: {report.DealerPlan}");
        csv.AppendLine($"# Included Limit: {report.IncludedLimit}");
        csv.AppendLine($"# Overage Count: {report.OverageCount}");
        csv.AppendLine($"# Total Overage Cost: ${report.TotalOverageCost:F2} {report.Currency}");

        var fileName = $"overage-report-{report.BillingPeriod}.csv";
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            fileName);
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
    string Message,
    // SEM FIX: UTM attribution fields for lead tracking
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmTerm = null,
    string? UtmContent = null,
    string? Gclid = null,
    string? Fbclid = null,
    string? LandingPage = null
);

/// <summary>Input DTO for replying to a contact request.</summary>
public record ReplyInputDto(string Message);