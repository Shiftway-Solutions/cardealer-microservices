using AdvertisingService.Application.Interfaces;
using AdvertisingService.Domain.Interfaces;
using AdvertisingService.Infrastructure.BackgroundJobs;
using AdvertisingService.Infrastructure.Messaging;
using AdvertisingService.Infrastructure.Messaging.Consumers;
using AdvertisingService.Infrastructure.Persistence;
using AdvertisingService.Infrastructure.Persistence.Repositories;
using AdvertisingService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AdvertisingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IAdCampaignRepository, AdCampaignRepository>();
        services.AddScoped<IAdImpressionRepository, AdImpressionRepository>();
        services.AddScoped<IAdClickRepository, AdClickRepository>();
        services.AddScoped<IRotationConfigRepository, RotationConfigRepository>();
        services.AddScoped<ICategoryConfigRepository, CategoryConfigRepository>();
        services.AddScoped<IBrandConfigRepository, BrandConfigRepository>();

        // Services
        services.AddScoped<IAdRotationEngine, AdRotationEngine>();
        services.AddScoped<IHomepageRotationCacheService, HomepageRotationCacheService>();
        services.AddScoped<QualityScoreCalculator>();
        services.AddScoped<IAdReportingService, AdReportingService>();
        services.AddSingleton<AdvertisingMetrics>();

        // Dead Letter Queue (local implementation — MUST be registered before HostedServices)
        services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();

        // Background Jobs
        services.AddHostedService<RotationRefreshJob>();
        services.AddHostedService<DailyAdReportJob>();
        services.AddHostedService<MonthlyAdReportJob>();
        services.AddHostedService<CampaignExpirationJob>();
        services.AddHostedService<BillingPaymentCompletedConsumer>();

        return services;
    }
}
