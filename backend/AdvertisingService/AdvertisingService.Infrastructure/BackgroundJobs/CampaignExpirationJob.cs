using System.Text;
using System.Text.Json;
using AdvertisingService.Application.Interfaces;
using AdvertisingService.Domain.Enums;
using AdvertisingService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AdvertisingService.Infrastructure.BackgroundJobs;

public class CampaignExpirationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<CampaignExpirationJob> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private const string ExchangeName = "cardealer.events";

    public CampaignExpirationJob(
        IServiceScopeFactory scopeFactory,
        IConnection rabbitConnection,
        ILogger<CampaignExpirationJob> logger)
    {
        _scopeFactory     = scopeFactory;
        _rabbitConnection = rabbitConnection;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignExpirationJob started, checking every {Interval}", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExpireCampaignsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking campaign expirations");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndExpireCampaignsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var campaignRepo  = scope.ServiceProvider.GetRequiredService<IAdCampaignRepository>();
        var cacheService  = scope.ServiceProvider.GetRequiredService<IHomepageRotationCacheService>();

        var expiredCount = 0;

        foreach (var section in Enum.GetValues<AdPlacementType>())
        {
            var activeCampaigns = await campaignRepo.GetActiveByPlacementAsync(section, ct);
            foreach (var campaign in activeCampaigns.Where(c => c.IsExpired()))
            {
                campaign.MarkExpired();
                await campaignRepo.UpdateAsync(campaign, ct);
                expiredCount++;

                // Notify VehiclesSaleService to clear IsPremium / IsFeatured flags
                PublishCampaignCompletedEvent(campaign.Id, campaign.VehicleId);
            }

            if (expiredCount > 0)
            {
                await cacheService.InvalidateAsync(section);
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Expired {Count} campaigns", expiredCount);
        }
    }

    private void PublishCampaignCompletedEvent(Guid campaignId, Guid vehicleId)
    {
        try
        {
            using var channel = _rabbitConnection.CreateModel();
            var payload = JsonSerializer.Serialize(new
            {
                CampaignId  = campaignId,
                VehicleId   = vehicleId,
                CompletedAt = DateTime.UtcNow
            });

            var props = channel.CreateBasicProperties();
            props.Persistent  = true;
            props.ContentType = "application/json";

            channel.BasicPublish(
                exchange:        ExchangeName,
                routingKey:      "advertising.campaign.completed",
                basicProperties: props,
                body:            Encoding.UTF8.GetBytes(payload));

            _logger.LogDebug(
                "Published advertising.campaign.completed for campaign {CampaignId}", campaignId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish advertising.campaign.completed for campaign {CampaignId}", campaignId);
        }
    }
}

