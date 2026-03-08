using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Entities;

/// <summary>
/// Configuración del chatbot para un dealer o tenant
/// </summary>
public class ChatbotConfiguration
{
    public Guid Id { get; set; }
    public Guid? DealerId { get; set; } // null = configuración global
    public string Name { get; set; } = "Default Chatbot";
    public bool IsActive { get; set; } = true;
    
    // Alias para compatibilidad con código que usa IsEnabled
    public bool IsEnabled { get => IsActive; set => IsActive = value; }
    
    // LLM Configuration
    public string LlmProjectId { get; set; } = string.Empty;
    public string LlmModelId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "es";
    public string? SystemPromptText { get; set; } // System prompt personalizado por dealer
    
    // Plan y costos
    public ChatbotPlan Plan { get; set; } = ChatbotPlan.Standard;
    public int FreeInteractionsPerMonth { get; set; } = 180;
    public decimal CostPerInteraction { get; set; } = 0.002m;
    
    /// <summary>
    /// Cost per overage conversation (beyond free tier).
    /// Standard plan: $0.08 per conversation that exceeds FreeInteractionsPerMonth.
    /// This is the rate billed to the dealer for overage usage.
    /// </summary>
    public decimal OverageCostPerConversation { get; set; } = 0.08m;
    
    // Límites de interacciones
    public int MaxInteractionsPerSession { get; set; } = 10;
    public int MaxInteractionsPerUserPerDay { get; set; } = 50;
    public int MaxInteractionsPerUserPerMonth { get; set; } = 500;
    public int MaxGlobalInteractionsPerDay { get; set; } = 5000;
    public int MaxGlobalInteractionsPerMonth { get; set; } = 100000;
    
    // Comportamiento cuando se alcanza el límite
    public string LimitReachedMessage { get; set; } = 
        "Has alcanzado el límite de consultas. Por favor contacta a nuestro equipo de ventas directamente al {phone}.";
    public bool TransferToAgentOnLimit { get; set; } = true;
    
    // Configuración de sesión
    public int SessionTimeoutMinutes { get; set; } = 30;
    public int MaxMessagesPerSession { get; set; } = 100;
    
    // Personalización del chatbot
    public string BotName { get; set; } = "Asistente OKLA";
    public string BotAvatarUrl { get; set; } = string.Empty;
    public string WelcomeMessage { get; set; } = 
        "¡Hola! 👋 Soy tu asistente virtual. ¿En qué puedo ayudarte hoy?";
    public string OfflineMessage { get; set; } = 
        "En este momento no estamos disponibles. Deja tu mensaje y te contactaremos pronto.";
    public string? QuickRepliesJson { get; set; } // JSON de quick replies iniciales
    
    // Horarios de atención del bot
    public bool RestrictToBusinessHours { get; set; }
    public string? BusinessHoursJson { get; set; } // JSON con horarios
    public string TimeZone { get; set; } = "America/Santo_Domingo";
    
    // Canales habilitados
    public bool EnableWebChat { get; set; } = true;
    public bool EnableWhatsApp { get; set; }
    public bool EnableFacebook { get; set; }
    public bool EnableInstagram { get; set; }
    
    // Configuración de WhatsApp Business
    public string? WhatsAppBusinessPhoneId { get; set; }
    public string? WhatsAppBusinessAccountId { get; set; }
    public string? WhatsAppAccessToken { get; set; } // Encriptado
    
    // Configuración de Facebook
    public string? FacebookPageId { get; set; }
    public string? FacebookAccessToken { get; set; } // Encriptado
    
    // Webhooks para integraciones
    public string? WebhookUrl { get; set; }
    public string? WebhookSecret { get; set; }
    public bool SendLeadsToWebhook { get; set; }
    public bool SendConversationsToWebhook { get; set; }
    
    // Integración con CRM
    public string? CrmIntegrationType { get; set; } // salesforce, hubspot, etc.
    public string? CrmApiKey { get; set; } // Encriptado
    public bool AutoCreateLeadsInCrm { get; set; }
    
    // Notificaciones de citas
    /// <summary>
    /// Email del dealer para recibir notificaciones de citas agendadas vía chatbot.
    /// Si es null, no se envía correo al dealer.
    /// </summary>
    public string? ContactEmail { get; set; }
    
    // Automatización de mantenimiento
    public bool EnableAutoInventorySync { get; set; } = true;
    public int InventorySyncIntervalMinutes { get; set; } = 60;
    public bool EnableAutoReports { get; set; } = true;
    public bool EnableAutoLearning { get; set; } = true;
    public bool EnableHealthMonitoring { get; set; } = true;
    public int HealthCheckIntervalMinutes { get; set; } = 5;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Uso de interacciones del chatbot
/// </summary>
public class InteractionUsage
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }
    public Guid? UserId { get; set; }
    public string? SessionToken { get; set; }
    
    // Período
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    
    // Propiedad computada para fecha completa
    public DateTime UsageDate 
    { 
        get => new DateTime(Year, Month, Day);
        set { Year = value.Year; Month = value.Month; Day = value.Day; }
    }
    
    // Contadores
    public int InteractionCount { get; set; }
    public decimal TotalCost { get; set; }
    
    // Límites alcanzados
    public bool DailyLimitReached { get; set; }
    public bool MonthlyLimitReached { get; set; }
    public DateTime? LimitReachedAt { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
}

/// <summary>
/// Resumen mensual de uso del chatbot
/// </summary>
public class MonthlyUsageSummary
{
    public Guid Id { get; set; }
    public Guid ChatbotConfigurationId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    
    // Interacciones
    public int TotalInteractions { get; set; }
    public int FreeInteractionsUsed { get; set; }
    public int PaidInteractions { get; set; }
    public decimal TotalCost { get; set; }
    
    // Sesiones
    public int TotalSessions { get; set; }
    public int UniquUsers { get; set; }
    public decimal AvgInteractionsPerSession { get; set; }
    public decimal AvgSessionDurationMinutes { get; set; }
    
    // Leads
    public int LeadsGenerated { get; set; }
    public int LeadsConverted { get; set; }
    public decimal ConversionRate { get; set; }
    
    // Intents más usados (JSON)
    public string? TopIntentsJson { get; set; }
    
    // Satisfacción
    public decimal AvgConfidenceScore { get; set; }
    public int FallbackCount { get; set; } // Veces que no entendió
    
    // Estado
    public UsageStatus Status { get; set; }
    public decimal UsagePercentage { get; set; } // vs límite mensual
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
}
