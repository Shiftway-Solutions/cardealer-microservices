using AdvertisingService.Application.Clients;
using AdvertisingService.Application.Interfaces;
using AdvertisingService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvertisingService.Infrastructure.BackgroundJobs;

public class DailyAdReportJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyAdReportJob> _logger;

    // 12:00 UTC = 8:00 AM Dominican Republic time
    private static readonly TimeSpan TargetTimeUtc = new(12, 0, 0);

    public DailyAdReportJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyAdReportJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyAdReportJob started, will run daily at {Time} UTC", TargetTimeUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.Add(TargetTimeUtc);
            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogDebug("Next daily report run in {Delay}", delay);

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
                await GenerateDailyReportAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily ad report");
            }
        }

        _logger.LogInformation("DailyAdReportJob stopped");
    }

    private async Task GenerateDailyReportAsync(CancellationToken ct)
    {
        _logger.LogInformation("Generating daily advertising report...");

        using var scope = _scopeFactory.CreateScope();
        var reportingService = scope.ServiceProvider.GetRequiredService<IAdReportingService>();
        var campaignRepo = scope.ServiceProvider.GetRequiredService<IAdCampaignRepository>();
        var notificationClient = scope.ServiceProvider.GetRequiredService<NotificationServiceClient>();
        var userClient = scope.ServiceProvider.GetRequiredService<UserServiceClient>();

        var yesterday = DateTime.UtcNow.AddDays(-1).Date;
        var platformReport = await reportingService.GetPlatformReportAsync(yesterday, ct);

        _logger.LogInformation(
            "Daily Report: {ActiveCampaigns} active campaigns, {Impressions} impressions, {Clicks} clicks, CTR={Ctr:P2}, Revenue={Revenue:C2}",
            platformReport.TotalActiveCampaigns,
            platformReport.TotalImpressions,
            platformReport.TotalClicks,
            platformReport.OverallCtr,
            platformReport.TotalRevenue);

        // Check for campaigns that need expiration
        var activeCampaigns = await campaignRepo.GetActiveCampaignsForReportingAsync(yesterday, ct);
        foreach (var campaign in activeCampaigns.Where(c => c.IsExpired()))
        {
            campaign.MarkExpired();
            await campaignRepo.UpdateAsync(campaign, ct);
            _logger.LogInformation("Campaign {CampaignId} marked as expired", campaign.Id);
        }

        // Send daily report email to each owner with active campaigns
        var ownerIds = await campaignRepo.GetDistinctOwnerIdsWithActiveCampaignsAsync(ct);
        _logger.LogInformation("Sending daily report emails to {OwnerCount} owners", ownerIds.Count);

        foreach (var ownerId in ownerIds)
        {
            try
            {
                // Determine ownerType from campaigns (Individual or Dealer)
                var ownerCampaigns = await campaignRepo.GetByOwnerAsync(ownerId, "Individual", null, 1, 1, ct);
                var ownerType = ownerCampaigns.Count > 0 ? "Individual" : "Dealer";
                if (ownerCampaigns.Count == 0)
                {
                    ownerCampaigns = await campaignRepo.GetByOwnerAsync(ownerId, "Dealer", null, 1, 1, ct);
                    if (ownerCampaigns.Count == 0) continue; // No campaigns found for this owner
                }

                // Get user email from UserService
                var userInfo = await userClient.GetUserInfoAsync(ownerId, ct);
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogWarning("Could not resolve email for owner {OwnerId}, skipping daily report", ownerId);
                    continue;
                }

                var ownerReport = await reportingService.GetOwnerReportAsync(ownerId, ownerType, yesterday, ct);

                var subject = $"📈 Reporte Diario de Publicidad - {yesterday:dd/MM/yyyy} | OKLA";
                var htmlBody = RenderDailyReportEmail(
                    userInfo.FullName ?? userInfo.Email,
                    yesterday.ToString("dd/MM/yyyy"),
                    ownerReport.TotalImpressions,
                    ownerReport.TotalClicks,
                    ownerReport.OverallCtr,
                    ownerReport.ActiveCampaigns,
                    ownerReport.TotalSpent);

                await notificationClient.SendEmailAsync(
                    userInfo.Email,
                    subject,
                    htmlBody,
                    ct);

                _logger.LogDebug("Daily report email sent to {Email} (owner {OwnerId})", userInfo.Email, ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send daily report to owner {OwnerId}", ownerId);
            }
        }
    }

    private static string RenderDailyReportEmail(
        string ownerName,
        string reportDate,
        long totalImpressions,
        long totalClicks,
        decimal ctr,
        int activeCampaigns,
        decimal totalSpent)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""utf-8"">
    <title>Reporte Diario - OKLA</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #fff; }}
        .header {{ background: #1a56db; color: white; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 20px; }}
        .content {{ padding: 20px; }}
        .stats {{ display: flex; flex-wrap: wrap; gap: 10px; margin: 15px 0; }}
        .stat {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; flex: 1; min-width: 100px; text-align: center; }}
        .stat-val {{ font-size: 24px; font-weight: 700; color: #1a56db; }}
        .stat-lbl {{ font-size: 11px; color: #64748b; text-transform: uppercase; }}
        .btn {{ display: inline-block; background: #1a56db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 15px; text-align: center; font-size: 11px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📈 Reporte Diario de Publicidad</h1>
            <p style=""margin:4px 0 0;font-size:13px;opacity:0.9;"">{reportDate}</p>
        </div>
        <div class=""content"">
            <p>Hola <strong>{System.Net.WebUtility.HtmlEncode(ownerName)}</strong>,</p>
            <p>Aquí tienes el resumen de ayer:</p>
            <div class=""stats"">
                <div class=""stat""><span class=""stat-val"">{totalImpressions:N0}</span><br><span class=""stat-lbl"">Impresiones</span></div>
                <div class=""stat""><span class=""stat-val"">{totalClicks:N0}</span><br><span class=""stat-lbl"">Clics</span></div>
                <div class=""stat""><span class=""stat-val"">{(ctr * 100):F2}%</span><br><span class=""stat-lbl"">CTR</span></div>
                <div class=""stat""><span class=""stat-val"">RD${totalSpent:N2}</span><br><span class=""stat-lbl"">Invertido</span></div>
            </div>
            <p>Campañas activas: <strong>{activeCampaigns}</strong></p>
            <p style=""text-align:center;""><a href=""https://okla.com.do/impulsar/mis-campanas"" class=""btn"">Ver mis campañas →</a></p>
        </div>
        <div class=""footer"">&copy; {DateTime.UtcNow.Year} OKLA. Todos los derechos reservados.</div>
    </div>
</body>
</html>";
    }
}
