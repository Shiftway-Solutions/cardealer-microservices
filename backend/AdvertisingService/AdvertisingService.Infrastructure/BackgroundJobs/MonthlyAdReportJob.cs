using AdvertisingService.Application.Clients;
using AdvertisingService.Application.Interfaces;
using AdvertisingService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvertisingService.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that generates and sends monthly advertising performance reports
/// to all dealers and sellers with active or recently-active campaigns.
/// Runs on the 1st of each month at 10:00 UTC (6:00 AM Dominican Republic time).
/// </summary>
public class MonthlyAdReportJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyAdReportJob> _logger;

    // 10:00 UTC = 6:00 AM Dominican Republic time (on the 1st of each month)
    private static readonly TimeSpan TargetTimeUtc = new(10, 0, 0);

    public MonthlyAdReportJob(
        IServiceScopeFactory scopeFactory,
        ILogger<MonthlyAdReportJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonthlyAdReportJob started, will run on the 1st of each month at {Time} UTC", TargetTimeUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = CalculateNextFirstOfMonth(now);

            var delay = nextRun - now;
            _logger.LogInformation("Next monthly report run scheduled for {NextRun} (in {Delay})", nextRun, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await GenerateMonthlyReportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly advertising reports");
            }
        }

        _logger.LogInformation("MonthlyAdReportJob stopped");
    }

    /// <summary>
    /// Calculates the next 1st of the month at the target time.
    /// If today is the 1st and before target time, runs today.
    /// Otherwise, schedules for the 1st of next month.
    /// </summary>
    private static DateTime CalculateNextFirstOfMonth(DateTime now)
    {
        // Try current month's 1st
        var thisMonthFirst = new DateTime(now.Year, now.Month, 1, TargetTimeUtc.Hours, TargetTimeUtc.Minutes, 0, DateTimeKind.Utc);

        if (thisMonthFirst > now)
            return thisMonthFirst;

        // Move to next month
        var nextMonth = now.Month == 12
            ? new DateTime(now.Year + 1, 1, 1, TargetTimeUtc.Hours, TargetTimeUtc.Minutes, 0, DateTimeKind.Utc)
            : new DateTime(now.Year, now.Month + 1, 1, TargetTimeUtc.Hours, TargetTimeUtc.Minutes, 0, DateTimeKind.Utc);

        return nextMonth;
    }

    private async Task GenerateMonthlyReportsAsync(CancellationToken ct)
    {
        var reportMonth = DateTime.UtcNow.AddMonths(-1);
        var monthStart = new DateTime(reportMonth.Year, reportMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);
        var monthName = reportMonth.ToString("MMMM yyyy");

        _logger.LogInformation("Generating monthly advertising reports for {Month}...", monthName);

        using var scope = _scopeFactory.CreateScope();
        var reportingService = scope.ServiceProvider.GetRequiredService<IAdReportingService>();
        var campaignRepo = scope.ServiceProvider.GetRequiredService<IAdCampaignRepository>();
        var notificationClient = scope.ServiceProvider.GetRequiredService<NotificationServiceClient>();
        var userClient = scope.ServiceProvider.GetRequiredService<UserServiceClient>();

        // Get platform-wide stats for the month
        var platformReport = await reportingService.GetPlatformReportAsync(monthStart, ct);

        _logger.LogInformation(
            "Monthly Platform Report for {Month}: {ActiveCampaigns} active campaigns, " +
            "{Impressions} impressions, {Clicks} clicks, CTR={Ctr:P2}, Revenue=${Revenue:N2}",
            monthName,
            platformReport.TotalActiveCampaigns,
            platformReport.TotalImpressions,
            platformReport.TotalClicks,
            platformReport.OverallCtr,
            platformReport.TotalRevenue);

        // Get all owner IDs with active campaigns (current + recently expired)
        var ownerIds = await campaignRepo.GetDistinctOwnerIdsWithActiveCampaignsAsync(ct);
        _logger.LogInformation("Sending monthly report emails to {OwnerCount} campaign owners", ownerIds.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var ownerId in ownerIds)
        {
            try
            {
                // Determine owner type
                var ownerCampaigns = await campaignRepo.GetByOwnerAsync(ownerId, "Individual", null, 1, 1, ct);
                var ownerType = ownerCampaigns.Count > 0 ? "Individual" : "Dealer";
                if (ownerCampaigns.Count == 0)
                {
                    ownerCampaigns = await campaignRepo.GetByOwnerAsync(ownerId, "Dealer", null, 1, 1, ct);
                    if (ownerCampaigns.Count == 0) continue;
                }

                // Get owner's email from UserService
                var userInfo = await userClient.GetUserInfoAsync(ownerId, ct);
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogWarning("Could not resolve email for owner {OwnerId}, skipping monthly report", ownerId);
                    failCount++;
                    continue;
                }

                // Get owner's monthly report data
                var ownerReport = await reportingService.GetOwnerReportAsync(ownerId, ownerType, monthStart, ct);

                // Render the HTML email
                var htmlBody = RenderMonthlyReportEmail(
                    userInfo.FullName ?? userInfo.Email,
                    monthName,
                    ownerReport.ActiveCampaigns,
                    ownerReport.TotalCampaigns,
                    ownerReport.TotalImpressions,
                    ownerReport.TotalClicks,
                    ownerReport.OverallCtr,
                    ownerReport.TotalSpent);

                var subject = $"📊 Reporte Mensual de Publicidad - {monthName} | OKLA";

                await notificationClient.SendEmailAsync(
                    userInfo.Email,
                    subject,
                    htmlBody,
                    ct);

                successCount++;
                _logger.LogDebug("Monthly report email sent to {Email} (owner {OwnerId})", userInfo.Email, ownerId);

                // Small delay between emails to avoid overwhelming the notification service
                await Task.Delay(500, ct);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogWarning(ex, "Failed to send monthly report to owner {OwnerId}", ownerId);
            }
        }

        _logger.LogInformation(
            "Monthly report generation complete for {Month}: {Success} sent, {Failed} failed out of {Total} owners",
            monthName, successCount, failCount, ownerIds.Count);
    }

    /// <summary>
    /// Renders a professional HTML email for the monthly advertising performance report.
    /// All text is in Spanish for the Dominican Republic market.
    /// </summary>
    private static string RenderMonthlyReportEmail(
        string ownerName,
        string monthName,
        int activeCampaigns,
        int totalCampaigns,
        long totalImpressions,
        long totalClicks,
        decimal ctr,
        decimal totalSpent)
    {
        var ctrPercent = (ctr * 100).ToString("F2");
        var impressionsFormatted = totalImpressions.ToString("N0");
        var clicksFormatted = totalClicks.ToString("N0");
        var spentFormatted = totalSpent.ToString("N2");

        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reporte Mensual de Publicidad - OKLA</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #1a56db, #3b82f6); color: white; padding: 30px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .header p {{ margin: 8px 0 0; font-size: 14px; opacity: 0.9; }}
        .content {{ padding: 30px 20px; }}
        .greeting {{ font-size: 16px; margin-bottom: 20px; }}
        .stats-grid {{ display: flex; flex-wrap: wrap; gap: 12px; margin: 20px 0; }}
        .stat-card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; flex: 1; min-width: 120px; text-align: center; }}
        .stat-value {{ font-size: 28px; font-weight: 700; color: #1a56db; display: block; }}
        .stat-label {{ font-size: 12px; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; margin-top: 4px; }}
        .highlight {{ background: #eff6ff; border-left: 4px solid #3b82f6; padding: 16px; margin: 20px 0; border-radius: 0 8px 8px 0; }}
        .cta-button {{ display: inline-block; background: #1a56db; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; margin: 20px 0; }}
        .cta-button:hover {{ background: #1e40af; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 20px; text-align: center; font-size: 12px; }}
        .footer a {{ color: #60a5fa; text-decoration: none; }}
        .divider {{ border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0; }}
        .tips {{ background: #fefce8; border: 1px solid #fde68a; border-radius: 8px; padding: 16px; margin: 20px 0; }}
        .tips h3 {{ margin: 0 0 8px; color: #92400e; font-size: 14px; }}
        .tips ul {{ margin: 0; padding-left: 20px; font-size: 13px; color: #78350f; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📊 Reporte Mensual de Publicidad</h1>
            <p>{monthName}</p>
        </div>
        <div class=""content"">
            <p class=""greeting"">Hola <strong>{System.Net.WebUtility.HtmlEncode(ownerName)}</strong>,</p>
            <p>Aquí tienes el resumen de rendimiento de tu publicidad en OKLA durante <strong>{monthName}</strong>:</p>

            <div class=""stats-grid"">
                <div class=""stat-card"">
                    <span class=""stat-value"">{impressionsFormatted}</span>
                    <span class=""stat-label"">Impresiones</span>
                </div>
                <div class=""stat-card"">
                    <span class=""stat-value"">{clicksFormatted}</span>
                    <span class=""stat-label"">Clics</span>
                </div>
                <div class=""stat-card"">
                    <span class=""stat-value"">{ctrPercent}%</span>
                    <span class=""stat-label"">CTR</span>
                </div>
                <div class=""stat-card"">
                    <span class=""stat-value"">RD${spentFormatted}</span>
                    <span class=""stat-label"">Invertido</span>
                </div>
            </div>

            <div class=""highlight"">
                <strong>📋 Resumen de Campañas</strong><br>
                Campañas activas: <strong>{activeCampaigns}</strong> de {totalCampaigns} total
            </div>

            <hr class=""divider"">

            <div class=""tips"">
                <h3>💡 Consejos para mejorar tu rendimiento</h3>
                <ul>
                    <li>Agrega fotos de alta calidad a tus vehículos para aumentar el CTR</li>
                    <li>Actualiza tus precios regularmente para mantenerte competitivo</li>
                    <li>Utiliza las campañas ""Oferta del Día"" para mayor visibilidad</li>
                    <li>Responde rápido a las consultas — los compradores eligen al más ágil</li>
                </ul>
            </div>

            <p style=""text-align: center;"">
                <a href=""https://okla.com.do/impulsar/mis-campanas"" class=""cta-button"">Ver mis campañas →</a>
            </p>

            <p style=""font-size: 13px; color: #64748b;"">
                Este reporte se genera automáticamente el primer día de cada mes.
                Si tienes preguntas sobre tu publicidad, contáctanos en
                <a href=""mailto:soporte@okla.com.do"" style=""color: #3b82f6;"">soporte@okla.com.do</a>.
            </p>
        </div>
        <div class=""footer"">
            <p><strong>OKLA</strong> — Tu mercado de vehículos en República Dominicana</p>
            <p>
                <a href=""https://okla.com.do"">okla.com.do</a> |
                <a href=""https://okla.com.do/soporte"">Soporte</a> |
                <a href=""https://okla.com.do/privacidad"">Privacidad</a>
            </p>
            <p>&copy; {DateTime.UtcNow.Year} OKLA. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
    }
}
