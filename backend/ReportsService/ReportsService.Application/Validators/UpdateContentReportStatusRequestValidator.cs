using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class UpdateContentReportStatusRequestValidator : AbstractValidator<UpdateContentReportStatusRequest>
{
    private static readonly string[] ValidStatuses = { "Pending", "Investigating", "Resolved", "Dismissed" };

    public UpdateContentReportStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Resolution)
            .MaximumLength(2000).WithMessage("Resolution must not exceed 2000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Resolution));

        RuleFor(x => x.ResolvedById)
            .MaximumLength(100).WithMessage("Resolver ID must not exceed 100 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.ResolvedById));
    }
}
