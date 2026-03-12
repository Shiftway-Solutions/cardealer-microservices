namespace NotificationService.Domain.Interfaces.External;

public interface IEmailProvider
{
    Task<(bool success, string? messageId, string? error)> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        Dictionary<string, object>? metadata = null);

    string ProviderName { get; }
}