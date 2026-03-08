using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportsService.Application.DTOs;
using ReportsService.Domain.Entities;
using ReportsService.Domain.Interfaces;

namespace ReportsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportRepository reportRepository,
        ILogger<ReportsController> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetAll(CancellationToken cancellationToken)
    {
        var reports = await _reportRepository.GetAllAsync(cancellationToken);
        return Ok(reports.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        return Ok(MapToDto(report));
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetByType(ReportType type, CancellationToken cancellationToken)
    {
        var reports = await _reportRepository.GetByTypeAsync(type, cancellationToken);
        return Ok(reports.Select(MapToDto));
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetByStatus(ReportStatus status, CancellationToken cancellationToken)
    {
        var reports = await _reportRepository.GetByStatusAsync(status, cancellationToken);
        return Ok(reports.Select(MapToDto));
    }

    [HttpGet("ready")]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetReady(CancellationToken cancellationToken)
    {
        var reports = await _reportRepository.GetReadyAsync(cancellationToken);
        return Ok(reports.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<ReportDto>> Create(
        [FromBody] CreateReportRequest request,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ReportType>(request.Type, true, out var reportType))
            return BadRequest("Invalid report type");

        if (!Enum.TryParse<ReportFormat>(request.Format, true, out var reportFormat))
            return BadRequest("Invalid report format");

        var report = new Report(
            dealerId,
            request.Name,
            reportType,
            reportFormat,
            userId,
            request.Description);

        if (request.StartDate.HasValue && request.EndDate.HasValue)
            report.SetDateRange(request.StartDate.Value, request.EndDate.Value);

        if (!string.IsNullOrEmpty(request.QueryDefinition))
            report.SetQueryDefinition(request.QueryDefinition);

        if (!string.IsNullOrEmpty(request.Parameters))
            report.SetParameters(request.Parameters);

        if (!string.IsNullOrEmpty(request.FilterCriteria))
            report.SetFilter(request.FilterCriteria);

        var created = await _reportRepository.AddAsync(report, cancellationToken);
        _logger.LogInformation("Report {ReportId} created for dealer {DealerId}", created.Id, dealerId);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportDto>> Update(Guid id, [FromBody] UpdateReportRequest request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        if (request.StartDate.HasValue && request.EndDate.HasValue)
            report.SetDateRange(request.StartDate.Value, request.EndDate.Value);

        if (!string.IsNullOrEmpty(request.QueryDefinition))
            report.SetQueryDefinition(request.QueryDefinition);

        if (!string.IsNullOrEmpty(request.Parameters))
            report.SetParameters(request.Parameters);

        if (!string.IsNullOrEmpty(request.FilterCriteria))
            report.SetFilter(request.FilterCriteria);

        await _reportRepository.UpdateAsync(report, cancellationToken);
        _logger.LogInformation("Report {ReportId} updated", id);

        return Ok(MapToDto(report));
    }

    [HttpPost("{id:guid}/generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> Generate(Guid id, [FromBody] GenerateReportRequest? request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request?.Parameters))
            report.SetParameters(request.Parameters);

        report.StartGeneration();
        await _reportRepository.UpdateAsync(report, cancellationToken);
        _logger.LogInformation("Report {ReportId} generation started", id);

        return Ok(MapToDto(report));
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> Complete(Guid id, [FromBody] ReportGeneratedRequest request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        report.Complete(request.FilePath, request.FileSize, request.ExpiresAt);
        await _reportRepository.UpdateAsync(report, cancellationToken);
        _logger.LogInformation("Report {ReportId} completed", id);

        return Ok(MapToDto(report));
    }

    [HttpPost("{id:guid}/fail")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> Fail(Guid id, [FromBody] string errorMessage, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        if (report == null)
            return NotFound();

        report.Fail(errorMessage);
        await _reportRepository.UpdateAsync(report, cancellationToken);
        _logger.LogWarning("Report {ReportId} failed: {Error}", id, errorMessage);

        return Ok(MapToDto(report));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _reportRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
            return NotFound();

        await _reportRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Report {ReportId} deleted", id);

        return NoContent();
    }

    private static ReportDto MapToDto(Report report) => new(
        report.Id,
        report.Name,
        report.Description,
        report.Type.ToString(),
        report.Format.ToString(),
        report.Status.ToString(),
        report.StartDate,
        report.EndDate,
        report.FilePath,
        report.FileSize,
        report.ErrorMessage,
        report.GeneratedAt,
        report.ExpiresAt,
        report.CreatedAt
    );
}
