using CarDealer.Contracts.Abstractions;

namespace CarDealer.Contracts.Events.Alert;

/// <summary>
/// Event published when a saved search is activated and the user should be notified
/// that their search criteria has been saved and is being monitored.
/// Also published when new vehicles matching the saved search are found.
/// 
/// Published by: AlertService
/// Consumed by: NotificationService (sends email confirmation/match notification)
/// 
/// Routing key: alert.savedsearch.activated
/// </summary>
public class SavedSearchMatchEvent : EventBase
{
    public override string EventType => "alert.savedsearch.activated";

    /// <summary>
    /// The saved search ID
    /// </summary>
    public Guid SavedSearchId { get; set; }

    /// <summary>
    /// User who created/activated the saved search
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User-given name for the search (e.g., "Toyota SUVs bajo 1M")
    /// </summary>
    public string SearchName { get; set; } = string.Empty;

    /// <summary>
    /// JSON string with the search criteria/filters
    /// </summary>
    public string SearchCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a new search creation or a re-activation
    /// </summary>
    public string ActionType { get; set; } = "created"; // "created" | "activated"

    /// <summary>
    /// Notification frequency preference
    /// </summary>
    public string Frequency { get; set; } = "Daily";

    /// <summary>
    /// User's email address for notification delivery
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Friendly description of the search filters for the email
    /// </summary>
    public string? SearchDescription { get; set; }
}
