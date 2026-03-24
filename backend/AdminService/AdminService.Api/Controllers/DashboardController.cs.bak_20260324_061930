using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.UseCases.Dashboard;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for admin dashboard endpoints
/// These endpoints provide statistics, activity, and pending items for the admin dashboard
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <returns>Dashboard statistics including users, vehicles, dealers, revenue</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats()
    {
        _logger.LogInformation("Getting dashboard statistics");

        var result = await _mediator.Send(new GetDashboardStatsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get recent activity for the dashboard
    /// </summary>
    /// <param name="limit">Number of activities to return (default 10)</param>
    /// <returns>List of recent activities</returns>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(List<DashboardActivityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DashboardActivityResponse>>> GetActivity([FromQuery] int limit = 10)
    {
        _logger.LogInformation("Getting dashboard activity, limit={Limit}", limit);

        var result = await _mediator.Send(new GetDashboardActivityQuery(limit));
        return Ok(result);
    }

    /// <summary>
    /// Get pending actions for admin attention
    /// </summary>
    /// <returns>List of pending actions grouped by type</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<DashboardPendingActionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DashboardPendingActionResponse>>> GetPending()
    {
        _logger.LogInformation("Getting dashboard pending actions");

        var result = await _mediator.Send(new GetDashboardPendingQuery());
        return Ok(result);
    }
}
