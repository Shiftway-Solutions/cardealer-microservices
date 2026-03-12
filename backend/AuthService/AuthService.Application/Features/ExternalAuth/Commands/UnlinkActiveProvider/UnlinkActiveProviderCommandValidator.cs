using FluentValidation;
using AuthService.Application.Validators;

namespace AuthService.Application.Features.ExternalAuth.Commands.UnlinkActiveProvider;

/// <summary>
/// Validator for UnlinkActiveProviderCommand (AUTH-EXT-008)
/// </summary>
public class UnlinkActiveProviderCommandValidator : AbstractValidator<UnlinkActiveProviderCommand>
{
    private static readonly string[] ValidProviders = { "Google", "Microsoft", "Facebook", "Apple" };

    public UnlinkActiveProviderCommandValidator()
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

        RuleFor(x => x.VerificationCode)
            .NotEmpty()
            .WithMessage("Verification code is required.")
            .Length(6)
            .WithMessage("Verification code must be 6 digits.")
            .Matches("^[0-9]{6}$")
            .WithMessage("Verification code must contain only digits.")
            .NoSqlInjection()
            .NoXss();
    }

    private static bool BeValidProvider(string provider)
    {
        return !string.IsNullOrEmpty(provider) &&
               ValidProviders.Any(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }
}
