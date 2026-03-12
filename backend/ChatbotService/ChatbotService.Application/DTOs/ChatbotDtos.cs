using ChatbotService.Domain.Enums;

namespace ChatbotService.Application.DTOs;

#region Session DTOs

/// <summary>
/// DTO para iniciar una nueva sesión de chat
/// </summary>
public record StartSessionRequest
{
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserPhone { get; init; }
    public SessionType SessionType { get; init; } = SessionType.WebChat;
    public string Channel { get; init; } = "web";
    public string? ChannelUserId { get; init; }
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceType { get; init; }
    public string? Language { get; init; } = "es";
    public Guid? DealerId { get; init; }

    // Dual-mode support
    public string? ChatMode { get; init; }
    public Guid? VehicleId { get; init; }
}

/// <summary>
/// Respuesta al iniciar sesión
/// </summary>
public record StartSessionResponse
{
    public Guid SessionId { get; init; }
    public string SessionToken { get; init; } = string.Empty;
    public string WelcomeMessage { get; init; } = string.Empty;
    public string BotName { get; init; } = string.Empty;
    public string? BotAvatarUrl { get; init; }
    public List<QuickReplyDto>? InitialQuickReplies { get; init; }
    public int MaxInteractionsPerSession { get; init; }
    public int RemainingInteractions { get; init; }
    public string? ChatMode { get; init; }

    // ══════════════════════════════════════════════════════════════
    // BOT DISCLOSURE — Campos obligatorios de identificación de bot
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Mensaje de disclosure obligatorio que incluye: nombre del dealer,
    /// identificación como asistente virtual de OKLA, y enlace a privacidad.
    /// El frontend DEBE mostrar este mensaje antes de permitir interacción.
    /// </summary>
    public string DisclosureMessage { get; init; } = string.Empty;

    /// <summary>
    /// URL de la política de privacidad (okla.do/privacidad).
    /// </summary>
    public string PrivacyPolicyUrl { get; init; } = string.Empty;

    /// <summary>
    /// Si true, el comprador DEBE aceptar el disclosure antes de enviar mensajes.
    /// El frontend debe mostrar un botón "Acepto" y llamar POST /api/chat/accept-disclosure.
    /// </summary>
    public bool RequiresConsent { get; init; }
}

/// <summary>
/// DTO de sesión de chat
/// </summary>
public record ChatSessionDto
{
    public Guid Id { get; init; }
    public string SessionToken { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public SessionType SessionType { get; init; }
    public string Channel { get; init; } = string.Empty;
    public SessionStatus Status { get; init; }
    public int MessageCount { get; init; }
    public int InteractionCount { get; init; }
    public int MaxInteractionsPerSession { get; init; }
    public bool InteractionLimitReached { get; init; }
    public Guid? CurrentVehicleId { get; init; }
    public string? CurrentVehicleName { get; init; }
    public Guid? LeadId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int SessionDurationSeconds { get; init; }
}

#endregion

#region Message DTOs

/// <summary>
/// Request para enviar un mensaje
/// </summary>
public record SendMessageRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public MessageType Type { get; init; } = MessageType.UserText;
    public string? MediaUrl { get; init; }
}

/// <summary>
/// Respuesta del chatbot
/// </summary>
public record ChatbotResponse
{
    public Guid MessageId { get; init; }
    public string Response { get; init; } = string.Empty;
    public string? IntentName { get; init; }
    public IntentCategory IntentCategory { get; init; }
    public decimal ConfidenceScore { get; init; }
    public ConfidenceLevel ConfidenceLevel { get; init; }
    public List<QuickReplyDto>? QuickReplies { get; init; }
    public VehicleCardDto? VehicleCard { get; init; }
    public List<VehicleCardDto>? VehicleCards { get; init; }
    public bool IsFallback { get; init; }
    public int ResponseTimeMs { get; init; }

    // Modo de chat y handoff
    public string? ChatMode { get; init; }
    public bool IsHumanMode { get; init; }

    // DealerChatAgent — Intent Scoring
    public int IntentScore { get; init; }
    public string? Clasificacion { get; init; }
    public string? ModuloActivo { get; init; }
    public bool HandoffActivado { get; init; }

    // Información de límites
    public int RemainingInteractions { get; init; }
    public bool InteractionLimitReached { get; init; }
    public string? LimitReachedMessage { get; init; }

    // Si se generó lead
    public bool LeadGenerated { get; init; }
    public Guid? LeadId { get; init; }
}

/// <summary>
/// Quick reply button
/// </summary>
public record QuickReplyDto
{
    public string Text { get; init; } = string.Empty;
    public string? Payload { get; init; }
    public string? IconUrl { get; init; }
}

/// <summary>
/// Card de vehículo para mostrar en el chat
/// </summary>
public record VehicleCardDto
{
    public Guid VehicleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? OriginalPrice { get; init; }
    public bool IsOnSale { get; init; }
    public string? DetailsUrl { get; init; }
    public List<string>? Highlights { get; init; }
    public List<QuickReplyDto>? Actions { get; init; }
}

/// <summary>
/// DTO de mensaje individual
/// </summary>
public record ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public MessageType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? MediaUrl { get; init; }
    public string? IntentName { get; init; }
    public IntentCategory IntentCategory { get; init; }
    public decimal ConfidenceScore { get; init; }
    public string? BotResponse { get; init; }
    public bool IsFromBot { get; init; }
    public int? ResponseTimeMs { get; init; }
    public bool ConsumedInteraction { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion

#region Lead DTOs

/// <summary>
/// Request para capturar lead
/// </summary>
public record CaptureLeadRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? WhatsApp { get; init; }
    public string? PreferredContactMethod { get; init; }
    public string? PreferredContactTime { get; init; }
    public Guid? InterestedVehicleId { get; init; }
    public decimal? BudgetMin { get; init; }
    public decimal? BudgetMax { get; init; }
    public bool InterestedInFinancing { get; init; }
    public bool InterestedInTradeIn { get; init; }
    public string? TradeInVehicleInfo { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO de lead
/// </summary>
public record ChatLeadDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? InterestedVehicleId { get; init; }
    public string? InterestedVehicleName { get; init; }
    public decimal? BudgetMin { get; init; }
    public decimal? BudgetMax { get; init; }
    public bool InterestedInFinancing { get; init; }
    public bool InterestedInTradeIn { get; init; }
    public LeadStatus Status { get; init; }
    public LeadTemperature Temperature { get; init; }
    public int QualificationScore { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

#endregion

#region Configuration DTOs

/// <summary>
/// DTO de configuración del chatbot
/// </summary>
public record ChatbotConfigurationDto
{
    public Guid Id { get; init; }
    public Guid? DealerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    // Plan y límites
    public ChatbotPlan Plan { get; init; }
    public int FreeInteractionsPerMonth { get; init; }
    public decimal CostPerInteraction { get; init; }
    public int MaxInteractionsPerSession { get; init; }
    public int MaxInteractionsPerUserPerDay { get; init; }
    public int MaxInteractionsPerUserPerMonth { get; init; }

    // Personalización
    public string BotName { get; init; } = string.Empty;
    public string? BotAvatarUrl { get; init; }
    public string WelcomeMessage { get; init; } = string.Empty;

    // Canales
    public bool EnableWebChat { get; init; }
    public bool EnableWhatsApp { get; init; }
    public bool EnableFacebook { get; init; }

    // Automatización
    public bool EnableAutoInventorySync { get; init; }
    public bool EnableAutoReports { get; init; }
    public bool EnableAutoLearning { get; init; }
    public bool EnableHealthMonitoring { get; init; }
}

/// <summary>
/// Request para crear/actualizar configuración
/// </summary>
public record CreateOrUpdateConfigurationRequest
{
    public Guid? DealerId { get; init; }
    public string Name { get; init; } = "Default Chatbot";

    // LLM Server
    public string LlmServerUrl { get; init; } = string.Empty;
    public string LlmModelId { get; init; } = string.Empty;
    public string LlmLanguageCode { get; init; } = "es";
    public string? LlmSystemPrompt { get; init; }

    // Límites
    public int MaxInteractionsPerSession { get; init; } = 10;
    public int MaxInteractionsPerUserPerDay { get; init; } = 50;
    public int MaxInteractionsPerUserPerMonth { get; init; } = 500;
    public int MaxGlobalInteractionsPerDay { get; init; } = 5000;
    public int MaxGlobalInteractionsPerMonth { get; init; } = 100000;

    // Personalización
    public string BotName { get; init; } = "Asistente OKLA";
    public string? BotAvatarUrl { get; init; }
    public string WelcomeMessage { get; init; } = "¡Hola! 👋 Soy tu asistente virtual. ¿En qué puedo ayudarte hoy?";
    public string? QuickRepliesJson { get; init; }

    // Automatización
    public bool EnableAutoInventorySync { get; init; } = true;
    public int InventorySyncIntervalMinutes { get; init; } = 60;
    public bool EnableAutoReports { get; init; } = true;
    public bool EnableAutoLearning { get; init; } = true;
    public bool EnableHealthMonitoring { get; init; } = true;
}

#endregion

#region Usage & Analytics DTOs

/// <summary>
/// DTO de uso de interacciones
/// </summary>
public record InteractionUsageDto
{
    public int Today { get; init; }
    public int Month { get; init; }
    public int FreeRemaining { get; init; }
    public int PaidToDate { get; init; }
    public decimal TotalCostToDate { get; init; }
    /// <summary>Overage cost at $0.08 per conversation beyond free tier.</summary>
    public decimal OverageCost { get; init; }
    public decimal UsagePercentage { get; init; }
    public UsageStatus Status { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}

/// <summary>
/// DTO de resumen mensual
/// </summary>
public record MonthlySummaryDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int TotalInteractions { get; init; }
    public int FreeInteractionsUsed { get; init; }
    public int PaidInteractions { get; init; }
    public decimal TotalCost { get; init; }
    public int TotalSessions { get; init; }
    public int UniqueUsers { get; init; }
    public decimal AvgInteractionsPerSession { get; init; }
    public int LeadsGenerated { get; init; }
    public int LeadsConverted { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal AvgConfidenceScore { get; init; }
    public int FallbackCount { get; init; }
    public Dictionary<string, int>? TopIntents { get; init; }
}

/// <summary>
/// Estadísticas del chatbot
/// </summary>
public record ChatbotStatsDto
{
    public int ActiveSessions { get; init; }
    public int TodaySessions { get; init; }
    public int TodayMessages { get; init; }
    public int TodayLeads { get; init; }
    public int TodayInteractions { get; init; }
    public decimal TodayCost { get; init; }
    public decimal AverageSessionDuration { get; init; }
    public decimal AverageMessagesPerSession { get; init; }
    public decimal AvgResponseTimeMs { get; init; }
    public decimal AvgConfidence { get; init; }
    public decimal FallbackRate { get; init; }
    public int VehiclesInCache { get; init; }
    public DateTime LastInventorySync { get; init; }
    public bool IsHealthy { get; init; }
}

#endregion

#region Maintenance DTOs

/// <summary>
/// DTO de tarea de mantenimiento
/// </summary>
public record MaintenanceTaskDto
{
    public Guid Id { get; init; }
    public MaintenanceTaskType TaskType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CronExpression { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public MaintenanceTaskStatus Status { get; init; }
    public DateTime? LastRunAt { get; init; }
    public DateTime? NextRunAt { get; init; }
    public bool LastRunSuccess { get; init; }
    public string? LastRunResult { get; init; }
    public int TotalExecutions { get; init; }
    public int SuccessfulExecutions { get; init; }
    public decimal AvgExecutionTimeMs { get; init; }
}

/// <summary>
/// Request para ejecutar tarea de mantenimiento manualmente
/// </summary>
public record RunMaintenanceTaskRequest
{
    public Guid TaskId { get; init; }
    public bool Force { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Resultado de ejecución de tarea
/// </summary>
public record MaintenanceTaskResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public int ExecutionTimeMs { get; init; }
}

#endregion

#region Quick Response DTOs

/// <summary>
/// DTO de respuesta rápida
/// </summary>
public record QuickResponseDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string> Triggers { get; init; } = new();
    public string Response { get; init; } = string.Empty;
    public List<QuickReplyDto>? QuickReplies { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public bool BypassLlm { get; init; }
    public int UsageCount { get; init; }
    public DateTime? LastUsedAt { get; init; }
}

/// <summary>
/// Request para crear respuesta rápida
/// </summary>
public record CreateQuickResponseRequest
{
    public Guid ConfigurationId { get; init; }
    public string Category { get; init; } = "general";
    public string Name { get; init; } = string.Empty;
    public List<string> Triggers { get; init; } = new();
    public string Response { get; init; } = string.Empty;
    public List<QuickReplyDto>? QuickReplies { get; init; }
    public int Priority { get; init; } = 100;
    public bool BypassLlm { get; init; } = true;
}

#endregion

#region Session & Message Response DTOs

/// <summary>
/// DTO simplificado de sesión para queries
/// </summary>
public record SessionDto
{
    public Guid SessionId { get; init; }
    public string SessionToken { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MessageCount { get; init; }
    public int InteractionCount { get; init; }
    public int MaxInteractionsPerSession { get; init; }
    public int RemainingInteractions { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
}

/// <summary>
/// DTO simplificado de mensaje para queries
/// </summary>
public record MessageDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? Response { get; init; }
    public bool IsFromBot { get; init; }
    public string? IntentName { get; init; }
    public bool IsFallback { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO de salud del chatbot
/// </summary>
public record ChatbotHealthDto
{
    public Guid ConfigurationId { get; init; }
    public string OverallStatus { get; init; } = "Unknown";
    public bool LlmConnected { get; init; }
    public bool DatabaseConnected { get; init; }
    public int ActiveSessions { get; init; }
    public int TotalSessions { get; init; }
    public List<HealthAlertDto> Alerts { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// DTO de alerta de salud
/// </summary>
public record HealthAlertDto
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = "Info";
    public string Message { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
}

#endregion

#region Auto-Learning DTOs

/// <summary>
/// Sugerencia de intención
/// </summary>
public record IntentSuggestionDto
{
    public Guid Id { get; init; }
    public string SuggestedName { get; init; } = string.Empty;
    public IntentCategory Category { get; init; }
    public List<string> TrainingPhrases { get; init; } = new();
    public string SuggestedResponse { get; init; } = string.Empty;
    public int Confidence { get; init; }
    public int BasedOnQuestions { get; init; }
    public bool IsApproved { get; init; }
    public bool IsRejected { get; init; }
}

/// <summary>
/// Pregunta sin respuesta
/// </summary>
public record UnansweredQuestionDto
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public int OccurrenceCount { get; init; }
    public DateTime FirstAskedAt { get; init; }
    public DateTime LastAskedAt { get; init; }
    public string? AttemptedIntentName { get; init; }
    public decimal? AttemptedConfidence { get; init; }
    public IntentCategory SuggestedCategory { get; init; }
    public string? SuggestedIntentName { get; init; }
    public string? SuggestedResponse { get; init; }
    public bool IsProcessed { get; init; }
}

#endregion
#region Session Control DTOs

/// <summary>
/// Request para terminar una sesión
/// </summary>
public record EndSessionRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string? EndReason { get; init; }
    public int? SatisfactionScore { get; init; }
    public string? Feedback { get; init; }
}

/// <summary>
/// Request para transferir a agente humano
/// </summary>
public record TransferToAgentRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string? TransferReason { get; init; }
    public string? PreferredAgent { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Request para que un dealer/agente tome control de la sesión (handoff bot→humano)
/// </summary>
public record TakeOverRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string AgentId { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

/// <summary>
/// Request para devolver control al bot (handoff humano→bot)
/// </summary>
public record ReturnToBotRequest
{
    public string SessionToken { get; init; } = string.Empty;
}

/// <summary>
/// Request para aceptar el disclosure de bot y política de privacidad.
/// El comprador DEBE aceptar antes de poder enviar mensajes.
/// </summary>
public record AcceptDisclosureRequest
{
    public string SessionToken { get; init; } = string.Empty;
}

/// <summary>
/// Respuesta al aceptar el disclosure
/// </summary>
public record AcceptDisclosureResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ConsentAcceptedAt { get; init; }
}

#endregion

#region Report DTOs

/// <summary>
/// Reporte diario del chatbot
/// </summary>
public record DailyReport
{
    public Guid ChatbotConfigurationId { get; init; }
    public DateTime ReportDate { get; init; }
    public DateTime GeneratedAt { get; init; }
    public int TotalSessions { get; init; }
    public int TotalMessages { get; init; }
    public int TotalInteractions { get; init; }
    public int UniqueUsers { get; init; }
    public int LeadsGenerated { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AverageSessionDuration { get; init; }
    public decimal AverageMessagesPerSession { get; init; }
    public decimal FallbackRate { get; init; }
    public decimal AverageConfidence { get; init; }
    public Dictionary<string, int> TopIntents { get; init; } = new();
    public Dictionary<int, int> SessionsByHour { get; init; } = new();
}

/// <summary>
/// Reporte semanal del chatbot
/// </summary>
public record WeeklyReport
{
    public Guid ChatbotConfigurationId { get; init; }
    public DateTime WeekStart { get; init; }
    public DateTime WeekEnd { get; init; }
    public DateTime GeneratedAt { get; init; }
    public int TotalSessions { get; init; }
    public int TotalMessages { get; init; }
    public int TotalInteractions { get; init; }
    public int UniqueUsers { get; init; }
    public int LeadsGenerated { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AverageSessionDuration { get; init; }
    public decimal FallbackRate { get; init; }
    public List<DailyReport> DailyBreakdown { get; init; } = new();
    public Dictionary<string, int> TopIntents { get; init; } = new();
    public Dictionary<DayOfWeek, int> SessionsByDayOfWeek { get; init; } = new();
}

/// <summary>
/// Reporte mensual del chatbot
/// </summary>
public record MonthlyReport
{
    public Guid ChatbotConfigurationId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public DateTime GeneratedAt { get; init; }
    public int TotalSessions { get; init; }
    public int TotalMessages { get; init; }
    public int TotalInteractions { get; init; }
    public int FreeInteractionsUsed { get; init; }
    public int PaidInteractions { get; init; }
    public int UniqueUsers { get; init; }
    public int LeadsGenerated { get; init; }
    public int LeadsConverted { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal TotalCost { get; init; }
    public decimal ProjectedAnnualCost { get; init; }
    public decimal AverageSessionDuration { get; init; }
    public decimal FallbackRate { get; init; }
    public List<WeeklyReport> WeeklyBreakdown { get; init; } = new();
    public Dictionary<string, int> TopIntents { get; init; } = new();
}

#endregion

#region Auto-Learning Results DTOs

/// <summary>
/// Resultado del análisis de auto-aprendizaje
/// </summary>
public record AutoLearningResult
{
    public Guid ChatbotConfigurationId { get; init; }
    public DateTime AnalyzedAt { get; init; }
    public int ConversationsAnalyzed { get; init; }
    public int FallbacksAnalyzed { get; init; }
    public int SuggestedIntentsCount { get; init; }
    public int AutoAppliedCount { get; init; }
    public int PendingReviewCount { get; init; }
    public decimal EstimatedCostSavings { get; init; }
    public List<IntentSuggestionDto> Suggestions { get; init; } = new();
}

/// <summary>
/// Resultado de sugerencias de intent
/// </summary>
public record IntentSuggestionResult
{
    public Guid ChatbotConfigurationId { get; init; }
    public DateTime GeneratedAt { get; init; }
    public int TotalSuggestions { get; init; }
    public int HighConfidenceCount { get; init; }
    public int RequiresReviewCount { get; init; }
    public List<IntentSuggestionDto> Suggestions { get; init; } = new();
}

#endregion