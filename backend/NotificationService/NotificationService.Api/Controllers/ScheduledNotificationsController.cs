using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/notifications/scheduled")]
[Authorize(Roles = "admin,notification-admin")]
public class ScheduledNotificationsController : ControllerBase
{
    private readonly IScheduledNotificationRepository _repository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<ScheduledNotificationsController> _logger;

    public ScheduledNotificationsController(
        IScheduledNotificationRepository repository,
        INotificationRepository notificationRepository,
        ISchedulingService schedulingService,
        ILogger<ScheduledNotificationsController> logger)
    {
        _repository = repository;
        _notificationRepository = notificationRepository;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ScheduleNotification([FromBody] ScheduleNotificationRequest request)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId);
            if (notification == null)
                return NotFound(new { error = "Notification not found" });

            ScheduledNotification scheduled;

            if (!string.IsNullOrWhiteSpace(request.CronExpression))
            {
                scheduled = await _schedulingService.ScheduleWithCronAsync(
                    notification, request.CronExpression, request.TimeZone, request.MaxExecutions, User.Identity?.Name);
            }
            else if (request.IsRecurring && request.RecurrenceType.HasValue)
            {
                scheduled = await _schedulingService.ScheduleRecurringAsync(
                    notification, request.ScheduledFor, request.RecurrenceType.Value, request.TimeZone, request.MaxExecutions, User.Identity?.Name);
            }
            else
            {
                scheduled = await _schedulingService.ScheduleOneTimeAsync(
                    notification, request.ScheduledFor, request.TimeZone, User.Identity?.Name);
            }

            return CreatedAtAction(nameof(GetScheduledNotification), new { id = scheduled.Id }, MapToResponse(scheduled));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetScheduledNotification(Guid id)
    {
        var scheduled = await _repository.GetByIdAsync(id);
        if (scheduled == null)
            return NotFound();

        return Ok(MapToResponse(scheduled));
    }

    [HttpGet]
    public async Task<IActionResult> GetScheduledNotifications([FromQuery] GetScheduledNotificationsRequest request)
    {
        IEnumerable<ScheduledNotification> scheduled;

        if (request.Status.HasValue)
        {
            scheduled = await _repository.GetByStatusAsync(request.Status.Value);
        }
        else if (request.ScheduledFrom.HasValue && request.ScheduledTo.HasValue)
        {
            scheduled = await _repository.GetScheduledForDateRangeAsync(request.ScheduledFrom.Value, request.ScheduledTo.Value);
        }
        else
        {
            scheduled = await _repository.GetAllAsync();
        }

        if (request.IsRecurring.HasValue)
        {
            scheduled = scheduled.Where(s => s.IsRecurring == request.IsRecurring.Value);
        }

        var totalCount = scheduled.Count();
        scheduled = scheduled.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);

        var response = new GetScheduledNotificationsResponse(
            scheduled.Select(MapToResponse).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize
        );

        return Ok(response);
    }

    [HttpPut("{id}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] DateTime newScheduledFor)
    {
        var success = await _schedulingService.RescheduleAsync(id, newScheduledFor);
        if (!success)
            return NotFound();

        var scheduled = await _repository.GetByIdAsync(id);
        return Ok(MapToResponse(scheduled!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelScheduledNotification(Guid id, [FromQuery] string? reason = null)
    {
        var success = await _schedulingService.CancelAsync(id, reason ?? "Cancelled by user", User.Identity?.Name);
        if (!success)
            return NotFound();

        return NoContent();
    }

    private ScheduledNotificationResponse MapToResponse(ScheduledNotification scheduled)
    {
        return new ScheduledNotificationResponse(
            scheduled.Id,
            scheduled.NotificationId,
            scheduled.ScheduledFor,
            scheduled.TimeZone,
            scheduled.Status,
            scheduled.IsRecurring,
            scheduled.RecurrenceType,
            scheduled.CronExpression,
            scheduled.NextExecution,
            scheduled.LastExecution,
            scheduled.ExecutionCount,
            scheduled.MaxExecutions,
            scheduled.CreatedAt,
            scheduled.CreatedBy
        );
    }
}
