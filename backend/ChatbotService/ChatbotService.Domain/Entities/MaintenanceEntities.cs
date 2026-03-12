using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Entities;

/// <summary>
/// Tarea de mantenimiento automatizado del chatbot
/// </summary>
public class MaintenanceTask
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }

    // Tipo y nombre
    public MaintenanceTaskType TaskType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Programación (CRON)
    public string CronExpression { get; set; } = "0 * * * *"; // Cada hora por defecto
    public bool IsEnabled { get; set; } = true;

    // Ejecución
    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Pending;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public int ExecutionTimeMs { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int MaxRetries { get; set; } = 3;

    // Resultado de la última ejecución
    public bool LastRunSuccess { get; set; }
    public string? LastRunResult { get; set; }
    public string? LastRunError { get; set; }

    // Métricas acumuladas
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public decimal AvgExecutionTimeMs { get; set; }

    // Configuración específica de la tarea (JSON)
    public string? TaskConfigJson { get; set; }

    // Notificaciones
    public bool NotifyOnFailure { get; set; } = true;
    public bool NotifyOnSuccess { get; set; }
    public string? NotificationEmails { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
    public virtual ICollection<MaintenanceTaskLog> Logs { get; set; } = new List<MaintenanceTaskLog>();
}

/// <summary>
/// Log de ejecución de tarea de mantenimiento
/// </summary>
public class MaintenanceTaskLog
{
    public Guid Id { get; set; }
    public Guid MaintenanceTaskId { get; set; }

    // Ejecución
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ExecutionTimeMs { get; set; }

    // Resultado
    public MaintenanceTaskStatus Status { get; set; }
    public bool Success { get; set; }
    public string? ResultSummary { get; set; }
    public string? ResultDetails { get; set; } // JSON con detalles
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }

    // Métricas específicas de la tarea
    public int? ItemsProcessed { get; set; }
    public int? ItemsSucceeded { get; set; }
    public int? ItemsFailed { get; set; }

    // Relaciones
    public virtual MaintenanceTask Task { get; set; } = null!;
}

/// <summary>
/// Intent registrado para el chatbot
/// </summary>
public class ChatbotIntent
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }

    // Información del intent
    public string IntentId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IntentCategory Category { get; set; }
    public int Priority { get; set; }

    // Training phrases (JSON array)
    public string? TrainingPhrasesJson { get; set; }
    public int TrainingPhraseCount { get; set; }

    // Respuestas (JSON array)
    public string? ResponsesJson { get; set; }

    // Parámetros/Entidades (JSON)
    public string? ParametersJson { get; set; }

    // Contextos
    public string? InputContextsJson { get; set; }
    public string? OutputContextsJson { get; set; }

    // Webhook
    public bool UseWebhook { get; set; }

    // Métricas de uso
    public int TotalMatches { get; set; }
    public decimal AvgConfidenceScore { get; set; }
    public int FallbackToThis { get; set; } // Veces que fue fallback

    // Estado
    public bool IsActive { get; set; } = true;
    public bool IsFallback { get; set; }
    public bool IsWelcome { get; set; }

    // Sugerencias de mejora (auto-learning)
    public string? SuggestedPhrasesJson { get; set; }
    public int SuggestedPhrasesCount { get; set; }
    public bool HasPendingSuggestions { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMatchedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
}

/// <summary>
/// Pregunta sin respuesta detectada para auto-learning
/// </summary>
public class UnansweredQuestion
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }
    public Guid? SessionId { get; set; }

    // La pregunta
    public string Question { get; set; } = string.Empty;
    public string NormalizedQuestion { get; set; } = string.Empty; // Limpio y lowercase

    // Alias para compatibilidad
    public string OriginalQuestion { get => Question; set => Question = value; }

    // Cuántas veces se ha preguntado
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstAskedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAskedAt { get; set; } = DateTime.UtcNow;

    // Intentos de respuesta
    public string? AttemptedIntentName { get; set; }
    public decimal? AttemptedConfidence { get; set; }

    // Alias para compatibilidad
    public decimal AttemptedConfidenceScore { get => AttemptedConfidence ?? 0; set => AttemptedConfidence = value; }

    // Categorización sugerida por IA
    public IntentCategory SuggestedCategory { get; set; }
    public string? SuggestedIntentName { get; set; }
    public string? SuggestedResponse { get; set; }

    // Estado de procesamiento
    public bool IsProcessed { get; set; }
    public bool IsAddedToTraining { get; set; }
    public Guid? AddedToIntentId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
}

/// <summary>
/// Vehículo sincronizado con el chatbot para consultas
/// </summary>
public class ChatbotVehicle
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }
    public Guid VehicleId { get; set; } // ID del vehículo en VehiclesSaleService

    // Información básica para el chatbot
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Trim { get; set; }
    public string? Color { get; set; }

    // Colores detallados
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }

    // Descripción
    public string? Description { get; set; }

    // Precios
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool IsOnSale { get; set; }

    // Características principales
    public string? BodyType { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public int? Mileage { get; set; }
    public string? EngineSize { get; set; }

    // Para búsquedas
    public string SearchableText { get; set; } = string.Empty; // Concatenación de todo
    public string? TagsJson { get; set; } // Tags para matching

    // Imágenes y URLs
    public string? MainImageUrl { get; set; }
    public string? ImagesJson { get; set; }
    public string? ImageUrl { get => MainImageUrl; set => MainImageUrl = value; }
    public string? VehicleUrl { get; set; }

    // Estado
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }

    // Métricas
    public int ViewsFromChatbot { get; set; }
    public int InquiriesFromChatbot { get; set; }

    // Aliases para compatibilidad
    public int ViewCount { get => ViewsFromChatbot; set => ViewsFromChatbot = value; }
    public int InquiryCount { get => InquiriesFromChatbot; set => InquiriesFromChatbot = value; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
}

/// <summary>
/// Respuesta rápida/FAQ precargada
/// </summary>
public class QuickResponse
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }

    // Categoría y nombre
    public string Category { get; set; } = "general";
    public string Name { get; set; } = string.Empty;

    // Triggers (palabras clave que activan esta respuesta)
    public string TriggersJson { get; set; } = "[]"; // JSON array

    // Aliases para compatibilidad
    public string TriggerKeywords { get => TriggersJson; set => TriggersJson = value; }

    // Respuesta
    public string Response { get; set; } = string.Empty;

    // Alias para compatibilidad
    public string ResponseText { get => Response; set => Response = value; }

    public string? QuickRepliesJson { get; set; } // Botones de seguimiento

    // Prioridad (para cuando varias coinciden)
    public int Priority { get; set; } = 100;

    // Estado
    public bool IsActive { get; set; } = true;

    // Métricas
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Si esta respuesta evita llamar al LLM (ahorra interacción)
    public bool BypassLlm { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;

    /// <summary>
    /// Obtiene los triggers como lista de strings
    /// </summary>
    public List<string> GetTriggers()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(TriggersJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
