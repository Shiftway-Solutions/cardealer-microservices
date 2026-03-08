using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class GenerateReportRequestValidator : AbstractValidator<GenerateReportRequest>
{
    public GenerateReportRequestValidator()
    {
        RuleFor(x => x.Parameters)
            .MaximumLength(2000).WithMessage("Parameters must not exceed 2000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Parameters));
    }
}
