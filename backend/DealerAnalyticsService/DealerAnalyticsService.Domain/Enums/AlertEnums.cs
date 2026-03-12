namespace DealerAnalyticsService.Domain.Enums;

/// <summary>
/// Tipos de alertas para dealers
/// </summary>
public enum DealerAlertType
{
    // Inventory Alerts
    LowInventory = 1,
    AgingInventory = 2,
    PriceAdjustmentNeeded = 3,
    InventoryImbalance = 4,

    // Engagement Alerts
    ViewsDropping = 10,
    LowContactRate = 11,
    LowFavoriteRate = 12,
    PoorListingQuality = 13,

    // Lead Alerts
    LeadResponseSlow = 20,
    LeadsNotFollowedUp = 21,
    ConversionDropping = 22,
    HotLeadReceived = 23,
    ConversionRateDropping = 24,
    HighDemandVehicle = 25,

    // Competitive Alerts
    CompetitorPriceLower = 30,
    RankingDropped = 31,
    MarketTrendChange = 32,

    // Performance Alerts
    BadReviewReceived = 40,
    GoalNotMet = 41,
    GoalAchieved = 42,
    TierChanged = 43,

    // System Alerts
    SubscriptionExpiring = 50,
    DocumentExpiring = 51,
    ActionRequired = 52
}

/// <summary>
/// Severidad de la alerta
/// </summary>
public enum AlertSeverity
{
    Info = 1,     // Informativo
    Low = 2,      // Baja prioridad
    Medium = 3,   // Media prioridad
    Warning = 4,  // Advertencia
    High = 5,     // Alta prioridad
    Critical = 6  // Crítico - requiere acción inmediata
}

/// <summary>
/// Estado de la alerta
/// </summary>
public enum AlertStatus
{
    Active = 1,
    Unread = 1, // Alias for Active
    Read = 2,
    Dismissed = 3,
    Resolved = 4,
    Expired = 5
}
