using MediatR;

namespace AdminService.Application.UseCases.Messages;

public record GetAdminMessagesQuery(
    string? Search = null,
    string? Status = null,
    string? Priority = null
) : IRequest<AdminMessagesResponse>;

public record MarkMessageReadCommand(string MessageId) : IRequest;
public record ReplyToMessageCommand(string MessageId, string Message) : IRequest;
