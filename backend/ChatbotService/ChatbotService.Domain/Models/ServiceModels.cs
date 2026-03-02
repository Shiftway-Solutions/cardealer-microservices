namespace ChatbotService.Domain.Models;

/// <summary>
/// Resultado de la inferencia LLM.
/// El modelo Llama 3 retorna JSON con estos campos.
/// </summary>
public class LlmDetectionResult
{
    public string? DetectedIntent { get; set; }
    public float ConfidenceScore { get; set; }
    public string? FulfillmentText { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool IsFallback { get; set; }
    public string? ResponseId { get; set; }
    public string? LanguageCode { get; set; }
    public string? QueryText { get; set; }
    public List<string>? OutputContexts { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    // Campos adicionales del LLM
    public int TokensUsed { get; set; }
    public double ResponseTimeMs { get; set; }
    public LlmLeadSignals? LeadSignals { get; set; }

    /// <summary>Suggested next action from the model (e.g., "show_financing", "transfer_agent").</summary>
    public string? SuggestedAction { get; set; }

    /// <summary>Quick reply options suggested by the model for the UI.</summary>
    public List<string>? QuickReplies { get; set; }
    // ═══════════════════════════════════════════════════════════════
    // DealerChatAgent — Intent Scoring & Classification
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Intent Score 1-10 calculated by the DealerChatAgent per turn.</summary>
    public int IntentScore { get; set; } = 1;

    /// <summary>Classification: curioso, prospecto_frio, prospecto_tibio, comprador_inminente</summary>
    public string Clasificacion { get; set; } = "curioso";

    /// <summary>Active module: qa, cierre, handoff</summary>
    public string ModuloActivo { get; set; } = "qa";

    /// <summary>Vehicle ID the user is interested in</summary>
    public string? VehiculoInteresId { get; set; }

    /// <summary>Whether handoff to human was triggered</summary>
    public bool HandoffActivado { get; set; }

    /// <summary>Reason for handoff</summary>
    public string? RazonHandoff { get; set; }

    /// <summary>Topics discussed: precio, financiamiento, disponibilidad, etc.</summary>
    public List<string> TemasConsulta { get; set; } = new();

    /// <summary>Appointment proposal from the agent</summary>
    public object? CitaPropuesta { get; set; }}

/// <summary>
/// Señales de lead detectadas por el modelo LLM.
/// MUST match the training data JSON schema exactly:
///   "leadSignals": { "mentionedBudget": bool, "requestedTestDrive": bool, 
///                     "askedFinancing": bool, "providedContactInfo": bool }
/// </summary>
public class LlmLeadSignals
{
    /// <summary>Whether the user mentioned a budget or price range.</summary>
    public bool MentionedBudget { get; set; }

    /// <summary>Whether the user requested a test drive.</summary>
    public bool RequestedTestDrive { get; set; }

    /// <summary>Whether the user asked about financing options.</summary>
    public bool AskedFinancing { get; set; }

    /// <summary>Whether the user provided contact information.</summary>
    public bool ProvidedContactInfo { get; set; }
}

/// <summary>
/// Mensaje de chat para contexto del LLM
/// </summary>
public class LlmChatMessage
{
    public string Role { get; set; } = string.Empty;   // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Información del modelo LLM
/// </summary>
public class LlmModelInfo
{
    public string? ModelId { get; set; }
    public string? ModelPath { get; set; }
    public string? ModelType { get; set; }
    public string? Quantization { get; set; }
    public int ContextLength { get; set; }
    public int GpuLayers { get; set; }
    public int Threads { get; set; }
    public string? BaseModel { get; set; }
    public string? FineTunedFor { get; set; }
    public bool IsHealthy { get; set; }
    public string? HealthMessage { get; set; }
}

/// <summary>
/// Reporte de salud del chatbot
/// </summary>
public class ChatbotHealthReport
{
    public Guid ChatbotConfigurationId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public ChatbotHealthStatus OverallStatus { get; set; }
    public LlmHealthStatus LlmStatus { get; set; } = new();
    public DatabaseHealthStatus DatabaseStatus { get; set; } = new();
    public SystemMetrics SystemMetrics { get; set; } = new();
    public List<HealthAlert> Alerts { get; set; } = new();
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

public enum ChatbotHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Estado de salud del servidor LLM
/// </summary>
public class LlmHealthStatus
{
    public bool IsConnected { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime? LastSuccessfulCall { get; set; }
    public int FailedCallsLast24h { get; set; }
    public string? LastError { get; set; }
    public LlmModelInfo? ModelInfo { get; set; }
    public bool ModelLoaded { get; set; }
    public double UptimeSeconds { get; set; }
    public int TotalRequests { get; set; }
    public double AvgResponseTimeMs { get; set; }
}

public class DatabaseHealthStatus
{
    public bool IsConnected { get; set; }
    public double ResponseTimeMs { get; set; }
    public long TotalSessions { get; set; }
    public long TotalMessages { get; set; }
    public long ActiveSessions { get; set; }
}

public class SystemMetrics
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsageMb { get; set; }
    public double AvailableMemoryMb { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ActiveConnections { get; set; }
    public double RequestsPerMinute { get; set; }
    public double AverageResponseTimeMs { get; set; }
}

public class HealthAlert
{
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
    public string Message { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Details { get; set; } = new();
}

/// <summary>
/// Sugerencia de intent para auto-aprendizaje
/// </summary>
public class SuggestedIntent
{
    public string IntentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> TrainingPhrases { get; set; } = new();
    public List<string> SuggestedResponses { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public float ConfidenceScore { get; set; }
    public bool RequiresReview { get; set; } = true;
    public string Source { get; set; } = "auto-learning"; // auto-learning, fallback-analysis, user-suggestion
}

/// <summary>
/// Sugerencia de auto-aprendizaje
/// </summary>
public class AutoLearningSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatbotConfigurationId { get; set; }
    public SuggestionType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? Data { get; set; }
    public int Priority { get; set; } // 1-10, higher is more important
    public float ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsApplied { get; set; }
    public bool RequiresReview { get; set; } = true;
    public DateTime? AppliedAt { get; set; }
    public string? AppliedBy { get; set; }
}

public enum SuggestionType
{
    NewIntent,
    TrainingPhraseAddition,
    ResponseImprovement,
    EntityAddition,
    QuickResponseCreation,
    FallbackImprovement
}

/// <summary>
/// Reporte de análisis de costos
/// </summary>
public class CostAnalysisReport
{
    public Guid ChatbotConfigurationId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // Métricas de uso
    public int TotalInteractions { get; set; }
    public int FreeInteractionsUsed { get; set; }
    public int PaidInteractions { get; set; }
    public int QuickResponseInteractions { get; set; }
    public int LlmInteractions { get; set; }
    
    // Costos
    public decimal TotalCost { get; set; }
    public decimal CostPerInteraction { get; set; }
    public decimal ProjectedMonthlyCost { get; set; }
    public decimal CostSavingsFromQuickResponses { get; set; }
    public decimal CostSavingsFromAutoLearning { get; set; }
    
    // Comparaciones
    public decimal PreviousPeriodCost { get; set; }
    public decimal CostChangePercent { get; set; }
    
    // Desglose por categoría
    public Dictionary<string, int> InteractionsByCategory { get; set; } = new();
    public Dictionary<string, decimal> CostsByCategory { get; set; } = new();
    
    // Recomendaciones
    public List<CostRecommendation> Recommendations { get; set; } = new();
}

public class CostRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedSavings { get; set; }
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string ActionRequired { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de sincronización de inventario
/// </summary>
public class InventorySyncResult
{
    public Guid ChatbotConfigurationId { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public int TotalVehiclesProcessed { get; set; }
    public int NewVehiclesAdded { get; set; }
    public int VehiclesUpdated { get; set; }
    public int VehiclesRemoved { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, int> VehiclesByMake { get; set; } = new();
    public Dictionary<string, int> VehiclesByBodyType { get; set; } = new();
}

/// <summary>
/// Resultado de análisis de auto-aprendizaje
/// </summary>
public class AutoLearningAnalysisResult
{
    public Guid ChatbotConfigurationId { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public int ConversationsAnalyzed { get; set; }
    public int FallbacksAnalyzed { get; set; }
    public List<SuggestedIntent> SuggestedIntents { get; set; } = new();
    public List<AutoLearningSuggestion> Suggestions { get; set; } = new();
    public int AutoAppliedCount { get; set; }
    public int PendingReviewCount { get; set; }
    public decimal EstimatedCostSavings { get; set; }
}
