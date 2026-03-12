namespace ChatbotService.Domain.Enums;

/// <summary>
/// Modo de operación del chatbot — determina la estrategia de contexto
/// </summary>
public enum ChatMode
{
    /// <summary>Chat sobre un vehículo específico (contexto fijo)</summary>
    SingleVehicle = 1,

    /// <summary>Chat con acceso al inventario completo del dealer (RAG + function calling)</summary>
    DealerInventory = 2,

    /// <summary>Chat general sin contexto de vehículo (FAQ, soporte)</summary>
    General = 3
}

/// <summary>
/// Estado del handoff bot ↔ humano para WhatsApp
/// </summary>
public enum HandoffStatus
{
    /// <summary>Bot responde automáticamente</summary>
    BotActive = 1,

    /// <summary>Agente humano tomó control</summary>
    HumanActive = 2,

    /// <summary>Handoff solicitado, esperando agente</summary>
    PendingHuman = 3,

    /// <summary>Agente devolvió control al bot</summary>
    ReturnedToBot = 4
}

/// <summary>
/// Tipos de sesión de chat
/// </summary>
public enum SessionType
{
    WebChat = 1,
    WhatsApp = 2,
    FacebookMessenger = 3,
    Instagram = 4,
    Telegram = 5,
    SMS = 6,
    VoiceCall = 7
}

/// <summary>
/// Estado de la sesión de chat
/// </summary>
public enum SessionStatus
{
    Active = 1,
    Paused = 2,
    Completed = 3,
    Expired = 4,
    TransferredToAgent = 5,
    Abandoned = 6,
    /// <summary>Sesión en modo humano (dealer respondiendo)</summary>
    HumanTakeover = 7
}

/// <summary>
/// Tipo de function call que el LLM puede solicitar
/// </summary>
public enum FunctionCallType
{
    /// <summary>Buscar vehículos en inventario del dealer</summary>
    SearchInventory = 1,

    /// <summary>Comparar 2-3 vehículos lado a lado</summary>
    CompareVehicles = 2,

    /// <summary>Agendar cita para ver un vehículo</summary>
    ScheduleAppointment = 3,

    /// <summary>Obtener detalles de un vehículo específico</summary>
    GetVehicleDetails = 4,

    /// <summary>Calcular financiamiento</summary>
    CalculateFinancing = 5,

    /// <summary>Obtener info del dealer (horarios, ubicación)</summary>
    GetDealerInfo = 6
}

/// <summary>
/// Tipo de mensaje
/// </summary>
public enum MessageType
{
    UserText = 1,
    BotText = 2,
    QuickReply = 3,
    Image = 4,
    Video = 5,
    Document = 6,
    Location = 7,
    Contact = 8,
    SystemMessage = 9,
    CarouselCard = 10
}

/// <summary>
/// Categorías de intención del chatbot
/// </summary>
public enum IntentCategory
{
    // Consultas generales
    Greeting = 1,
    Farewell = 2,
    Help = 3,
    Fallback = 4,

    // Inventario y vehículos
    VehicleSearch = 10,
    VehicleDetails = 11,
    VehicleComparison = 12,
    VehicleAvailability = 13,
    VehiclePrice = 14,
    VehicleFeatures = 15,

    // Financiamiento
    FinancingInfo = 20,
    FinancingCalculation = 21,
    FinancingRequirements = 22,
    TradeIn = 23,

    // Citas y test drives
    TestDriveSchedule = 30,
    AppointmentSchedule = 31,
    AppointmentCancel = 32,
    AppointmentReschedule = 33,

    // Información del dealer
    DealerLocation = 40,
    DealerHours = 41,
    DealerContact = 42,
    DealerServices = 43,

    // Postventa
    ServiceAppointment = 50,
    WarrantyInfo = 51,
    PartsInquiry = 52,

    // Lead generation
    ContactRequest = 60,
    QuoteRequest = 61,
    CallbackRequest = 62,

    // Otros
    Complaint = 70,
    Feedback = 71,
    Other = 99
}

/// <summary>
/// Nivel de confianza de la respuesta del chatbot
/// </summary>
public enum ConfidenceLevel
{
    VeryLow = 1,    // < 20%
    Low = 2,        // 20-40%
    Medium = 3,     // 40-60%
    High = 4,       // 60-80%
    VeryHigh = 5    // > 80%
}

/// <summary>
/// Sentimiento detectado en el mensaje
/// </summary>
public enum SentimentType
{
    VeryNegative = 1,
    Negative = 2,
    Neutral = 3,
    Positive = 4,
    VeryPositive = 5
}

/// <summary>
/// Estado del lead generado por el chatbot
/// </summary>
public enum LeadStatus
{
    New = 1,
    Qualified = 2,
    Contacted = 3,
    InProgress = 4,
    Converted = 5,
    Lost = 6,
    Disqualified = 7
}

/// <summary>
/// Temperatura del lead (calidad)
/// </summary>
public enum LeadTemperature
{
    Cold = 1,
    Warm = 2,
    Hot = 3
}

/// <summary>
/// Estado de la tarea de mantenimiento automático
/// </summary>
public enum MaintenanceTaskStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5,
    Cancelled = 6
}

/// <summary>
/// Tipo de tarea de mantenimiento automático
/// </summary>
public enum MaintenanceTaskType
{
    // Sincronización
    InventorySync = 1,
    PriceSync = 2,

    // Monitoreo
    HealthCheck = 10,
    ErrorAnalysis = 11,
    PerformanceCheck = 12,

    // Reportes
    DailyReport = 20,
    WeeklyReport = 21,
    MonthlyReport = 22,

    // Backup
    DataBackup = 30,
    ConversationBackup = 31,

    // Optimización
    IntentAnalysis = 40,
    ConversationAnalysis = 41,
    AutoLearning = 42,

    // Limpieza
    SessionCleanup = 50,
    LogCleanup = 51,
    CacheCleanup = 52
}

/// <summary>
/// Plan de suscripción del chatbot
/// </summary>
public enum ChatbotPlan
{
    Free = 1,           // 180 interacciones/mes gratis
    Standard = 2,       // $0.002 por interacción adicional
    Enterprise = 3      // $0.004 por interacción (sin límites)
}

/// <summary>
/// Estado del uso de interacciones
/// </summary>
public enum UsageStatus
{
    Normal = 1,
    Warning = 2,        // > 80% del límite
    Critical = 3,       // > 95% del límite
    Exceeded = 4        // Sobrepasó el límite
}

/// <summary>
/// Tipo de límite de interacciones
/// </summary>
public enum InteractionLimitType
{
    PerSession = 1,     // Límite por sesión
    PerDay = 2,         // Límite diario por usuario
    PerMonth = 3,       // Límite mensual por usuario
    GlobalDaily = 4,    // Límite diario global (todo el sistema)
    GlobalMonthly = 5   // Límite mensual global
}
