namespace NotificationService.Domain.Interfaces.External;

public interface IPushNotificationProvider
{
    Task<(bool success, string? messageId, string? error)> SendAsync(
        string deviceToken,
        string title,
        string body,
        object? data = null,
        Dictionary<string, object>? metadata = null);

    string ProviderName { get; }
}