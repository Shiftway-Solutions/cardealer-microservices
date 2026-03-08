using Cronos;
using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class ScheduleNotificationRequestValidator : AbstractValidator<ScheduleNotificationRequest>
{
    /// <summary>
    /// Minimum interval between cron executions to prevent DoS (5 minutes).
    /// </summary>
    private static readonly TimeSpan MinimumCronInterval = TimeSpan.FromMinutes(5);

    public ScheduleNotificationRequestValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("Notification ID is required.");

        RuleFor(x => x.ScheduledFor)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Scheduled time must be in the future.");

        RuleFor(x => x.TimeZone)
            .MaximumLength(100).WithMessage("Timezone must not exceed 100 characters.")
            .NoSqlInjection().NoXss()
            .Must(BeValidTimeZone).WithMessage("Invalid timezone identifier.")
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZone));

        RuleFor(x => x.CronExpression)
            .MaximumLength(100).WithMessage("Cron expression must not exceed 100 characters.")
            .Matches(@"^[\d\s\*\-/,\?LW#]+$").WithMessage("Cron expression contains invalid characters.")
            .NoSqlInjection().NoXss()
            .Must(BeValidCronExpression).WithMessage("Cron expression is not valid. Use standard 5-field cron format (minute hour day month weekday).")
            .Must(NotFireTooFrequently).WithMessage($"Cron expression fires too frequently. Minimum interval between executions is {MinimumCronInterval.TotalMinutes} minutes.")
            .When(x => !string.IsNullOrWhiteSpace(x.CronExpression));

        RuleFor(x => x.MaxExecutions)
            .InclusiveBetween(1, 10000).WithMessage("Max executions must be between 1 and 10000.")
            .When(x => x.MaxExecutions.HasValue);

        // If recurring, must have either a RecurrenceType or CronExpression
        RuleFor(x => x)
            .Must(x => x.RecurrenceType.HasValue || !string.IsNullOrWhiteSpace(x.CronExpression))
            .WithMessage("Recurring notifications must specify either RecurrenceType or CronExpression.")
            .When(x => x.IsRecurring);
    }

    private static bool BeValidCronExpression(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return true;

        try
        {
            CronExpression.Parse(cronExpression);
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }

    private static bool NotFireTooFrequently(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return true;

        try
        {
            var cron = CronExpression.Parse(cronExpression);
            var now = DateTime.UtcNow;
            var first = cron.GetNextOccurrence(now);
            if (first == null) return true;

            var second = cron.GetNextOccurrence(first.Value);
            if (second == null) return true;

            var interval = second.Value - first.Value;
            return interval >= MinimumCronInterval;
        }
        catch (CronFormatException)
        {
            // Invalid cron — let the other validator catch it
            return true;
        }
    }

    private static bool BeValidTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return true;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }
}
