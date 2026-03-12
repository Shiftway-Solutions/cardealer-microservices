using MediaService.Application.DTOs;

namespace MediaService.Application.Interfaces;

/// <summary>
/// Service responsible for sending image health report emails to administrators.
/// </summary>
public interface IImageHealthReportEmailService
{
    /// <summary>
    /// Sends the image health scan report to the OKLA administrator via email.
    /// </summary>
    /// <param name="report">The health report to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendReportAsync(ImageHealthReportDto report, CancellationToken cancellationToken = default);
}
