using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Providers;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Templates;
using NotificationService.Infrastructure.BackgroundServices;
using NotificationService.Infrastructure.External;
using NotificationService.Shared;
using CarDealer.Shared.MultiTenancy;
using CarDealer.Shared.Resilience.Extensions;

namespace NotificationService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Core ──────────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddMemoryCache();

        // ── Configuration Bindings ────────────────────────────
        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.Configure<NotificationServiceRabbitMQSettings>(configuration.GetSection("NotificationService"));
        services.Configure<NotificationSettings>(configuration.GetSection("NotificationSettings"));

        // ── Repositories ──────────────────────────────────────
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, EfNotificationTemplateRepository>();
        services.AddScoped<INotificationQueueRepository, EfNotificationQueueRepository>();
        services.AddScoped<IScheduledNotificationRepository, EfScheduledNotificationRepository>();
        services.AddScoped<INotificationLogRepository, EfNotificationLogRepository>();
        services.AddScoped<IUserNotificationRepository, EfUserNotificationRepository>();
        services.AddScoped<IPriceAlertRepository, EfPriceAlertRepository>();
        services.AddScoped<ISavedSearchRepository, EfSavedSearchRepository>();

        // ── User Notification Service (in-app notifications) ──
        services.AddScoped<IUserNotificationService, UserNotificationService>();

        // ── Email Providers ───────────────────────────────────
        services.AddHttpClient("Resend").AddStandardResilience(configuration);
        services.AddScoped<IEmailProvider, ResendEmailService>();
        services.AddScoped<IEmailService, EmailService>();

        // ── SMS Provider ──────────────────────────────────────
        services.AddScoped<ISmsProvider, TwilioSmsService>();

        // ── Push Provider ─────────────────────────────────────
        services.AddScoped<IPushNotificationProvider, FirebasePushService>();

        // ── WhatsApp Providers (Strategy/Factory pattern) ─────
        services.AddScoped<TwilioWhatsAppService>();
        services.AddScoped<MockWhatsAppService>();
        services.AddScoped<IWhatsAppProviderFactory, WhatsAppProviderFactory>();
        services.AddScoped<IWhatsAppProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IWhatsAppProviderFactory>();
            // Use synchronous resolution to avoid deadlock from GetAwaiter().GetResult()
            return factory.GetProviderAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        });

        // ── Webhook Providers (Teams + Slack) ─────────────────
        services.AddHttpClient<ITeamsProvider, TeamsProvider>().AddStandardResilience(configuration);
        services.AddHttpClient<ISlackProvider, SlackProvider>().AddStandardResilience(configuration);

        // ── Admin Alert Service ───────────────────────────────
        services.AddScoped<IAdminAlertService, AdminAlertService>();

        // ── ConfigurationService Client ───────────────────────
        var configServiceUrl = configuration["Services:ConfigurationService"] ?? "http://localhost:15124";
        services.AddHttpClient<ConfigurationServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configServiceUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
            client.Timeout = TimeSpan.FromSeconds(5);
        }).AddStandardResilience(configuration);
        services.AddScoped<IConfigurationServiceClient>(sp =>
            sp.GetRequiredService<ConfigurationServiceClient>());

        // ── Meta WhatsApp HTTP Client ─────────────────────────
        services.AddHttpClient<MetaWhatsAppService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "OKLA-NotificationService");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilience(configuration);

        // ── ErrorService Client ───────────────────────────────
        services.AddHttpClient<ErrorServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:ErrorService"] ?? "http://errorservice:8080");
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
        }).AddStandardResilience(configuration);

        // ── Inter-Service Named HTTP Clients (UserService, ContactService, DealerAnalyticsService) ──
        services.AddHttpClient("UserService", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:UserService"] ?? "http://userservice:80");
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilience(configuration);

        services.AddHttpClient("ContactService", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:ContactService"] ?? "http://contactservice:80");
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilience(configuration);

        services.AddHttpClient("DealerAnalyticsService", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:DealerAnalyticsService"] ?? "http://dealeranalyticsservice:80");
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilience(configuration);

        services.AddHttpClient("VehiclesSaleService", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:VehiclesSaleService"] ?? "http://vehiclessaleservice:80");
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).AddStandardResilience(configuration);

        // ── AnalyticsAgent HTTP Client (LLM insight generation for weekly recommendations) ──
        services.AddHttpClient("AnalyticsAgentService", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:AnalyticsAgentService"] ?? "http://analyticsagent:8080");
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService");
            client.Timeout = TimeSpan.FromSeconds(30); // LLM calls can be slower
        }).AddStandardResilience(configuration);

        // ── Template Engine & Services ────────────────────────
        services.AddScoped<ITemplateEngine, TemplateEngine>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<NotificationQueueService>();

        // ── Background Services ───────────────────────────────
        services.AddHostedService<RabbitMQNotificationConsumer>();
        services.AddHostedService<NotificationQueueBackgroundService>();
        services.AddHostedService<ScheduledNotificationWorker>();
        services.AddHostedService<KYCStatusChangedNotificationConsumer>();  // KYC approval/rejection emails

        return services;
    }
}