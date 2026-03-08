using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class TeamsAlertRequestValidator : AbstractValidator<TeamsAlertRequest>
{
    private static readonly string[] ValidSeverities = { "Info", "Warning", "Error", "Critical" };

    public TeamsAlertRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(5000).WithMessage("Message must not exceed 5000 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Severity!)
            .Must(s => ValidSeverities.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Severity must be one of: {string.Join(", ", ValidSeverities)}")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Severity));
    }
}
