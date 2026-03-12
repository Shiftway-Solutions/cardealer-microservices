namespace NotificationService.Domain.Interfaces.External;

public interface ISmsProvider
{
    Task<(bool success, string? messageId, string? error)> SendAsync(
        string to,
        string message,
        Dictionary<string, object>? metadata = null);

    string ProviderName { get; }
}