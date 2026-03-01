namespace AdminService.Application.UseCases.Messages;

public record AdminMessageDto(
    string Id,
    string SenderName,
    string SenderEmail,
    string SenderType,
    string Subject,
    string Preview,
    string Status,
    string Priority,
    string Category,
    string CreatedAt,
    int MessagesCount,
    bool HasAttachments
);

public record AdminMessagesResponse(
    List<AdminMessageDto> Items,
    int Total
);
