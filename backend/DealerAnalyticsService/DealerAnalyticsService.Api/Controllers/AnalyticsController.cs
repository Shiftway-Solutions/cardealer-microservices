using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Application.Features.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get dashboard analytics for a dealer
    /// </summary>
    [HttpGet("dashboard/{dealerId}")]
    [Authorize]
    [ProducesResponseType(typeof(AnalyticsDashboardDto), 200)]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetDashboard(
        Guid dealerId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var query = new GetDashboardAnalyticsQuery(dealerId, start, end);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Track profile view (called by frontend)
    /// </summary>
    [HttpPost("track/view")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfileViewDto), 200)]
    public async Task<ActionResult<ProfileViewDto>> TrackProfileView(
        [FromBody] TrackProfileViewRequest request)
    {
        var command = new TrackProfileViewCommand(request);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Track contact event (called when user clicks phone, email, WhatsApp, etc.)
    /// </summary>
    [HttpPost("track/contact")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ContactEventDto), 200)]
    public async Task<ActionResult<ContactEventDto>> TrackContactEvent(
        [FromBody] TrackContactEventRequest request)
    {
        var command = new TrackContactEventCommand(request);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("/health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "DealerAnalyticsService", timestamp = DateTime.UtcNow });
    }

    private bool IsAuthorizedForDealer(Guid dealerId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        if (role is "Admin" or "SuperAdmin")
            return true;

        var claimDealerId = User.FindFirst("dealerId")?.Value;
        return Guid.TryParse(claimDealerId, out var id) && id == dealerId;
    }
}
