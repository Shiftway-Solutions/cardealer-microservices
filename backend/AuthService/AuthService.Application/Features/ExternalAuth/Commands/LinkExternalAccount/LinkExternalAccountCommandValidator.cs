using AuthService.Domain.Enums;
using FluentValidation;
using AuthService.Application.Validators;

namespace AuthService.Application.Features.ExternalAuth.Commands.LinkExternalAccount;

/// <summary>
/// Validator for LinkExternalAccountCommand (AUTH-EXT-005)
/// Validates that the provider is supported and the ID token is valid.
/// </summary>
public class LinkExternalAccountCommandValidator : AbstractValidator<LinkExternalAccountCommand>
{
    private static readonly string[] SupportedProviders =
        Enum.GetNames<ExternalAuthProvider>();

    public LinkExternalAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .Must(BeValidGuid).WithMessage("User ID must be a valid GUID")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required")
            .Must(BeValidProvider)
            .WithMessage($"Provider must be one of: {string.Join(", ", SupportedProviders)}")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required")
            .MinimumLength(50).WithMessage("ID token is too short to be valid")
            .MaximumLength(10000).WithMessage("ID token is too long")
            .NoSqlInjection()
            .NoXss();
    }

    private static bool BeValidGuid(string userId)
    {
        return Guid.TryParse(userId, out _);
    }

    private static bool BeValidProvider(string provider)
    {
        return Enum.TryParse<ExternalAuthProvider>(provider, true, out _);
    }
}
