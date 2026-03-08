using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class UpdateWidgetRequestValidator : AbstractValidator<UpdateWidgetRequest>
{
    public UpdateWidgetRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Widget title must not exceed 200 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.PositionX)
            .GreaterThanOrEqualTo(0).WithMessage("Position X must be non-negative.")
            .When(x => x.PositionX.HasValue);

        RuleFor(x => x.PositionY)
            .GreaterThanOrEqualTo(0).WithMessage("Position Y must be non-negative.")
            .When(x => x.PositionY.HasValue);

        RuleFor(x => x.Width)
            .InclusiveBetween(1, 24).WithMessage("Width must be between 1 and 24.")
            .When(x => x.Width.HasValue);

        RuleFor(x => x.Height)
            .InclusiveBetween(1, 24).WithMessage("Height must be between 1 and 24.")
            .When(x => x.Height.HasValue);

        RuleFor(x => x.DataSource)
            .MaximumLength(1000).WithMessage("Data source must not exceed 1000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.DataSource));

        RuleFor(x => x.Configuration)
            .MaximumLength(5000).WithMessage("Configuration must not exceed 5000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Configuration));
    }
}
