using CarDealer.Contracts.Abstractions;

namespace CarDealer.Contracts.Events.Alert;

/// <summary>
/// Event published when a price alert is triggered because a vehicle's price
/// dropped below (or met) the user's target price.
/// 
/// Published by: AlertService
/// Consumed by: NotificationService (sends email/SMS/push to the user)
/// 
/// Routing key: alert.price.triggered
/// </summary>
public class PriceAlertTriggeredEvent : EventBase
{
    public override string EventType => "alert.price.triggered";

    /// <summary>
    /// The price alert ID
    /// </summary>
    public Guid AlertId { get; set; }

    /// <summary>
    /// User who created the alert and should be notified
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Vehicle that had the price change
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Vehicle display title (e.g., "2024 Toyota Corolla")
    /// </summary>
    public string VehicleTitle { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle URL slug for building the link
    /// </summary>
    public string? VehicleSlug { get; set; }

    /// <summary>
    /// Previous price before the change
    /// </summary>
    public decimal OldPrice { get; set; }

    /// <summary>
    /// New price after the change
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// User's target price that was met
    /// </summary>
    public decimal TargetPrice { get; set; }

    /// <summary>
    /// Currency code (e.g., "DOP")
    /// </summary>
    public string Currency { get; set; } = "DOP";

    /// <summary>
    /// Alert condition that was met (e.g., "Below", "Equals", "Any")
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// User's email address for notification delivery
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// User's phone number for SMS notification (optional)
    /// </summary>
    public string? UserPhone { get; set; }
}
