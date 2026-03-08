using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class UpdateDashboardRequestValidator : AbstractValidator<UpdateDashboardRequest>
{
    public UpdateDashboardRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dashboard name is required.")
            .MaximumLength(200).WithMessage("Dashboard name must not exceed 200 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Layout)
            .MaximumLength(5000).WithMessage("Layout definition must not exceed 5000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Layout));
    }
}
