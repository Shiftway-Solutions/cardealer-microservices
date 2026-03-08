using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportsService.Application.DTOs;
using ReportsService.Domain.Entities;
using ReportsService.Domain.Interfaces;

namespace ReportsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportSchedulesController : ControllerBase
{
    private readonly IReportScheduleRepository _scheduleRepository;
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportSchedulesController> _logger;

    public ReportSchedulesController(
        IReportScheduleRepository scheduleRepository,
        IReportRepository reportRepository,
        ILogger<ReportSchedulesController> logger)
    {
        _scheduleRepository = scheduleRepository;
        _reportRepository = reportRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportScheduleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetAllAsync(cancellationToken);
        return Ok(schedules.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportScheduleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
        if (schedule == null)
            return NotFound();

        return Ok(MapToDto(schedule));
    }

    [HttpGet("report/{reportId:guid}")]
    public async Task<ActionResult<IEnumerable<ReportScheduleDto>>> GetByReportId(Guid reportId, CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetByReportIdAsync(reportId, cancellationToken);
        return Ok(schedules.Select(MapToDto));
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ReportScheduleDto>>> GetActive(CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetActiveAsync(cancellationToken);
        return Ok(schedules.Select(MapToDto));
    }

    [HttpGet("due")]
    public async Task<ActionResult<IEnumerable<ReportScheduleDto>>> GetDue(CancellationToken cancellationToken)
    {
        var schedules = await _scheduleRepository.GetDueAsync(cancellationToken);
        return Ok(schedules.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<ReportScheduleDto>> Create(
        [FromBody] CreateReportScheduleRequest request,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken cancellationToken)
    {
        // Verify report exists
        var reportExists = await _reportRepository.ExistsAsync(request.ReportId, cancellationToken);
        if (!reportExists)
            return BadRequest("Report not found");

        if (!Enum.TryParse<ScheduleFrequency>(request.Frequency, true, out var frequency))
            return BadRequest("Invalid frequency");

        var schedule = new ReportSchedule(
            dealerId,
            request.ReportId,
            request.Name,
            frequency,
            userId);

        if (!string.IsNullOrEmpty(request.ExecutionTime) && TimeOnly.TryParse(request.ExecutionTime, out var executionTime))
            schedule.SetExecutionTime(executionTime);

        if (!string.IsNullOrEmpty(request.DayOfWeek) && Enum.TryParse<DayOfWeek>(request.DayOfWeek, true, out var dayOfWeek))
            schedule.SetWeeklySchedule(dayOfWeek);

        if (request.DayOfMonth.HasValue)
            schedule.SetMonthlySchedule(request.DayOfMonth.Value);

        if (!string.IsNullOrEmpty(request.Recipients))
            schedule.SetRecipients(request.Recipients);

        schedule.SetDeliveryOptions(request.SendEmail, request.SaveToStorage);

        var created = await _scheduleRepository.AddAsync(schedule, cancellationToken);
        _logger.LogInformation("ReportSchedule {ScheduleId} created for report {ReportId}", created.Id, request.ReportId);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportScheduleDto>> Update(Guid id, [FromBody] UpdateReportScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
        if (schedule == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.ExecutionTime) && TimeOnly.TryParse(request.ExecutionTime, out var executionTime))
            schedule.SetExecutionTime(executionTime);

        if (!string.IsNullOrEmpty(request.DayOfWeek) && Enum.TryParse<DayOfWeek>(request.DayOfWeek, true, out var dayOfWeek))
            schedule.SetWeeklySchedule(dayOfWeek);

        if (request.DayOfMonth.HasValue)
            schedule.SetMonthlySchedule(request.DayOfMonth.Value);

        if (!string.IsNullOrEmpty(request.Recipients))
            schedule.SetRecipients(request.Recipients);

        if (request.SendEmail.HasValue || request.SaveToStorage.HasValue)
            schedule.SetDeliveryOptions(request.SendEmail ?? schedule.SendEmail, request.SaveToStorage ?? schedule.SaveToStorage);

        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);
        _logger.LogInformation("ReportSchedule {ScheduleId} updated", id);

        return Ok(MapToDto(schedule));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ReportScheduleDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
        if (schedule == null)
            return NotFound();

        schedule.Activate();
        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);
        _logger.LogInformation("ReportSchedule {ScheduleId} activated", id);

        return Ok(MapToDto(schedule));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ReportScheduleDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
        if (schedule == null)
            return NotFound();

        schedule.Deactivate();
        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);
        _logger.LogInformation("ReportSchedule {ScheduleId} deactivated", id);

        return Ok(MapToDto(schedule));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _scheduleRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
            return NotFound();

        await _scheduleRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("ReportSchedule {ScheduleId} deleted", id);

        return NoContent();
    }

    private static ReportScheduleDto MapToDto(ReportSchedule schedule) => new(
        schedule.Id,
        schedule.ReportId,
        schedule.Name,
        schedule.Frequency.ToString(),
        schedule.ExecutionTime?.ToString(),
        schedule.DayOfWeek?.ToString(),
        schedule.DayOfMonth,
        schedule.IsActive,
        schedule.Recipients,
        schedule.SendEmail,
        schedule.SaveToStorage,
        schedule.LastRunAt,
        schedule.NextRunAt,
        schedule.LastRunStatus,
        schedule.CreatedAt
    );
}
