using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.UseCases.GetNotifications;

public record GetNotificationsQuery(GetNotificationsRequest Request)
    : IRequest<GetNotificationsResponse>;