using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.UseCases.SendSmsNotification;

public record SendSmsNotificationCommand(SendSmsNotificationRequest Request)
    : IRequest<SendSmsNotificationResponse>;