using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequest>
{
    public UpdateReportRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Report name is required.")
            .MaximumLength(200).WithMessage("Report name must not exceed 200 characters.")
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
