using FluentValidation;
using UserService.Application.Validators;

namespace UserService.Application.UseCases.Users.GetOrCreateUser;

public class GetOrCreateUserCommandValidator : AbstractValidator<GetOrCreateUserCommand>
{
    public GetOrCreateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(254)
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.AvatarUrl!)
            .MaximumLength(500)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
    }
}
