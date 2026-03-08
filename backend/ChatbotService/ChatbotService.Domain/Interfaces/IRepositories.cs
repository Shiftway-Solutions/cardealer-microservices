using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Repositorio para sesiones de chat
/// </summary>
public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChatSession?> GetByTokenAsync(string sessionToken, CancellationToken ct = default);
    Task<ChatSession?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ChatSession?> GetByChannelUserIdAsync(string channel, string channelUserId, CancellationToken ct = default);
    Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(CancellationToken ct = default);
    Task<IEnumerable<ChatSession>> GetExpiredSessionsAsync(int timeoutMinutes, CancellationToken ct = default);
    Task<ChatSession> CreateAsync(ChatSession session, CancellationToken ct = default);
    Task UpdateAsync(ChatSession session, CancellationToken ct = default);
    Task<int> GetTodaySessionCountAsync(Guid configurationId, CancellationToken ct = default);
    Task<int> GetMonthSessionCountAsync(Guid configurationId, int year, int month, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para mensajes de chat
/// </summary>
public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<IEnumerable<ChatMessage>> GetBySessionTokenAsync(string sessionToken, CancellationToken ct = default);
    Task<ChatMessage> CreateAsync(ChatMessage message, CancellationToken ct = default);
    Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(Guid configurationId, int count, CancellationToken ct = default);
    Task<IEnumerable<ChatMessage>> GetFallbackMessagesAsync(Guid configurationId, DateTime since, CancellationToken ct = default);
    Task<IEnumerable<ChatMessage>> GetRecentBySessionTokenAsync(string sessionToken, int maxMessages, CancellationToken ct = default);
    Task<int> GetLlmCallsCountAsync(Guid configurationId, DateTime from, DateTime to, CancellationToken ct = default);
    
    /// <summary>
    /// Counts LLM interactions by a specific user within a time range (for daily/monthly limits).
    /// </summary>
    Task<int> GetLlmCallsCountByUserAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para leads del chatbot
/// </summary>
public interface IChatLeadRepository
{
    Task<ChatLead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChatLead?> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<IEnumerable<ChatLead>> GetByStatusAsync(LeadStatus status, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<ChatLead>> GetUnassignedLeadsAsync(CancellationToken ct = default);
    Task<ChatLead> CreateAsync(ChatLead lead, CancellationToken ct = default);
    Task UpdateAsync(ChatLead lead, CancellationToken ct = default);
    Task<int> GetTodayLeadCountAsync(Guid configurationId, CancellationToken ct = default);
    Task<int> GetMonthLeadCountAsync(Guid configurationId, int year, int month, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para configuración del chatbot
/// </summary>
public interface IChatbotConfigurationRepository
{
    Task<ChatbotConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChatbotConfiguration?> GetByDealerIdAsync(Guid dealerId, CancellationToken ct = default);
    Task<ChatbotConfiguration?> GetGlobalConfigurationAsync(CancellationToken ct = default);
    Task<ChatbotConfiguration?> GetDefaultAsync(CancellationToken ct = default);
    Task<IEnumerable<ChatbotConfiguration>> GetAllActiveAsync(CancellationToken ct = default);
    Task<ChatbotConfiguration> CreateAsync(ChatbotConfiguration config, CancellationToken ct = default);
    Task UpdateAsync(ChatbotConfiguration config, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para uso de interacciones
/// </summary>
public interface IInteractionUsageRepository
{
    Task<InteractionUsage?> GetTodayUsageAsync(Guid configurationId, Guid? userId, CancellationToken ct = default);
    Task<InteractionUsage?> GetMonthUsageAsync(Guid configurationId, Guid? userId, int year, int month, CancellationToken ct = default);
    Task<int> GetGlobalTodayInteractionsAsync(Guid configurationId, CancellationToken ct = default);
    Task<int> GetGlobalMonthInteractionsAsync(Guid configurationId, int year, int month, CancellationToken ct = default);
    Task<InteractionUsage> IncrementUsageAsync(Guid configurationId, Guid? userId, string? sessionToken, decimal cost, CancellationToken ct = default);
    Task<MonthlyUsageSummary?> GetMonthlySummaryAsync(Guid configurationId, int year, int month, CancellationToken ct = default);
    Task<MonthlyUsageSummary> CreateOrUpdateMonthlySummaryAsync(MonthlyUsageSummary summary, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para tareas de mantenimiento
/// </summary>
public interface IMaintenanceTaskRepository
{
    Task<MaintenanceTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<MaintenanceTask>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default);
    Task<IEnumerable<MaintenanceTask>> GetDueTasksAsync(CancellationToken ct = default);
    Task<IEnumerable<MaintenanceTask>> GetByTypeAsync(MaintenanceTaskType type, CancellationToken ct = default);
    Task<MaintenanceTask> CreateAsync(MaintenanceTask task, CancellationToken ct = default);
    Task UpdateAsync(MaintenanceTask task, CancellationToken ct = default);
    Task<MaintenanceTaskLog> LogExecutionAsync(MaintenanceTaskLog log, CancellationToken ct = default);
    Task<IEnumerable<MaintenanceTaskLog>> GetLogsAsync(Guid taskId, int limit, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para intents del chatbot
/// </summary>
public interface IChatbotIntentRepository
{
    Task<ChatbotIntent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChatbotIntent?> GetByIntentIdAsync(string intentId, CancellationToken ct = default);
    Task<IEnumerable<ChatbotIntent>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default);
    Task<IEnumerable<ChatbotIntent>> GetByCategoryAsync(Guid configurationId, IntentCategory category, CancellationToken ct = default);
    Task<IEnumerable<ChatbotIntent>> GetWithPendingSuggestionsAsync(Guid configurationId, CancellationToken ct = default);
    Task<ChatbotIntent> CreateAsync(ChatbotIntent intent, CancellationToken ct = default);
    Task UpdateAsync(ChatbotIntent intent, CancellationToken ct = default);
    Task IncrementMatchCountAsync(Guid id, decimal confidenceScore, CancellationToken ct = default);
    Task SyncIntentsAsync(Guid configurationId, IEnumerable<ChatbotIntent> intents, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para preguntas sin respuesta
/// </summary>
public interface IUnansweredQuestionRepository
{
    Task<UnansweredQuestion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UnansweredQuestion?> GetByQuestionAsync(Guid configurationId, string normalizedQuestion, CancellationToken ct = default);
    Task<IEnumerable<UnansweredQuestion>> GetUnprocessedAsync(Guid configurationId, int limit, CancellationToken ct = default);
    Task<IEnumerable<UnansweredQuestion>> GetMostFrequentAsync(Guid configurationId, int limit, CancellationToken ct = default);
    Task<UnansweredQuestion> CreateOrIncrementAsync(Guid configurationId, string question, string? attemptedIntent, decimal? confidence, CancellationToken ct = default);
    Task UpdateAsync(UnansweredQuestion question, CancellationToken ct = default);
    Task MarkAsProcessedAsync(Guid id, Guid? addedToIntentId, string processedBy, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para vehículos sincronizados con el chatbot
/// </summary>
public interface IChatbotVehicleRepository
{
    Task<ChatbotVehicle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChatbotVehicle?> GetByVehicleIdAsync(Guid configurationId, Guid vehicleId, CancellationToken ct = default);
    Task<IEnumerable<ChatbotVehicle>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default);
    Task<IEnumerable<ChatbotVehicle>> SearchAsync(Guid configurationId, string searchText, int limit = 5, CancellationToken ct = default);
    Task<IEnumerable<ChatbotVehicle>> GetFeaturedAsync(Guid configurationId, int limit = 5, CancellationToken ct = default);
    Task<IEnumerable<ChatbotVehicle>> GetByPriceRangeAsync(Guid configurationId, decimal minPrice, decimal maxPrice, int limit = 10, CancellationToken ct = default);
    Task<ChatbotVehicle> CreateAsync(ChatbotVehicle vehicle, CancellationToken ct = default);
    Task UpdateAsync(ChatbotVehicle vehicle, CancellationToken ct = default);
    Task<int> SyncFromInventoryAsync(Guid configurationId, IEnumerable<ChatbotVehicle> vehicles, CancellationToken ct = default);
    Task IncrementViewCountAsync(Guid id, CancellationToken ct = default);
    Task IncrementInquiryCountAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repositorio para respuestas rápidas
/// </summary>
public interface IQuickResponseRepository
{
    Task<QuickResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<QuickResponse>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default);
    Task<IEnumerable<QuickResponse>> GetByCategoryAsync(Guid configurationId, string category, CancellationToken ct = default);
    Task<QuickResponse?> FindMatchingAsync(Guid configurationId, string userMessage, CancellationToken ct = default);
    Task<QuickResponse> CreateAsync(QuickResponse response, CancellationToken ct = default);
    Task UpdateAsync(QuickResponse response, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task IncrementUsageCountAsync(Guid id, CancellationToken ct = default);
}
