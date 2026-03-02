using MediatR;
using SupportAgent.Application.DTOs;

namespace SupportAgent.Application.Features.Chat.Queries;

public record GetSessionHistoryQuery(string SessionId) : IRequest<SessionHistoryResponse?>;
