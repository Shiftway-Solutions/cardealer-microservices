using MediatR;
using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.DTOs;

namespace ChatbotService.Application.Features.Sessions.Queries;

/// <summary>
/// Handler para obtener sesión por token
/// </summary>
public class GetSessionByTokenQueryHandler : IRequestHandler<GetSessionByTokenQuery, SessionDto?>
{
    private readonly IChatSessionRepository _sessionRepository;

    public GetSessionByTokenQueryHandler(IChatSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<SessionDto?> Handle(GetSessionByTokenQuery request, CancellationToken ct)
    {
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);
        if (session == null) return null;

        return new SessionDto
        {
            SessionId = session.Id,
            SessionToken = session.SessionToken,
            Status = session.Status.ToString(),
            MessageCount = session.MessageCount,
            InteractionCount = session.InteractionCount,
            MaxInteractionsPerSession = session.MaxInteractionsPerSession,
            RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt
        };
    }
}

/// <summary>
/// Handler para obtener mensajes de una sesión
/// </summary>
public class GetSessionMessagesQueryHandler : IRequestHandler<GetSessionMessagesQuery, List<MessageDto>>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;

    public GetSessionMessagesQueryHandler(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
    }

    public async Task<List<MessageDto>> Handle(GetSessionMessagesQuery request, CancellationToken ct)
    {
        // First get session by token to get the session ID
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);
        if (session == null) return new List<MessageDto>();

        var messages = await _messageRepository.GetBySessionIdAsync(session.Id, ct);
        
        return messages.Select(m => new MessageDto
        {
            Id = m.Id,
            Content = m.Content,
            Response = m.BotResponse,
            IsFromBot = m.IsFromBot,
            IntentName = m.IntentName,
            IsFallback = m.IntentCategory == Domain.Enums.IntentCategory.Fallback,
            CreatedAt = m.CreatedAt
        }).ToList();
    }
}

/// <summary>
/// Handler para obtener sesiones de un usuario
/// </summary>
public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, List<SessionDto>>
{
    private readonly IChatSessionRepository _sessionRepository;

    public GetUserSessionsQueryHandler(IChatSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<List<SessionDto>> Handle(GetUserSessionsQuery request, CancellationToken ct)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(request.UserId, ct);
        
        return sessions.Select(s => new SessionDto
        {
            SessionId = s.Id,
            SessionToken = s.SessionToken,
            Status = s.Status.ToString(),
            MessageCount = s.MessageCount,
            InteractionCount = s.InteractionCount,
            MaxInteractionsPerSession = s.MaxInteractionsPerSession,
            RemainingInteractions = s.MaxInteractionsPerSession - s.InteractionCount,
            CreatedAt = s.CreatedAt,
            LastActivityAt = s.LastActivityAt
        }).ToList();
    }
}

/// <summary>
/// Handler para obtener estadísticas del chatbot
/// </summary>
public class GetChatbotStatsQueryHandler : IRequestHandler<GetChatbotStatsQuery, ChatbotStatsDto>
{
    private readonly IChatSessionRepository _sessionRepository;

    public GetChatbotStatsQueryHandler(IChatSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<ChatbotStatsDto> Handle(GetChatbotStatsQuery request, CancellationToken ct)
    {
        var todaySessions = await _sessionRepository.GetTodaySessionCountAsync(request.ConfigurationId, ct);

        return new ChatbotStatsDto
        {
            TodaySessions = todaySessions,
            TodayMessages = 0,
            TodayLeads = 0,
            AverageSessionDuration = 0,
            AverageMessagesPerSession = 0
        };
    }
}

/// <summary>
/// Handler para obtener uso de interacciones
/// </summary>
public class GetInteractionUsageQueryHandler : IRequestHandler<GetInteractionUsageQuery, InteractionUsageDto>
{
    private readonly IChatbotConfigurationRepository _configRepository;
    private readonly IChatMessageRepository _messageRepository;

    public GetInteractionUsageQueryHandler(
        IChatbotConfigurationRepository configRepository,
        IChatMessageRepository messageRepository)
    {
        _configRepository = configRepository;
        _messageRepository = messageRepository;
    }

    public async Task<InteractionUsageDto> Handle(GetInteractionUsageQuery request, CancellationToken ct)
    {
        var config = await _configRepository.GetByIdAsync(request.ConfigurationId, ct);
        if (config == null)
        {
            throw new InvalidOperationException("Configuration not found");
        }

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        
        var todayMessages = await _messageRepository.GetLlmCallsCountAsync(request.ConfigurationId, today, today.AddDays(1), ct);
        var monthMessages = await _messageRepository.GetLlmCallsCountAsync(request.ConfigurationId, monthStart, today.AddDays(1), ct);

        var freeInteractionsPerMonth = config.FreeInteractionsPerMonth;
        var costPerInteraction = config.CostPerInteraction;
        
        var paidInteractions = Math.Max(0, monthMessages - freeInteractionsPerMonth);
        var totalCost = paidInteractions * costPerInteraction;
        var overageCost = paidInteractions > 0
            ? paidInteractions * config.OverageCostPerConversation
            : 0m;

        return new InteractionUsageDto
        {
            Today = todayMessages,
            Month = monthMessages,
            FreeRemaining = Math.Max(0, freeInteractionsPerMonth - monthMessages),
            PaidToDate = paidInteractions,
            TotalCostToDate = totalCost,
            OverageCost = overageCost
        };
    }
}

/// <summary>
/// Handler para obtener salud del chatbot
/// </summary>
public class GetChatbotHealthQueryHandler : IRequestHandler<GetChatbotHealthQuery, ChatbotHealthDto>
{
    private readonly IHealthMonitoringService _healthService;
    private readonly ILogger<GetChatbotHealthQueryHandler> _logger;

    public GetChatbotHealthQueryHandler(
        IHealthMonitoringService healthService,
        ILogger<GetChatbotHealthQueryHandler> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    public async Task<ChatbotHealthDto> Handle(GetChatbotHealthQuery request, CancellationToken ct)
    {
        try
        {
            var report = await _healthService.GenerateHealthReportAsync(request.ConfigurationId, ct);

            return new ChatbotHealthDto
            {
                ConfigurationId = request.ConfigurationId,
                OverallStatus = report.OverallStatus.ToString(),
                LlmConnected = report.LlmStatus.IsConnected,
                DatabaseConnected = report.DatabaseStatus.IsConnected,
                ActiveSessions = (int)report.DatabaseStatus.ActiveSessions,
                TotalSessions = (int)report.DatabaseStatus.TotalSessions,
                Alerts = report.Alerts.Select(a => new HealthAlertDto
                {
                    Type = a.AlertType,
                    Severity = a.Severity,
                    Message = a.Message,
                    DetectedAt = a.DetectedAt
                }).ToList(),
                GeneratedAt = report.GeneratedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate health report");
            return new ChatbotHealthDto
            {
                ConfigurationId = request.ConfigurationId,
                OverallStatus = "Unknown",
                LlmConnected = false,
                DatabaseConnected = true,
                Alerts = new List<HealthAlertDto>
                {
                    new HealthAlertDto
                    {
                        Type = "HealthCheckFailed",
                        Severity = "Error",
                        Message = ex.Message,
                        DetectedAt = DateTime.UtcNow
                    }
                },
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}
