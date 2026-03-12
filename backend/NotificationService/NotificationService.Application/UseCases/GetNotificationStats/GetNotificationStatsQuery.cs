using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.UseCases.GetNotificationStats;

public record GetNotificationStatsQuery(GetNotificationStatsRequest Request)
    : IRequest<GetNotificationStatsResponse>;