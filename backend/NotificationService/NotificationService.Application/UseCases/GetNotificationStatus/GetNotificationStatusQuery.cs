using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.UseCases.GetNotificationStatus;

public record GetNotificationStatusQuery(Guid NotificationId)
    : IRequest<GetNotificationStatusResponse>;