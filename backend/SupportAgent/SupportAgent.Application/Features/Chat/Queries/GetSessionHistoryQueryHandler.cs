using MediatR;
using Microsoft.Extensions.Logging;
using SupportAgent.Application.DTOs;
using SupportAgent.Domain.Interfaces;

namespace SupportAgent.Application.Features.Chat.Queries;

public class GetSessionHistoryQueryHandler : IRequestHandler<GetSessionHistoryQuery, SessionHistoryResponse?>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ILogger<GetSessionHistoryQueryHandler> _logger;

    public GetSessionHistoryQueryHandler(
        IChatSessionRepository sessionRepository,
        ILogger<GetSessionHistoryQueryHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<SessionHistoryResponse?> Handle(GetSessionHistoryQuery request, CancellationToken ct)
    {
        var session = await _sessionRepository.GetBySessionIdAsync(request.SessionId, ct);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return null;
        }

        var messages = await _sessionRepository.GetMessagesAsync(session.Id, 50, ct);

        return new SessionHistoryResponse
        {
            SessionId = session.SessionId,
            Messages = messages.OrderBy(m => m.CreatedAt).Select(m => new MessageDto
            {
                Role = m.Role,
                Content = m.Content,
                DetectedModule = m.DetectedModule,
                CreatedAt = m.CreatedAt
            }).ToList(),
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt
        };
    }
}
