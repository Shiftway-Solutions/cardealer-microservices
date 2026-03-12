using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.UseCases.SendPushNotification;

public record SendPushNotificationCommand(SendPushNotificationRequest Request)
    : IRequest<SendPushNotificationResponse>;