using FluentValidation;

namespace ErrorService.Application.UseCases.GetError;

/// <summary>
/// Validator for GetErrorQuery — ensures the error ID is valid.
/// </summary>
public class GetErrorQueryValidator : AbstractValidator<GetErrorQuery>
{
    public GetErrorQueryValidator()
    {
        RuleFor(x => x.Request.Id)
            .NotEmpty().WithMessage("Error ID is required.");
    }
}
