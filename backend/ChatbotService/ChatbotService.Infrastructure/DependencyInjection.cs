using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.Interfaces;
using ChatbotService.Infrastructure.Persistence;
using ChatbotService.Infrastructure.Persistence.Repositories;
using ChatbotService.Infrastructure.Services;
using ChatbotService.Infrastructure.Services.Strategies;
using CarDealer.Shared.Resilience.Extensions;

namespace ChatbotService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ChatbotDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            }));

        // Settings
        services.Configure<LlmSettings>(configuration.GetSection("LlmService"));
        services.Configure<InventorySyncSettings>(configuration.GetSection("InventorySync"));
        services.Configure<ReportingSettings>(configuration.GetSection("Reporting"));
        services.Configure<EmbeddingSettings>(configuration.GetSection("Embedding"));

        // Repositories
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IChatLeadRepository, ChatLeadRepository>();
        services.AddScoped<IChatbotConfigurationRepository, ChatbotConfigurationRepository>();
        services.AddScoped<IInteractionUsageRepository, InteractionUsageRepository>();
        services.AddScoped<IMaintenanceTaskRepository, MaintenanceTaskRepository>();
        services.AddScoped<IQuickResponseRepository, QuickResponseRepository>();
        services.AddScoped<IChatbotVehicleRepository, ChatbotVehicleRepository>();
        services.AddScoped<IUnansweredQuestionRepository, UnansweredQuestionRepository>();
        
        // pgvector Repository for RAG
        services.AddScoped<IVehicleEmbeddingRepository>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<VehicleEmbeddingRepository>>();
            return new VehicleEmbeddingRepository(connectionString, logger);
        });

        // Core Services — Claude Sonnet 4.5 (Anthropic API)
        services.AddScoped<ILlmService, ClaudeLlmService>();
        services.AddScoped<IAutoLearningService, AutoLearningService>();
        services.AddScoped<IHealthMonitoringService, HealthMonitoringService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IInventorySyncService, InventorySyncService>();

        // R17-PC: Anthropic Prompt Cache stats (singleton — accumulates across the process lifetime)
        services.AddSingleton<IPromptCacheStats, InMemoryPromptCacheStats>();

        // ═══════════════════════════════════════════════════════════
        // DUAL-MODE: Strategy Pattern + RAG + Embeddings
        // ═══════════════════════════════════════════════════════════
        
        // Embedding service (sentence-transformers via LlmServer)
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        
        // Vector search service (pgvector-based RAG)
        services.AddScoped<IVectorSearchService, VectorSearchService>();

        // Chat mode strategies
        services.AddScoped<SingleVehicleStrategy>();
        services.AddScoped<DealerInventoryStrategy>();
        services.AddScoped<GeneralChatStrategy>();
        services.AddScoped<IChatModeStrategyFactory, ChatModeStrategyFactory>();

        // WhatsApp integration (Meta Cloud API)
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        services.AddHttpClient("WhatsApp", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddStandardResilience(configuration);

        // Observability — .NET 8 Metrics
        services.AddSingleton<ChatbotMetrics>();
        services.AddSingleton<IChatbotSafetyMetrics>(sp => sp.GetRequiredService<ChatbotMetrics>());

        // HTTP Client para Claude API (Anthropic)
        // Claude Sonnet responds in 1-5s; timeout reduced from 120s to 60s for Claude API
        services.AddHttpClient("LlmServer", client =>
        {
            client.BaseAddress = new Uri(configuration["LlmService:ServerUrl"] ?? "https://api.anthropic.com");
            client.Timeout = TimeSpan.FromSeconds(int.Parse(configuration["LlmService:TimeoutSeconds"] ?? "60"));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        })
        .AddStandardResilience(configuration);

        // HTTP Client para Embedding Server (separado del LLM Server)
        // Puede apuntar a: llm-server local, HuggingFace Inference API (gratis), OpenAI, etc.
        // Single query embedding should be <1s; reduced timeout from 30s to 10s
        services.AddHttpClient("EmbeddingServer", client =>
        {
            client.BaseAddress = new Uri(configuration["Embedding:ServerUrl"] ?? "http://llm-server:8000");
            client.Timeout = TimeSpan.FromSeconds(int.Parse(configuration["Embedding:TimeoutSeconds"] ?? "10"));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddStandardResilience(configuration);

        // HTTP Clients with Polly resilience
        services.AddHttpClient("VehiclesApi", client =>
        {
            client.BaseAddress = new Uri(configuration["InventorySync:VehiclesApiUrl"] ?? "http://vehiclessaleservice:8080");
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient("NotificationService", client =>
        {
            client.BaseAddress = new Uri(configuration["Reporting:NotificationServiceUrl"] ?? "http://notificationservice:8080");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(GetRetryPolicy());

        // R17 (MLOps): Redis response cache for LLM inference
        services.AddSingleton<ILlmResponseCacheService, LlmResponseCacheService>(sp =>
        {
            var cache = sp.GetRequiredService<IDistributedCache>();
            var logger = sp.GetRequiredService<ILogger<LlmResponseCacheService>>();
            var metrics = sp.GetRequiredService<ChatbotMetrics>();
            var cacheTtl = TimeSpan.FromMinutes(
                int.Parse(configuration["LlmService:CacheTtlMinutes"] ?? "30"));
            var enabled = bool.Parse(configuration["LlmService:CacheEnabled"] ?? "true");
            return new LlmResponseCacheService(cache, logger, metrics, cacheTtl, enabled);
        });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
