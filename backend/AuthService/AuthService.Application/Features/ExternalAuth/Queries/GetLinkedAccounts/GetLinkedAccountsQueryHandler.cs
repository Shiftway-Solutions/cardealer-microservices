using AuthService.Application.DTOs.ExternalAuth;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.ExternalAuth.Queries.GetLinkedAccounts;

public class GetLinkedAccountsQueryHandler : IRequestHandler<GetLinkedAccountsQuery, List<LinkedAccountResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetLinkedAccountsQueryHandler> _logger;

    public GetLinkedAccountsQueryHandler(
        IUserRepository userRepository,
        ILogger<GetLinkedAccountsQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<List<LinkedAccountResponse>> Handle(GetLinkedAccountsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting linked accounts for user {UserId}", request.UserId);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return new List<LinkedAccountResponse>();
        }

        var linkedAccounts = new List<LinkedAccountResponse>();

        // Check if user has an external account linked
        if (user.IsExternalUser && user.ExternalAuthProvider.HasValue)
        {
            linkedAccounts.Add(new LinkedAccountResponse(
                Provider: user.ExternalAuthProvider.Value.ToString(),
                ExternalUserId: user.ExternalUserId ?? string.Empty,
                Email: user.Email ?? string.Empty,
                LinkedAt: user.UpdatedAt ?? user.CreatedAt
            ));

            _logger.LogInformation(
                "Found linked account for user {UserId}: {Provider}",
                request.UserId,
                user.ExternalAuthProvider.Value);
        }
        else
        {
            _logger.LogInformation("No linked accounts found for user {UserId}", request.UserId);
        }

        return linkedAccounts;
    }
}
