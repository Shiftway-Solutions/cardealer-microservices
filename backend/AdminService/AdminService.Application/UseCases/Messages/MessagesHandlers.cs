using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminService.Application.UseCases.Messages;

public class GetAdminMessagesQueryHandler : IRequestHandler<GetAdminMessagesQuery, AdminMessagesResponse>
{
    private readonly ILogger<GetAdminMessagesQueryHandler> _logger;

    public GetAdminMessagesQueryHandler(ILogger<GetAdminMessagesQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<AdminMessagesResponse> Handle(GetAdminMessagesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting admin messages search={Search} status={Status}", request.Search, request.Status);

        // Return empty inbox - no messages service exists yet
        // Future: integrate with ContactService or a dedicated MessagesService
        var items = new List<AdminMessageDto>();
        return Task.FromResult(new AdminMessagesResponse(items, 0));
    }
}

public class MarkMessageReadCommandHandler : IRequestHandler<MarkMessageReadCommand>
{
    private readonly ILogger<MarkMessageReadCommandHandler> _logger;

    public MarkMessageReadCommandHandler(ILogger<MarkMessageReadCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(MarkMessageReadCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Marking message {MessageId} as read", request.MessageId);
        return Task.CompletedTask;
    }
}

public class ReplyToMessageCommandHandler : IRequestHandler<ReplyToMessageCommand>
{
    private readonly ILogger<ReplyToMessageCommandHandler> _logger;

    public ReplyToMessageCommandHandler(ILogger<ReplyToMessageCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ReplyToMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Replying to message {MessageId}", request.MessageId);
        return Task.CompletedTask;
    }
}
