using AuthService.Domain.Enums;
using FluentValidation;
using AuthService.Application.Validators;

namespace AuthService.Application.Features.ExternalAuth.Commands.UnlinkExternalAccount;

/// <summary>
/// Validator for UnlinkExternalAccountCommand (AUTH-EXT-006)
/// Validates that the provider is supported.
/// </summary>
public class UnlinkExternalAccountCommandValidator : AbstractValidator<UnlinkExternalAccountCommand>
{
    private static readonly string[] SupportedProviders =
        Enum.GetNames<ExternalAuthProvider>();

    public UnlinkExternalAccountCommandValidator()
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
