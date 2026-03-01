using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.UseCases.Analytics;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for platform analytics endpoints (/admin/analytics page)
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IMediator mediator, ILogger<AnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get full platform analytics</summary>
    [HttpGet]
    public async Task<IActionResult> GetPlatformAnalytics([FromQuery] string period = "7d")
    {
        _logger.LogInformation("Getting platform analytics for period={Period}", period);
        var result = await _mediator.Send(new GetPlatformAnalyticsQuery(period));
        return Ok(result);
    }

    /// <summary>Get analytics overview stats</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetAnalyticsOverviewQuery(period));
        return Ok(result);
    }

    /// <summary>Get weekly data points</summary>
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyData([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetWeeklyDataQuery(period));
        return Ok(result);
    }

    /// <summary>Get top vehicle searches</summary>
    [HttpGet("top-vehicles")]
    public async Task<IActionResult> GetTopVehicles([FromQuery] int limit = 5)
    {
        var result = await _mediator.Send(new GetTopVehicleSearchesQuery(limit));
        return Ok(result);
    }

    /// <summary>Get traffic sources breakdown</summary>
    [HttpGet("traffic-sources")]
    public async Task<IActionResult> GetTrafficSources([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetTrafficSourcesQuery(period));
        return Ok(result);
    }

    /// <summary>Get device breakdown</summary>
    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetDeviceBreakdownQuery(period));
        return Ok(result);
    }

    /// <summary>Get conversion rates</summary>
    [HttpGet("conversions")]
    public async Task<IActionResult> GetConversions([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetConversionRatesQuery(period));
        return Ok(result);
    }

    /// <summary>Get revenue by channel</summary>
    [HttpGet("revenue-channels")]
    public async Task<IActionResult> GetRevenueChannels([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetRevenueByChannelQuery(period));
        return Ok(result);
    }

    /// <summary>Export analytics report (returns JSON summary)</summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportReport([FromQuery] string period = "7d", [FromQuery] string format = "pdf")
    {
        _logger.LogInformation("Exporting analytics report period={Period} format={Format}", period, format);
        var result = await _mediator.Send(new GetPlatformAnalyticsQuery(period));
        return Ok(new { message = "Export iniciado", data = result, format, period });
    }
}
