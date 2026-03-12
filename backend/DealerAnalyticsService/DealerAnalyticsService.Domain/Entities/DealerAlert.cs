namespace DealerAnalyticsService.Domain.Entities;

using DealerAnalyticsService.Domain.Enums;

/// <summary>
/// Alertas automáticas para dealers
/// Generadas cuando se detectan condiciones que requieren atención
/// </summary>
public class DealerAlert
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DealerAlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; }

    // Alert Content
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }

    // Context Data (JSON serialized)
    public string? MetadataJson { get; set; }

    // Computed property for accessing metadata as Dictionary
    private Dictionary<string, string>? _metadata;
    public Dictionary<string, string>? Metadata
    {
        get
        {
            if (_metadata != null) return _metadata;
            if (string.IsNullOrEmpty(MetadataJson)) return null;
            try
            {
                _metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson);
                return _metadata;
            }
            catch
            {
                return null;
            }
        }
    }

    // Related Entities
    public Guid? RelatedVehicleId { get; set; }
    public Guid? RelatedLeadId { get; set; }

    // Thresholds (what triggered the alert)
    public string? TriggerCondition { get; set; }
    public double? CurrentValue { get; set; }
    public double? ThresholdValue { get; set; }

    // User Interaction
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime? DismissedAt { get; set; }
    public bool IsActedUpon { get; set; }
    public DateTime? ActedUponAt { get; set; }

    // Notification Tracking
    public bool EmailSent { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public bool PushSent { get; set; }
    public DateTime? PushSentAt { get; set; }
    public bool SmsSent { get; set; }
    public DateTime? SmsSentAt { get; set; }

    // Expiration
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Constructor
    public DealerAlert()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = AlertStatus.Active;
    }

    /// <summary>
    /// Factory method para crear alertas específicas
    /// </summary>
    public static DealerAlert Create(
        Guid dealerId,
        DealerAlertType type,
        AlertSeverity severity,
        string title,
        string message,
        string? actionUrl = null,
        TimeSpan? expiresIn = null)
    {
        return new DealerAlert
        {
            DealerId = dealerId,
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            ExpiresAt = expiresIn.HasValue ? DateTime.UtcNow.Add(expiresIn.Value) : null
        };
    }

    // Alert Factory Methods
    public static DealerAlert CreateLowInventoryAlert(Guid dealerId, int currentCount)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.LowInventory,
            AlertSeverity.Warning,
            "Inventario Bajo",
            $"Solo tiene {currentCount} vehículos activos. Agregue más para mantener visibilidad.",
            "/dealer/inventory/add",
            TimeSpan.FromDays(7));
        alert.CurrentValue = currentCount;
        alert.ThresholdValue = 5;
        alert.ActionLabel = "Agregar Vehículo";
        return alert;
    }

    public static DealerAlert CreateAgingInventoryAlert(Guid dealerId, Guid vehicleId, string vehicleTitle, int daysOnMarket)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.AgingInventory,
            AlertSeverity.Warning,
            "Vehículo Sin Vender",
            $"{vehicleTitle} lleva {daysOnMarket} días en el mercado. Considere ajustar el precio.",
            $"/dealer/inventory/{vehicleId}/edit",
            TimeSpan.FromDays(7));
        alert.RelatedVehicleId = vehicleId;
        alert.CurrentValue = daysOnMarket;
        alert.ThresholdValue = 60;
        alert.ActionLabel = "Editar Precio";
        return alert;
    }

    public static DealerAlert CreateSlowResponseAlert(Guid dealerId, Guid leadId, double hoursWithoutResponse)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.LeadResponseSlow,
            AlertSeverity.High,
            "Lead Sin Responder",
            $"Un lead lleva {hoursWithoutResponse:F1} horas sin respuesta. Responda pronto.",
            $"/dealer/leads/{leadId}",
            TimeSpan.FromHours(24));
        alert.RelatedLeadId = leadId;
        alert.CurrentValue = hoursWithoutResponse;
        alert.ThresholdValue = 4;
        alert.ActionLabel = "Responder";
        return alert;
    }

    public static DealerAlert CreateViewsDroppingAlert(Guid dealerId, double percentDrop)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.ViewsDropping,
            AlertSeverity.Medium,
            "Vistas en Descenso",
            $"Las vistas han bajado {percentDrop:F0}% esta semana. Mejore sus listings.",
            "/dealer/analytics/insights",
            TimeSpan.FromDays(7));
        alert.CurrentValue = percentDrop;
        alert.ThresholdValue = 30;
        alert.ActionLabel = "Ver Recomendaciones";
        return alert;
    }

    public static DealerAlert CreateConversionDroppingAlert(Guid dealerId, double currentRate, double previousRate)
    {
        var percentDrop = previousRate > 0 ? ((previousRate - currentRate) / previousRate) * 100 : 0;
        var alert = Create(
            dealerId,
            DealerAlertType.ConversionDropping,
            AlertSeverity.High,
            "Conversión en Descenso",
            $"Su tasa de conversión bajó de {previousRate:F1}% a {currentRate:F1}%. Revise su estrategia.",
            "/dealer/analytics/funnel",
            TimeSpan.FromDays(7));
        alert.CurrentValue = currentRate;
        alert.ThresholdValue = previousRate;
        alert.ActionLabel = "Analizar Funnel";
        return alert;
    }

    public static DealerAlert CreateGoalAchievedAlert(Guid dealerId, string goalName, double achievedValue)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.GoalAchieved,
            AlertSeverity.Info,
            "¡Meta Alcanzada! 🎉",
            $"Felicidades, ha alcanzado su meta de {goalName}: {achievedValue:N0}",
            "/dealer/analytics",
            TimeSpan.FromDays(30));
        alert.CurrentValue = achievedValue;
        alert.ActionLabel = "Ver Progreso";
        return alert;
    }

    // Additional Factory Methods for AlertAnalysisService

    public static DealerAlert CreateLowInventoryAlert(Guid dealerId, int currentCount, int threshold)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.LowInventory,
            AlertSeverity.Warning,
            "Inventario Bajo",
            $"Solo tiene {currentCount} vehículos activos (mínimo recomendado: {threshold}). Agregue más para mantener visibilidad.",
            "/dealer/inventory/add",
            TimeSpan.FromDays(7));
        alert.CurrentValue = currentCount;
        alert.ThresholdValue = threshold;
        alert.ActionLabel = "Agregar Vehículo";
        return alert;
    }

    public static DealerAlert CreateViewsDroppingAlert(Guid dealerId, double percentDrop, int currentViews, int previousViews)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.ViewsDropping,
            AlertSeverity.Medium,
            "Vistas en Descenso",
            $"Las vistas han bajado {Math.Abs(percentDrop * 100):F0}% (de {previousViews:N0} a {currentViews:N0}). Mejore sus listings.",
            "/dealer/analytics/insights",
            TimeSpan.FromDays(7));
        alert.CurrentValue = currentViews;
        alert.ThresholdValue = previousViews;
        alert.MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { percentDrop, previousViews, currentViews });
        alert.ActionLabel = "Ver Recomendaciones";
        return alert;
    }

    public static DealerAlert CreateLeadResponseSlowAlert(Guid dealerId, int avgResponseMinutes, int thresholdMinutes)
    {
        var hours = avgResponseMinutes / 60.0;
        var alert = Create(
            dealerId,
            DealerAlertType.LeadResponseSlow,
            AlertSeverity.High,
            "Respuesta Lenta a Leads",
            $"Su tiempo promedio de respuesta es {hours:F1} horas. Responda dentro de {thresholdMinutes} minutos para mejor conversión.",
            "/dealer/leads",
            TimeSpan.FromHours(24));
        alert.CurrentValue = avgResponseMinutes;
        alert.ThresholdValue = thresholdMinutes;
        alert.ActionLabel = "Ver Leads Pendientes";
        return alert;
    }

    public static DealerAlert CreateConversionDropAlert(Guid dealerId, double percentDrop, decimal currentRate, decimal previousRate)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.ConversionDropping,
            AlertSeverity.High,
            "Conversión en Descenso",
            $"Su tasa de conversión bajó de {previousRate * 100:F1}% a {currentRate * 100:F1}% (-{Math.Abs(percentDrop * 100):F0}%). Revise su estrategia.",
            "/dealer/analytics/funnel",
            TimeSpan.FromDays(7));
        alert.CurrentValue = (double)currentRate * 100;
        alert.ThresholdValue = (double)previousRate * 100;
        alert.MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { percentDrop, currentRate, previousRate });
        alert.ActionLabel = "Analizar Funnel";
        return alert;
    }

    public static DealerAlert CreateAgingInventoryAlert(Guid dealerId, int vehicleCount, decimal totalValue)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.AgingInventory,
            AlertSeverity.Warning,
            "Inventario Antiguo",
            $"Tiene {vehicleCount} vehículos con más de 90 días en el mercado (valor: ${totalValue:N0}). Considere ajustar precios.",
            "/dealer/inventory?filter=aging",
            TimeSpan.FromDays(7));
        alert.CurrentValue = vehicleCount;
        alert.ThresholdValue = 90;
        alert.MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { vehicleCount, totalValue });
        alert.ActionLabel = "Ver Vehículos";
        return alert;
    }

    public static DealerAlert CreateHighDemandAlert(Guid dealerId, Guid vehicleId, int views, int contacts)
    {
        var alert = Create(
            dealerId,
            DealerAlertType.HotLeadReceived,
            AlertSeverity.Info,
            "¡Vehículo en Alta Demanda! 🔥",
            $"Un vehículo tiene {views} vistas y {contacts} contactos. Considere actualizar el precio o responder rápido a los interesados.",
            $"/dealer/inventory/{vehicleId}",
            TimeSpan.FromDays(3));
        alert.RelatedVehicleId = vehicleId;
        alert.CurrentValue = views;
        alert.ThresholdValue = 50;
        alert.MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { vehicleId, views, contacts });
        alert.ActionLabel = "Ver Vehículo";
        return alert;
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Dismiss()
    {
        IsDismissed = true;
        DismissedAt = DateTime.UtcNow;
        Status = AlertStatus.Dismissed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsActedUpon()
    {
        IsActedUpon = true;
        ActedUponAt = DateTime.UtcNow;
        Status = AlertStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }
}
