using FluentValidation;
using AuthService.Application.Validators;

namespace AuthService.Application.Features.ExternalAuth.Commands.ValidateUnlinkAccount;

/// <summary>
/// Validator for ValidateUnlinkAccountCommand (AUTH-EXT-008)
/// </summary>
public class ValidateUnlinkAccountCommandValidator : AbstractValidator<ValidateUnlinkAccountCommand>
{
    private static readonly string[] ValidProviders = { "Google", "Microsoft", "Facebook", "Apple" };

    public ValidateUnlinkAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required.")
            .Must(BeValidProvider)
            .WithMessage("Invalid provider. Supported providers: Google, Microsoft, Facebook, Apple.")
            .NoSqlInjection()
            .NoXss();
    }

    private static bool BeValidProvider(string provider)
    {
        return !string.IsNullOrEmpty(provider) &&
               ValidProviders.Any(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }
}
