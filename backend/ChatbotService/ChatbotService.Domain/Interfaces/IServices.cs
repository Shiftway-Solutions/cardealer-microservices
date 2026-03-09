using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Models;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Servicio de inferencia LLM.
/// Se comunica con el servidor llama.cpp que sirve el modelo GGUF fine-tuned.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Genera una respuesta del chatbot usando el modelo LLM fine-tuned.
    /// </summary>
    /// <param name="systemPrompt">Optional per-dealer system prompt. If null, uses the default from settings.</param>
    Task<LlmDetectionResult> GenerateResponseAsync(string sessionId, string text, string? languageCode = null, string? systemPrompt = null, CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene información del modelo LLM cargado
    /// </summary>
    Task<LlmModelInfo> GetModelInfoAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Verifica conectividad con el servidor LLM
    /// </summary>
    Task<bool> TestConnectivityAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene el estado de salud del servidor LLM
    /// </summary>
    Task<LlmHealthStatus> GetHealthStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene el historial de mensajes recientes de una sesión para contexto
    /// </summary>
    Task<IEnumerable<LlmChatMessage>> GetSessionContextAsync(string sessionId, int maxMessages = 10, CancellationToken ct = default);
}

/// <summary>
/// Servicio de sincronización de inventario de vehículos
/// </summary>
public interface IInventorySyncService
{
    /// <summary>
    /// Sincroniza vehículos desde VehiclesSaleService
    /// </summary>
    Task<InventorySyncResult> SyncVehiclesAsync(Guid configurationId, CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene la cantidad de vehículos activos
    /// </summary>
    Task<int> GetActiveVehicleCountAsync(Guid configurationId, CancellationToken ct = default);
    
    /// <summary>
    /// Busca vehículos por texto
    /// </summary>
    Task<IEnumerable<ChatbotVehicle>> SearchVehiclesAsync(Guid configurationId, string query, int limit = 5, CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene vehículos destacados
    /// </summary>
    Task<IEnumerable<ChatbotVehicle>> GetFeaturedVehiclesAsync(Guid configurationId, int limit = 5, CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene vehículos por rango de precio
    /// </summary>
    Task<IEnumerable<ChatbotVehicle>> GetVehiclesByPriceRangeAsync(Guid configurationId, decimal minPrice, decimal maxPrice, int limit = 10, CancellationToken ct = default);
}

/// <summary>
/// Servicio de auto-aprendizaje del chatbot
/// </summary>
public interface IAutoLearningService
{
    /// <summary>
    /// Analiza conversaciones y sugiere mejoras
    /// </summary>
    Task<AutoLearningAnalysisResult> AnalyzeAndSuggestAsync(Guid configurationId, CancellationToken ct = default);
    
    /// <summary>
    /// Aplica una sugerencia de auto-aprendizaje
    /// </summary>
    Task<bool> ApplySuggestionAsync(AutoLearningSuggestion suggestion, CancellationToken ct = default);
    
    /// <summary>
    /// Aplica automáticamente sugerencias con alta confianza
    /// </summary>
    Task<int> AutoApplyHighConfidenceSuggestionsAsync(Guid configurationId, float minConfidence = 0.85f, CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene intents sugeridos
    /// </summary>
    Task<IEnumerable<SuggestedIntent>> GetSuggestedIntentsAsync(Guid configurationId, int limit = 10, CancellationToken ct = default);
    
    /// <summary>
    /// Registra un fallback para análisis
    /// </summary>
    Task RecordFallbackAsync(Guid configurationId, string userMessage, string? attemptedIntent, float? confidence, CancellationToken ct = default);
}

/// <summary>
/// Servicio de monitoreo de salud del chatbot
/// </summary>
public interface IHealthMonitoringService
{
    /// <summary>
    /// Genera un reporte de salud completo
    /// </summary>
    Task<ChatbotHealthReport> GenerateHealthReportAsync(Guid configurationId, CancellationToken ct = default);
    
    /// <summary>
    /// Verifica la salud del servidor LLM
    /// </summary>
    Task<bool> CheckLlmHealthAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Verifica la salud de la base de datos
    /// </summary>
    Task<bool> CheckDatabaseHealthAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene métricas del sistema
    /// </summary>
    Task<SystemMetrics> GetSystemMetricsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene alertas activas
    /// </summary>
    Task<IEnumerable<HealthAlert>> GetActiveAlertsAsync(Guid configurationId, CancellationToken ct = default);
}

/// <summary>
/// Servicio de reportes y análisis de costos
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Genera reporte de análisis de costos
    /// </summary>
    Task<CostAnalysisReport> GenerateCostReportAsync(Guid configurationId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    
    /// <summary>
    /// Envía reporte por email
    /// </summary>
    Task<bool> SendReportByEmailAsync(CostAnalysisReport report, string recipientEmail, CancellationToken ct = default);
    
    /// <summary>
    /// Genera datos para dashboard
    /// </summary>
    Task<object> GenerateDashboardDataAsync(Guid configurationId, CancellationToken ct = default);
    
    /// <summary>
    /// Obtiene tendencias de uso
    /// </summary>
    Task<object> GetUsageTrendsAsync(Guid configurationId, int days = 30, CancellationToken ct = default);
}

/// <summary>
/// Servicio de cache para respuestas LLM.
/// Evita llamadas redundantes al modelo para preguntas FAQ idénticas.
/// </summary>
public interface ILlmResponseCacheService
{
    /// <summary>
    /// Intenta obtener una respuesta cacheada para la consulta dada.
    /// Retorna null si no hay cache hit.
    /// </summary>
    Task<CachedLlmResponseDto?> GetAsync(string query, string? systemPrompt = null, CancellationToken ct = default);

    /// <summary>
    /// Cachea una respuesta del LLM. Solo cachea respuestas no-fallback, alta confianza,
    /// para intents cacheables.
    /// </summary>
    Task SetAsync(string query, string response, string? intent, float confidence, bool isFallback,
        string? systemPrompt = null, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all cached LLM responses (e.g., after inventory sync or model update).
    /// Stale cache entries can serve outdated prices and sold vehicle data.
    /// </summary>
    Task InvalidateAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidate a specific cached LLM response by query and system prompt.
    /// Used to remove hallucinated or moderated cached responses.
    /// </summary>
    Task InvalidateAsync(string query, string? systemPrompt = null, CancellationToken ct = default);
}

/// <summary>
/// DTO para respuestas LLM cacheadas.
/// </summary>
public class CachedLlmResponseDto
{
    public string Response { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public float Confidence { get; set; }
    public DateTime CachedAt { get; set; }
    public bool FromCache { get; set; }
}

/// <summary>
/// R17-PC: Tracks Anthropic server-side prompt cache token usage for cost-savings measurement.
/// This is an in-memory accumulator used to feed the /metrics/prompt-cache REST endpoint.
/// The actual OpenTelemetry metrics are handled separately by ChatbotMetrics.
/// </summary>
public interface IPromptCacheStats
{
    /// <summary>
    /// Record token usage from a single Anthropic API call.
    /// </summary>
    void RecordCall(long cacheReadTokens, long cacheWriteTokens, long totalInputTokens);

    /// <summary>
    /// Get current accumulated stats.
    /// </summary>
    PromptCacheReport GetReport();
}

/// <summary>
/// Snapshot of accumulated Anthropic Prompt Cache token usage and estimated cost savings.
/// </summary>
public sealed record PromptCacheReport
{
    public long TotalLlmCalls { get; init; }
    public long TotalInputTokens { get; init; }
    public long CacheReadTokens { get; init; }
    public long CacheWriteTokens { get; init; }

    /// <summary>
    /// Fraction of input tokens served from cache (0–100%).
    /// Higher = better cache hit rate.
    /// </summary>
    public double CacheHitRatePercent => TotalInputTokens > 0
        ? Math.Round((double)CacheReadTokens / TotalInputTokens * 100, 1)
        : 0;

    /// <summary>
    /// Estimated input-token cost savings vs. no caching.
    /// Formula: savings = cacheReadTokens × (1 - 0.1) - cacheWriteTokens × (1.25 - 1.0)
    ///          as a fraction of totalInputTokens × basePrice.
    /// Simplifies to: (cacheReadTokens × 0.9 - cacheWriteTokens × 0.25) / totalInputTokens × 100
    /// </summary>
    public double EstimatedSavingsPercent => TotalInputTokens > 0
        ? Math.Round(
            (CacheReadTokens * 0.9 - CacheWriteTokens * 0.25) / TotalInputTokens * 100, 1)
        : 0;

    /// <summary>
    /// True when estimated savings exceed the 60% target from the audit.
    /// </summary>
    public bool TargetMet => EstimatedSavingsPercent >= 60.0;

    public double TargetPercent => 60.0;
    public DateTimeOffset? LastCallAt { get; init; }
    public DateTimeOffset MeasuredAt { get; init; }
}
