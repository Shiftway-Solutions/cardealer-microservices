using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class CreateContentReportRequestValidator : AbstractValidator<CreateContentReportRequest>
{
    private static readonly string[] ValidTypes = { "Vehicle", "User", "Message", "Dealer" };
    private static readonly string[] ValidPriorities = { "Low", "Medium", "High" };

    public CreateContentReportRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Content report type is required.")
            .Must(t => ValidTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Content report type must be one of: {string.Join(", ", ValidTypes)}")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("Target ID is required.")
            .MaximumLength(100).WithMessage("Target ID must not exceed 100 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.TargetTitle)
            .NotEmpty().WithMessage("Target title is required.")
            .MaximumLength(500).WithMessage("Target title must not exceed 500 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.ReportedById)
            .NotEmpty().WithMessage("Reporter ID is required.")
            .MaximumLength(100).WithMessage("Reporter ID must not exceed 100 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.ReportedByEmail)
            .NotEmpty().WithMessage("Reporter email is required.")
            .EmailAddress().WithMessage("Reporter email must be a valid email address.")
            .MaximumLength(320).WithMessage("Reporter email must not exceed 320 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Priority!)
            .Must(p => ValidPriorities.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Priority));
    }
}
