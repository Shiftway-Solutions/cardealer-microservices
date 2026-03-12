using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.DTOs;

public record GetNotificationStatusRequest(Guid NotificationId);

public record GetNotificationStatusQuery(Guid NotificationId)
    : IRequest<GetNotificationStatusResponse>;