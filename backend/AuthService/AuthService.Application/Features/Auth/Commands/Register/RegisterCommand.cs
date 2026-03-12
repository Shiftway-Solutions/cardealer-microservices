using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command for user registration.
/// Accepts firstName and lastName from frontend and constructs UserName internally.
/// Also accepts optional phone and acceptTerms for validation.
/// AccountType defaults to Buyer. Sellers can register directly with AccountType.Seller.
/// </summary>
public record RegisterCommand(
    string? UserName,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    bool AcceptTerms = true,
    AccountType AccountType = AccountType.Buyer,
    UserIntent UserIntent = UserIntent.Browse
) : IRequest<RegisterResponse>
{
    /// <summary>
    /// Gets the display name for the user.
    /// Uses FirstName + LastName if provided, otherwise falls back to UserName or Email prefix.
    /// </summary>
    public string GetDisplayName() =>
        !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName)
            ? $"{FirstName.Trim()} {LastName.Trim()}"
            : !string.IsNullOrWhiteSpace(UserName)
                ? UserName
                : Email.Split('@')[0];
}
