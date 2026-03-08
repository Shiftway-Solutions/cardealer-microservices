using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportsService.Application.DTOs;
using ReportsService.Domain.Entities;
using ReportsService.Domain.Interfaces;

namespace ReportsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardsController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<DashboardsController> _logger;

    public DashboardsController(
        IDashboardRepository dashboardRepository,
        ILogger<DashboardsController> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DashboardDto>>> GetAll(CancellationToken cancellationToken)
    {
        var dashboards = await _dashboardRepository.GetAllAsync(cancellationToken);
        return Ok(dashboards.Select(d => MapToDto(d, false)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DashboardDto>> GetById(Guid id, CancellationToken cancellationToken, [FromQuery] bool includeWidgets = false)
    {
        Dashboard? dashboard;
        if (includeWidgets)
            dashboard = await _dashboardRepository.GetByIdWithWidgetsAsync(id, cancellationToken);
        else
            dashboard = await _dashboardRepository.GetByIdAsync(id, cancellationToken);

        if (dashboard == null)
            return NotFound();

        return Ok(MapToDto(dashboard, includeWidgets));
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<DashboardDto>>> GetByType(DashboardType type, CancellationToken cancellationToken)
    {
        var dashboards = await _dashboardRepository.GetByTypeAsync(type, cancellationToken);
        return Ok(dashboards.Select(d => MapToDto(d, false)));
    }

    [HttpGet("default")]
    public async Task<ActionResult<DashboardDto>> GetDefault(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetDefaultAsync(cancellationToken);
        if (dashboard == null)
            return NotFound();

        return Ok(MapToDto(dashboard, true));
    }

    [HttpPost]
    public async Task<ActionResult<DashboardDto>> Create(
        [FromBody] CreateDashboardRequest request,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DashboardType>(request.Type, true, out var dashboardType))
            return BadRequest("Invalid dashboard type");

        var dashboard = new Dashboard(
            dealerId,
            request.Name,
            dashboardType,
            userId,
            request.Description);

        if (!string.IsNullOrEmpty(request.Layout))
            dashboard.SetLayout(request.Layout);

        dashboard.SetPublic(request.IsPublic);

        var created = await _dashboardRepository.AddAsync(dashboard, cancellationToken);
        _logger.LogInformation("Dashboard {DashboardId} created for dealer {DealerId}", created.Id, dealerId);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created, false));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DashboardDto>> Update(Guid id, [FromBody] UpdateDashboardRequest request, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(id, cancellationToken);
        if (dashboard == null)
            return NotFound();

        dashboard.Update(request.Name, request.Description);

        if (!string.IsNullOrEmpty(request.Layout))
            dashboard.SetLayout(request.Layout);

        if (request.IsPublic.HasValue)
            dashboard.SetPublic(request.IsPublic.Value);

        await _dashboardRepository.UpdateAsync(dashboard, cancellationToken);
        _logger.LogInformation("Dashboard {DashboardId} updated", id);

        return Ok(MapToDto(dashboard, false));
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult<DashboardDto>> SetAsDefault(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(id, cancellationToken);
        if (dashboard == null)
            return NotFound();

        // Unset current default
        var currentDefault = await _dashboardRepository.GetDefaultAsync(cancellationToken);
        if (currentDefault != null && currentDefault.Id != id)
        {
            currentDefault.UnsetAsDefault();
            await _dashboardRepository.UpdateAsync(currentDefault, cancellationToken);
        }

        dashboard.SetAsDefault();
        await _dashboardRepository.UpdateAsync(dashboard, cancellationToken);
        _logger.LogInformation("Dashboard {DashboardId} set as default", id);

        return Ok(MapToDto(dashboard, false));
    }

    [HttpPost("{id:guid}/widgets")]
    public async Task<ActionResult<DashboardDto>> AddWidget(Guid id, [FromBody] CreateWidgetRequest request, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdWithWidgetsAsync(id, cancellationToken);
        if (dashboard == null)
            return NotFound();

        var widget = new DashboardWidget(
            dashboard.Id,
            request.Title,
            request.WidgetType,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height);

        if (!string.IsNullOrEmpty(request.DataSource))
            widget.SetDataSource(request.DataSource);

        if (!string.IsNullOrEmpty(request.Configuration))
            widget.SetConfiguration(request.Configuration);

        dashboard.AddWidget(widget);
        await _dashboardRepository.UpdateAsync(dashboard, cancellationToken);
        _logger.LogInformation("Widget added to dashboard {DashboardId}", id);

        return Ok(MapToDto(dashboard, true));
    }

    [HttpDelete("{id:guid}/widgets/{widgetId:guid}")]
    public async Task<ActionResult<DashboardDto>> RemoveWidget(Guid id, Guid widgetId, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdWithWidgetsAsync(id, cancellationToken);
        if (dashboard == null)
            return NotFound();

        dashboard.RemoveWidget(widgetId);
        await _dashboardRepository.UpdateAsync(dashboard, cancellationToken);
        _logger.LogInformation("Widget {WidgetId} removed from dashboard {DashboardId}", widgetId, id);

        return Ok(MapToDto(dashboard, true));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _dashboardRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
            return NotFound();

        await _dashboardRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Dashboard {DashboardId} deleted", id);

        return NoContent();
    }

    private static DashboardDto MapToDto(Dashboard dashboard, bool includeWidgets) => new(
        dashboard.Id,
        dashboard.Name,
        dashboard.Description,
        dashboard.Type.ToString(),
        dashboard.Layout,
        dashboard.IsDefault,
        dashboard.IsPublic,
        dashboard.CreatedAt,
        includeWidgets ? dashboard.Widgets.Select(MapWidgetToDto) : null
    );

    private static DashboardWidgetDto MapWidgetToDto(DashboardWidget widget) => new(
        widget.Id,
        widget.Title,
        widget.WidgetType,
        widget.DataSource,
        widget.Configuration,
        widget.PositionX,
        widget.PositionY,
        widget.Width,
        widget.Height
    );
}
