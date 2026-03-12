using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Enums;
using DealerAnalyticsService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Application.Services;

/// <summary>
/// Background service that analyzes dealer metrics and creates alerts.
/// Runs every hour to check for conditions that require dealer attention.
/// Implements automated alerting from the process matrix.
/// </summary>
public class AlertAnalysisService : BackgroundService
{
    private readonly ILogger<AlertAnalysisService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    // Thresholds for alerts
    private const int LowInventoryThreshold = 5;
    private const int AgingDaysThreshold = 60;
    private const double ViewsDropThreshold = 0.30; // 30% drop
    private const double ConversionDropThreshold = 0.20; // 20% drop
    private const int ResponseTimeThresholdMinutes = 120; // 2 hours

    public AlertAnalysisService(
        ILogger<AlertAnalysisService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertAnalysisService started. Running every {Interval}", _interval);

        // Initial delay of 5 minutes to allow system to stabilize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnalyzeAndCreateAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during alert analysis");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task AnalyzeAndCreateAlertsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting alert analysis at {Time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var snapshotRepository = scope.ServiceProvider.GetRequiredService<IDealerSnapshotRepository>();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IDealerAlertRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var lastWeek = today.AddDays(-7);

        // Get recent snapshots to find active dealers
        var recentSnapshots = await snapshotRepository.GetRangeAsync(
            Guid.Empty,
            lastWeek,
            today,
            cancellationToken);

        var dealerIds = recentSnapshots
            .Select(s => s.DealerId)
            .Distinct()
            .ToList();

        _logger.LogInformation("Analyzing metrics for {Count} dealers", dealerIds.Count);

        var alertsCreated = 0;

        foreach (var dealerId in dealerIds)
        {
            try
            {
                // Get latest and comparison snapshots
                var currentSnapshot = await snapshotRepository.GetLatestAsync(dealerId, cancellationToken);
                var previousSnapshot = await snapshotRepository.GetByDateAsync(dealerId, yesterday, cancellationToken);

                if (currentSnapshot == null)
                    continue;

                // Check for low inventory
                if (currentSnapshot.ActiveVehicles < LowInventoryThreshold)
                {
                    var alert = DealerAlert.CreateLowInventoryAlert(
                        dealerId,
                        currentSnapshot.ActiveVehicles,
                        LowInventoryThreshold);

                    if (await ShouldCreateAlertAsync(alertRepository, dealerId, alert.Type, cancellationToken))
                    {
                        await alertRepository.CreateAsync(alert, cancellationToken);
                        alertsCreated++;
                        await PublishAlertAsync(mediator, alert, cancellationToken);
                    }
                }

                // Check for views dropping
                if (previousSnapshot != null && previousSnapshot.TotalViews > 0)
                {
                    var viewsChange = (currentSnapshot.TotalViews - previousSnapshot.TotalViews) / (double)previousSnapshot.TotalViews;

                    if (viewsChange < -ViewsDropThreshold)
                    {
                        var alert = DealerAlert.CreateViewsDroppingAlert(
                            dealerId,
                            Math.Abs(viewsChange),
                            currentSnapshot.TotalViews,
                            previousSnapshot.TotalViews);

                        if (await ShouldCreateAlertAsync(alertRepository, dealerId, alert.Type, cancellationToken))
                        {
                            await alertRepository.CreateAsync(alert, cancellationToken);
                            alertsCreated++;
                            await PublishAlertAsync(mediator, alert, cancellationToken);
                        }
                    }
                }

                // Check for slow response time
                if (currentSnapshot.AvgResponseTimeMinutes > ResponseTimeThresholdMinutes)
                {
                    var alert = DealerAlert.CreateLeadResponseSlowAlert(
                        dealerId,
                        (int)currentSnapshot.AvgResponseTimeMinutes,
                        ResponseTimeThresholdMinutes);

                    if (await ShouldCreateAlertAsync(alertRepository, dealerId, alert.Type, cancellationToken))
                    {
                        await alertRepository.CreateAsync(alert, cancellationToken);
                        alertsCreated++;
                        await PublishAlertAsync(mediator, alert, cancellationToken);
                    }
                }

                // Check for aging inventory
                if (currentSnapshot.VehiclesOver60Days > 0)
                {
                    var alert = DealerAlert.CreateAgingInventoryAlert(
                        dealerId,
                        currentSnapshot.VehiclesOver60Days,
                        currentSnapshot.TotalInventoryValue * 0.3m); // Estimate 30% of inventory is aging

                    if (await ShouldCreateAlertAsync(alertRepository, dealerId, alert.Type, cancellationToken))
                    {
                        await alertRepository.CreateAsync(alert, cancellationToken);
                        alertsCreated++;
                        await PublishAlertAsync(mediator, alert, cancellationToken);
                    }
                }

                // Check for conversion rate dropping
                if (previousSnapshot != null && previousSnapshot.LeadConversionRate > 0)
                {
                    var conversionChange = (currentSnapshot.LeadConversionRate - previousSnapshot.LeadConversionRate) / previousSnapshot.LeadConversionRate;

                    if (conversionChange < -ConversionDropThreshold)
                    {
                        var alert = DealerAlert.CreateConversionDropAlert(
                            dealerId,
                            Math.Abs(conversionChange),
                            (decimal)currentSnapshot.LeadConversionRate,
                            (decimal)previousSnapshot.LeadConversionRate);

                        if (await ShouldCreateAlertAsync(alertRepository, dealerId, alert.Type, cancellationToken))
                        {
                            await alertRepository.CreateAsync(alert, cancellationToken);
                            alertsCreated++;
                            await PublishAlertAsync(mediator, alert, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing dealer {DealerId}", dealerId);
            }
        }

        _logger.LogInformation("Alert analysis completed. Created {Count} alerts", alertsCreated);
    }

    private async Task<bool> ShouldCreateAlertAsync(
        IDealerAlertRepository alertRepository,
        Guid dealerId,
        DealerAlertType alertType,
        CancellationToken cancellationToken)
    {
        // Check if there's already an active alert of this type within the last 24 hours
        var recentAlerts = await alertRepository.GetByDealerIdAsync(
            dealerId,
            1,
            10,
            cancellationToken);

        var hasRecentAlert = recentAlerts.Items.Any(a =>
            a.Type == alertType &&
            a.Status == AlertStatus.Active &&
            a.CreatedAt > DateTime.UtcNow.AddHours(-24));

        return !hasRecentAlert;
    }

    private async Task PublishAlertAsync(
        IMediator mediator,
        DealerAlert alert,
        CancellationToken cancellationToken)
    {
        await mediator.Publish(
            new DealerAlertTriggeredNotification(
                alert.DealerId,
                alert.Id,
                alert.Type,
                alert.Severity),
            cancellationToken);
    }
}

/// <summary>
/// Notification published when an alert is triggered.
/// </summary>
public record DealerAlertTriggeredNotification(
    Guid DealerId,
    Guid AlertId,
    DealerAlertType AlertType,
    AlertSeverity Severity) : INotification;
