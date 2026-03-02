using MediatR;
using SupportAgent.Application.DTOs;

namespace SupportAgent.Application.Features.Chat.Commands;

public record SendMessageCommand(
    string Message,
    string? SessionId,
    string? UserId,
    string? IpAddress) : IRequest<SupportChatResponse>;
