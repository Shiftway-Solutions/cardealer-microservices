using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class UpdateReportScheduleRequestValidator : AbstractValidator<UpdateReportScheduleRequest>
{
    public UpdateReportScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Schedule name is required.")
            .MaximumLength(200).WithMessage("Schedule name must not exceed 200 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.ExecutionTime)
            .MaximumLength(20).WithMessage("Execution time must not exceed 20 characters.")
            .Matches(@"^\d{2}:\d{2}(:\d{2})?$").WithMessage("Execution time must be in HH:mm or HH:mm:ss format.")
            .When(x => !string.IsNullOrWhiteSpace(x.ExecutionTime));

        RuleFor(x => x.DayOfWeek)
            .MaximumLength(20).WithMessage("Day of week must not exceed 20 characters.")
            .Must(d => d == null || Enum.TryParse<System.DayOfWeek>(d, true, out _))
            .WithMessage("Day of week must be a valid day name (e.g., Monday, Tuesday, etc.).")
            .When(x => !string.IsNullOrWhiteSpace(x.DayOfWeek));

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31).WithMessage("Day of month must be between 1 and 31.")
            .When(x => x.DayOfMonth.HasValue);

        RuleFor(x => x.Recipients)
            .MaximumLength(2000).WithMessage("Recipients must not exceed 2000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Recipients));
    }
}
