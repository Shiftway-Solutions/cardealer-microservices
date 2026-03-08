using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportsService.Application.DTOs;
using ReportsService.Domain.Entities;
using ReportsService.Domain.Interfaces;

namespace ReportsService.Api.Controllers;

/// <summary>
/// Content moderation reports — user-submitted reports flagging
/// vehicles, users, messages, or dealers for admin review.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContentReportsController : ControllerBase
{
    private readonly IContentReportRepository _repository;
    private readonly ILogger<ContentReportsController> _logger;

    public ContentReportsController(
        IContentReportRepository repository,
        ILogger<ContentReportsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated content reports with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedContentReportResponse>> GetReports(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        ContentReportType? parsedType = null;
        ContentReportStatus? parsedStatus = null;
        ContentReportPriority? parsedPriority = null;

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<ContentReportType>(type, true, out var t))
            parsedType = t;

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContentReportStatus>(status, true, out var s))
            parsedStatus = s;

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<ContentReportPriority>(priority, true, out var p))
            parsedPriority = p;

        var (items, total) = await _repository.GetPaginatedAsync(
            parsedType, parsedStatus, parsedPriority, search, page, pageSize, cancellationToken);

        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var response = new PaginatedContentReportResponse(
            Items: items.Select(MapToDto).ToList(),
            Total: total,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages);

        return Ok(response);
    }

    /// <summary>
    /// Get content report statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ContentReportStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _repository.GetStatsAsync(cancellationToken);

        return Ok(new ContentReportStatsDto(
            stats.Total,
            stats.Pending,
            stats.Investigating,
            stats.Resolved,
            stats.Dismissed,
            stats.HighPriority));
    }

    /// <summary>
    /// Get a single content report by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentReportDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        return Ok(MapToDto(report));
    }

    /// <summary>
    /// Create a new content report (user flags content).
    /// If the same user already reported this target, increment the count.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContentReportDto>> Create(
        [FromBody] CreateContentReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ContentReportType>(request.Type, true, out var reportType))
            return BadRequest("Invalid report type. Valid values: Vehicle, User, Message, Dealer");

        // Check for existing report from same user on same target
        if (Guid.TryParse(request.ReportedById, out var reportedById))
        {
            var existing = await _repository.FindByTargetAndReporterAsync(
                request.TargetId, reportedById, cancellationToken);

            if (existing != null)
            {
                existing.IncrementReportCount();
                await _repository.UpdateAsync(existing, cancellationToken);
                return Ok(MapToDto(existing));
            }
        }
        else
        {
            reportedById = Guid.Empty;
        }

        var priority = ContentReportPriority.Medium;
        if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<ContentReportPriority>(request.Priority, true, out var p))
            priority = p;

        var report = new ContentReport(
            reportType,
            request.TargetId,
            request.TargetTitle,
            request.Reason,
            request.Description,
            reportedById,
            request.ReportedByEmail,
            priority);

        var created = await _repository.AddAsync(report, cancellationToken);
        _logger.LogInformation(
            "Content report {ReportId} created for {Type} target {TargetId} by {ReportedByEmail}",
            created.Id, reportType, request.TargetId, request.ReportedByEmail);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>
    /// Update content report status (admin action).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentReportDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateContentReportStatusRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        if (!Enum.TryParse<ContentReportStatus>(request.Status, true, out var newStatus))
            return BadRequest("Invalid status. Valid values: Pending, Investigating, Resolved, Dismissed");

        report.SetStatus(newStatus, request.Resolution, request.ResolvedById);
        await _repository.UpdateAsync(report, cancellationToken);

        _logger.LogInformation(
            "Content report {ReportId} status updated to {Status}",
            id, newStatus);

        return Ok(MapToDto(report));
    }

    /// <summary>
    /// Delete a content report.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(id, cancellationToken);
        if (!exists)
            return NotFound();

        await _repository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Content report {ReportId} deleted", id);

        return NoContent();
    }

    private static ContentReportDto MapToDto(ContentReport report) => new(
        report.Id,
        report.Type.ToString().ToLower(),
        report.TargetId,
        report.TargetTitle,
        report.Reason,
        report.Description,
        report.ReportedById.ToString(),
        report.ReportedByEmail,
        report.Status.ToString().ToLower(),
        report.Priority.ToString().ToLower(),
        report.CreatedAt,
        report.ResolvedAt,
        report.ResolvedById,
        report.Resolution,
        report.ReportCount);
}
