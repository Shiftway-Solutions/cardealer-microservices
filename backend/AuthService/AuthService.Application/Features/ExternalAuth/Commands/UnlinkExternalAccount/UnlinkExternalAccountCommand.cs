using MediatR;

namespace AuthService.Application.Features.ExternalAuth.Commands.UnlinkExternalAccount;

/// <summary>
/// Command to unlink an external OAuth provider from a user account (AUTH-EXT-006)
/// </summary>
/// <param name="UserId">The ID of the user requesting the unlink</param>
/// <param name="Provider">The OAuth provider to unlink (Google, Microsoft, Facebook, Apple)</param>
public record UnlinkExternalAccountCommand(string UserId, string Provider)
    : IRequest<UnlinkExternalAccountResponse>;

/// <summary>
/// Response from unlinking an external account
/// </summary>
/// <param name="Success">Whether the operation was successful</param>
/// <param name="Message">Human-readable success/error message</param>
/// <param name="Provider">The provider that was unlinked</param>
/// <param name="UnlinkedAt">When the account was unlinked</param>
public record UnlinkExternalAccountResponse(
    bool Success,
    string Message,
    string Provider,
    DateTime UnlinkedAt
);
