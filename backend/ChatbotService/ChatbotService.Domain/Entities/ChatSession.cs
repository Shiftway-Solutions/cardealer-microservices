using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Entities;

/// <summary>
/// Representa una sesión de conversación con el chatbot.
/// Soporta dos modos: SingleVehicle (1 vehículo) y DealerInventory (inventario completo).
/// </summary>
public class ChatSession
{
    public Guid Id { get; set; }
    public string SessionToken { get; set; } = string.Empty;

    // Configuración del chatbot
    public Guid ChatbotConfigurationId { get; set; }

    // ══════════════════════════════════════════════════════════════
    // MODO DE CHAT — Determina la estrategia de contexto
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Modo de operación: SingleVehicle, DealerInventory, o General
    /// </summary>
    public ChatMode ChatMode { get; set; } = ChatMode.General;

    /// <summary>
    /// ID del vehículo específico (solo en modo SingleVehicle)
    /// </summary>
    public Guid? VehicleId { get; set; }

    /// <summary>
    /// ID del dealer (requerido en modo DealerInventory, opcional en SingleVehicle)
    /// </summary>
    public Guid? DealerId { get; set; }

    // Usuario (puede ser anónimo)
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }

    // Información del canal
    public SessionType SessionType { get; set; }
    public string Channel { get; set; } = "web"; // web, whatsapp, facebook, etc.
    public string? ChannelUserId { get; set; } // ID del usuario en el canal externo (ej: WhatsApp phone number)

    // Estado de la sesión
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public string? ContextData { get; set; } // JSON con contexto de la sesión

    // Métricas de la sesión
    public int MessageCount { get; set; }
    public int InteractionCount { get; set; } // Interacciones con el LLM
    public int MaxInteractionsPerSession { get; set; } = 10; // Límite configurable
    public bool InteractionLimitReached { get; set; }

    // ══════════════════════════════════════════════════════════════
    // WHATSAPP HANDOFF — Bot ↔ Humano
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Estado actual del handoff bot↔humano
    /// </summary>
    public HandoffStatus HandoffStatus { get; set; } = HandoffStatus.BotActive;

    /// <summary>
    /// ID del agente humano que tomó control (null = bot activo)
    /// </summary>
    public Guid? HandoffAgentId { get; set; }

    /// <summary>
    /// Nombre del agente humano que tomó control
    /// </summary>
    public string? HandoffAgentName { get; set; }

    /// <summary>
    /// Cuándo el agente tomó control
    /// </summary>
    public DateTime? HandoffAt { get; set; }

    /// <summary>
    /// Razón del handoff (automático por PII, solicitado por usuario, etc.)
    /// </summary>
    public string? HandoffReason { get; set; }

    // Contexto del vehículo (legacy — usar VehicleId para modo SingleVehicle)
    public Guid? CurrentVehicleId { get; set; }
    public string? CurrentVehicleName { get; set; }

    // Lead generado
    public Guid? LeadId { get; set; }

    // ══════════════════════════════════════════════════════════════
    // BOT DISCLOSURE CONSENT — Obligatorio antes de enviar mensajes
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// El comprador aceptó el disclosure de bot + política de privacidad.
    /// Debe ser true antes de que SendMessage procese mensajes.
    /// </summary>
    public bool ConsentAccepted { get; set; }

    /// <summary>
    /// Timestamp de aceptación del disclosure.
    /// </summary>
    public DateTime? ConsentAcceptedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int SessionDurationSeconds { get; set; }

    // Metadata
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceType { get; set; }
    public string? Language { get; set; } = "es";

    // Relaciones
    public virtual ChatbotConfiguration Configuration { get; set; } = null!;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public virtual ChatLead? Lead { get; set; }

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// ¿El bot está activo en esta sesión? (false = humano respondiendo)
    /// </summary>
    public bool IsBotActive => HandoffStatus == HandoffStatus.BotActive || HandoffStatus == HandoffStatus.ReturnedToBot;

    /// <summary>
    /// ¿Es una sesión de WhatsApp?
    /// </summary>
    public bool IsWhatsApp => SessionType == SessionType.WhatsApp || Channel == "whatsapp";
}

/// <summary>
/// Representa un mensaje individual en la conversación
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    // Contenido del mensaje
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }

    // Información del intent detectado por el LLM
    public string? IntentName { get; set; }
    public string? LlmIntentId { get; set; }
    public IntentCategory IntentCategory { get; set; }
    public decimal ConfidenceScore { get; set; }
    public ConfidenceLevel ConfidenceLevel { get; set; }
    public string? IntentParameters { get; set; } // JSON

    // Análisis de sentimiento
    public SentimentType Sentiment { get; set; } = SentimentType.Neutral;
    public decimal SentimentScore { get; set; }

    // Quick replies / Botones
    public string? QuickReplies { get; set; } // JSON array de opciones

    // Respuesta del bot
    public string? BotResponse { get; set; }
    public bool IsFromBot { get; set; }
    public int? ResponseTimeMs { get; set; }

    // Si se consumió una interacción del LLM
    public bool ConsumedInteraction { get; set; }
    public decimal? InteractionCost { get; set; } // Costo en USD

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ChatSession Session { get; set; } = null!;
}

/// <summary>
/// Lead generado por el chatbot
/// </summary>
public class ChatLead
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    // Información del prospecto
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? PreferredContactTime { get; set; }

    // Interés
    public Guid? InterestedVehicleId { get; set; }
    public string? InterestedVehicleName { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public bool InterestedInFinancing { get; set; }
    public bool InterestedInTradeIn { get; set; }
    public string? TradeInVehicleInfo { get; set; }

    // Calificación del lead
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadTemperature Temperature { get; set; } = LeadTemperature.Warm;
    public int QualificationScore { get; set; } // 0-100
    public string? QualificationNotes { get; set; }

    // Asignación
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime? AssignedAt { get; set; }

    // Seguimiento
    public DateTime? LastContactedAt { get; set; }
    public int ContactAttempts { get; set; }
    public string? Notes { get; set; }

    // Conversión
    public DateTime? ConvertedAt { get; set; }
    public Guid? ConvertedToSaleId { get; set; }
    public decimal? SaleAmount { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public virtual ChatSession Session { get; set; } = null!;
}
