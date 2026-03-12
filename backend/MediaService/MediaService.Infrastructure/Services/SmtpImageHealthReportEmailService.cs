using System.Net;
using System.Net.Mail;
using System.Text;
using MediaService.Application.DTOs;
using MediaService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaService.Infrastructure.Services;

/// <summary>
/// Sends image health scan reports via SMTP email to the OKLA administrator.
/// </summary>
public class SmtpImageHealthReportEmailService : IImageHealthReportEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpImageHealthReportEmailService> _logger;

    public SmtpImageHealthReportEmailService(
        IConfiguration configuration,
        ILogger<SmtpImageHealthReportEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendReportAsync(ImageHealthReportDto report, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["ImageHealthScan:Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["ImageHealthScan:Email:SmtpPort"] ?? "587");
        var smtpUser = _configuration["ImageHealthScan:Email:SmtpUser"] ?? "";
        var smtpPassword = _configuration["ImageHealthScan:Email:SmtpPassword"] ?? "";
        var fromEmail = _configuration["ImageHealthScan:Email:From"] ?? "noreply@okla.com.do";
        var toEmail = _configuration["ImageHealthScan:Email:AdminRecipient"] ?? "admin@okla.com.do";
        var enableSsl = bool.Parse(_configuration["ImageHealthScan:Email:EnableSsl"] ?? "true");

        if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
        {
            _logger.LogWarning("📧 SMTP credentials not configured. Image health report will NOT be sent by email. " +
                               "Configure ImageHealthScan:Email:SmtpUser and SmtpPassword.");
            return;
        }

        try
        {
            var subject = $"🖼️ OKLA Image Health Report — {report.HealthPercentage}% Healthy — {report.GeneratedAtUtc:yyyy-MM-dd}";
            var body = BuildHtmlBody(report);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = enableSsl
            };

            var message = new MailMessage(fromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("📧 Image health report sent to {Recipient} — {HealthPct}% healthy, {Broken}/{Total} broken",
                toEmail, report.HealthPercentage, report.BrokenUrlCount, report.TotalImagesScanned);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "📧 Failed to send image health report email to {Recipient}", toEmail);
        }
    }

    private static string BuildHtmlBody(ImageHealthReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1 { color: #333; } h2 { color: #555; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin: 10px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #4CAF50; color: white; }");
        sb.AppendLine(".healthy { color: #4CAF50; font-weight: bold; }");
        sb.AppendLine(".warning { color: #FF9800; font-weight: bold; }");
        sb.AppendLine(".critical { color: #F44336; font-weight: bold; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<h1>🖼️ OKLA — Image Health Scan Report</h1>");
        sb.AppendLine($"<p><strong>Generated:</strong> {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p><strong>Scan Duration:</strong> {report.ScanDurationSeconds:F1} seconds</p>");

        // Health Summary
        var healthClass = report.HealthPercentage >= 95 ? "healthy" : report.HealthPercentage >= 80 ? "warning" : "critical";
        sb.AppendLine("<h2>📊 Summary</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><td>Total Images Scanned</td><td><strong>{report.TotalImagesScanned:N0}</strong></td></tr>");
        sb.AppendLine($"<tr><td>Healthy URLs</td><td class='healthy'>{report.HealthyUrlCount:N0}</td></tr>");
        sb.AppendLine($"<tr><td>Broken URLs</td><td class='critical'>{report.BrokenUrlCount:N0}</td></tr>");
        sb.AppendLine($"<tr><td>Timeouts (&gt;5s)</td><td>{report.TimeoutCount:N0}</td></tr>");
        sb.AppendLine($"<tr><td>Health Percentage</td><td class='{healthClass}'>{report.HealthPercentage}%</td></tr>");
        sb.AppendLine("</table>");

        // Breakdown by HTTP status
        if (report.BrokenByStatusCode.Count > 0)
        {
            sb.AppendLine("<h2>🔍 Broken URLs by HTTP Status Code</h2>");
            sb.AppendLine("<table><tr><th>HTTP Status</th><th>Count</th></tr>");
            foreach (var kvp in report.BrokenByStatusCode.OrderByDescending(x => x.Value))
            {
                var statusLabel = kvp.Key switch
                {
                    0 => "Timeout / Connection Error",
                    403 => "403 Forbidden",
                    404 => "404 Not Found",
                    410 => "410 Gone",
                    500 => "500 Internal Server Error",
                    _ => $"{kvp.Key}"
                };
                sb.AppendLine($"<tr><td>{statusLabel}</td><td>{kvp.Value:N0}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Top 10 dealers
        if (report.TopDealersWithBrokenImages.Count > 0)
        {
            sb.AppendLine("<h2>🏢 Top 10 Dealers with Most Broken Images</h2>");
            sb.AppendLine("<table><tr><th>#</th><th>Dealer ID</th><th>Broken Images</th></tr>");
            for (var i = 0; i < report.TopDealersWithBrokenImages.Count; i++)
            {
                var dealer = report.TopDealersWithBrokenImages[i];
                sb.AppendLine($"<tr><td>{i + 1}</td><td>{dealer.DealerId}</td><td class='critical'>{dealer.BrokenCount:N0}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<hr/>");
        sb.AppendLine("<p style='color: #999; font-size: 12px;'>This report is auto-generated by MediaService ImageUrlHealthScanJob. " +
                       "Do not reply to this email.</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }
}
