using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.UseCases.SendEmailNotification;

public record SendEmailNotificationCommand(SendEmailNotificationRequest Request)
    : IRequest<SendEmailNotificationResponse>;