using FluentValidation;
using ErrorService.Application.Validators;

namespace ErrorService.Application.UseCases.GetErrors;

/// <summary>
/// Validator for GetErrorsQuery.
/// Validates ServiceName filter with NoSqlInjection/NoXss.
/// </summary>
public class GetErrorsQueryValidator : AbstractValidator<GetErrorsQuery>
{
    public GetErrorsQueryValidator()
    {
        RuleFor(x => x.Request.ServiceName)
            .MaximumLength(100)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.ServiceName));

        RuleFor(x => x.Request.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.Request.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Request)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From date cannot be after To date.");
    }
}
