namespace NotificationService.Application.DTOs;

/// <summary>
/// Request to send a Teams alert via Adaptive Card.
/// </summary>
public record TeamsAlertRequest(
    string Title,
    string Message,
    string? Severity = "Info",
    Dictionary<string, string>? Metadata = null
);
