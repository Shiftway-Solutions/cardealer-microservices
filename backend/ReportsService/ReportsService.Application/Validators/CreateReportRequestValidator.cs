using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    private static readonly string[] ValidTypes = { "Sales", "Inventory", "Financial", "CRM", "Marketing", "Custom" };
    private static readonly string[] ValidFormats = { "Pdf", "Excel", "Csv", "Html", "Json" };

    public CreateReportRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Report name is required.")
            .MaximumLength(200).WithMessage("Report name must not exceed 200 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Report type is required.")
            .Must(t => ValidTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Report type must be one of: {string.Join(", ", ValidTypes)}")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Report format is required.")
            .Must(f => ValidFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Report format must be one of: {string.Join(", ", ValidFormats)}")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.QueryDefinition)
            .MaximumLength(5000).WithMessage("Query definition must not exceed 5000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.QueryDefinition));

        RuleFor(x => x.Parameters)
            .MaximumLength(2000).WithMessage("Parameters must not exceed 2000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Parameters));

        RuleFor(x => x.FilterCriteria)
            .MaximumLength(2000).WithMessage("Filter criteria must not exceed 2000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.FilterCriteria));

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
