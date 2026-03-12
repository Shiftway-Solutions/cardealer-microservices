using AuthService.Shared.Exceptions;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository sessionRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new BadRequestException("Refresh token is required.");

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken != null && !storedToken.IsRevoked)
        {
            // Revoke the refresh token
            storedToken.Revoke("logout", "user");
            await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

            // Also revoke the associated user session to keep state consistent
            try
            {
                var activeSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(
                    storedToken.UserId, cancellationToken);

                // Find and revoke session matching this token's IP/creation time
                var matchingSession = activeSessions
                    .FirstOrDefault(s => s.CreatedAt <= storedToken.CreatedAt.AddSeconds(5)
                                      && s.CreatedAt >= storedToken.CreatedAt.AddSeconds(-5));

                if (matchingSession != null)
                {
                    matchingSession.Revoke("User logged out");
                    await _sessionRepository.UpdateAsync(matchingSession, cancellationToken);
                    _logger.LogInformation("Revoked session {SessionId} during logout for user {UserId}",
                        matchingSession.Id, storedToken.UserId);
                }
            }
            catch (Exception ex)
            {
                // Non-critical: log but don't fail the logout
                _logger.LogWarning(ex, "Failed to revoke associated session during logout for user {UserId}",
                    storedToken.UserId);
            }
        }

        return Unit.Value;
    }
}
