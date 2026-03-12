using CarDealer.Shared.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Persistence;
using VehiclesSaleService.Infrastructure.Repositories;
using VehiclesSaleService.Infrastructure.Messaging;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.MultiTenancy;
// ConfigurationServiceClient for dynamic config from admin panel
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Caching.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

const string ServiceName = "VehiclesSaleService";
const string ServiceVersion = "2.0.0";

try
{

    var builder = WebApplication.CreateBuilder(args);

    // ============================================================================
    // FASE 2: OBSERVABILITY - Logging centralizado con Serilog + Seq
    // ============================================================================
    builder.UseStandardSerilog("VehiclesSaleService", options =>
    {
        options.SeqEnabled = true;
        options.SeqServerUrl = builder.Configuration["Logging:Seq:ServerUrl"] ?? "http://seq:5341";
        options.FileEnabled = builder.Configuration.GetValue<bool>("Logging:File:Enabled", false);
        options.FilePath = builder.Configuration["Logging:File:Path"] ?? "logs/vehiclessaleservice-.log";
        options.RabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
        options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.RabbitMQUser = builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is not configured");
        options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
    });

    // ========================================
    // SECRET PROVIDER
    // ========================================

    builder.Services.AddSecretProvider();

    // ========================================
    // MULTI-TENANCY
    // ========================================

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITenantContext, TenantContext>();

    // ========================================
    // CONFIGURATION
    // ========================================

    // Configure routing to use lowercase URLs
    builder.Services.AddRouting(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = false;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            // Serialize/deserialize enums as strings (case-insensitive on input)
            // Allows frontend to send "automatic", "gasoline", "used", etc.
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            );
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "VehiclesSaleService API",
            Version = "v1",
            Description = "API for vehicle sales marketplace - buy and sell cars, trucks, motorcycles, boats, and more"
        });
    });

    // ========================================
    // DATABASE
    // ========================================

    // Priority: Environment variable > Docker config > appsettings.json
    var connectionString = builder.Configuration["Database:ConnectionStrings:PostgreSQL"]
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? MicroserviceSecretsConfiguration.GetDatabaseConnectionString(builder.Configuration, "VehiclesSaleService");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    });

    // ========================================
    // CONFIGURATION SERVICE CLIENT (dynamic config from admin panel)
    // ========================================

    builder.Services.AddConfigurationServiceClient(builder.Configuration, "VehiclesSaleService");

    // ========================================
    // DEPENDENCY INJECTION
    // ========================================

    builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IVehicleCatalogRepository, VehicleCatalogRepository>();
    builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();

    // RabbitMQ Event Publisher
    builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

    // ========================================
    // REDIS CACHING (standard shared library)
    // ========================================
    // TTLs: catalog=86400s (24h), featured=600s (10m), search=120s (2m), detail=300s (5m)
    builder.Services.AddStandardCaching(builder.Configuration, "VehiclesSaleService");

    // ========================================
    // RESPONSE COMPRESSION (Brotli + Gzip)
    // ========================================
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/json", "text/json", "application/problem+json" });
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest; // Fastest for API responses
    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    // RabbitMQ Campaign Events Consumer — syncs vehicle promotion flags (IsPremium, IsFeatured)
    // when AdvertisingService publishes campaign lifecycle events.
    builder.Services.AddHostedService<CampaignEventsConsumer>();

    // RabbitMQ Cache Invalidation Consumer — invalidates Redis/memory cache when
    // vehicle lifecycle events arrive (created, updated, deleted, sold, published).
    builder.Services.AddHostedService<CacheInvalidationConsumer>();

    // RabbitMQ Review Events Consumer — syncs SellerRating/SellerReviewCount from
    // ReviewService when reviews are created/updated. Critical for OKLA Platform Score D4.
    builder.Services.AddHostedService<ReviewEventsConsumer>();

    // ═══════════════════════════════════════════════════════════════
    // SITEMAP FIX: SitemapRevalidationService — triggers ISR revalidation
    // + Google Indexing API on vehicle publish/unpublish/delete.
    // Requires Frontend:Url, Frontend:RevalidationSecret, Frontend:SeoWebhookSecret
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddSingleton<VehiclesSaleService.Infrastructure.Services.SitemapRevalidationService>();

    // ========================================
    // JWT AUTHENTICATION (centralized config)
    // ========================================

    var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
            };
        });

    builder.Services.AddAuthorization();

    // ============= AUDIT =============
    builder.Services.AddAuditPublisher(builder.Configuration);

    // ========================================
    // CORS
    // ========================================

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "https://okla.com.do", "https://www.okla.com.do", "https://api.okla.com.do" };

            policy.WithOrigins(allowedOrigins)
                  // Security: Restrict to specific HTTP methods and headers (OWASP)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

    // ========================================
    // ========================================
    // RATE LIMITING
    // ========================================
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("VehiclesPolicy", limiterOptions =>
        {
            limiterOptions.PermitLimit = builder.Configuration.GetValue<int>("Security:RateLimit:RequestsPerMinute", 120);
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json";

            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                ? retryAfterValue
                : TimeSpan.FromSeconds(60);

            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.io/429",
                title = "Too Many Requests",
                status = 429,
                detail = $"Rate limit exceeded. Retry after {(int)retryAfter.TotalSeconds} seconds.",
                retryAfterSeconds = (int)retryAfter.TotalSeconds
            }, cancellationToken);
        };
    });

    // HEALTH CHECKS
    // ========================================

    builder.Services.AddHealthChecks();

    // ============================================================================
    // FASE 2: OBSERVABILITY - OpenTelemetry Tracing + Metrics
    // ============================================================================
    builder.Services.AddStandardObservability("VehiclesSaleService", options =>
    {
        options.TracingEnabled = true;
        options.MetricsEnabled = true;
        options.OtlpEndpoint = builder.Configuration["Observability:Otlp:Endpoint"] ?? "http://jaeger:4317";
        options.SamplingRatio = builder.Configuration.GetValue<double>("Observability:SamplingRatio", builder.Environment.IsProduction() ? 0.1 : 1.0);
        options.PrometheusEnabled = builder.Configuration.GetValue<bool>("Observability:Prometheus:Enabled", true);
        options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger" };
    });

    // ============================================================================
    // FASE 2: OBSERVABILITY - Error Handling centralizado
    // ============================================================================
    builder.Services.AddStandardErrorHandling(options =>
    {
        options.ServiceName = "VehiclesSaleService";
        options.Environment = builder.Environment.EnvironmentName;
        options.PublishToErrorService = builder.Configuration.GetValue<bool>("ErrorHandling:PublishToErrorService", true);
        options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.RabbitMQUser = builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is not configured");
        options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
        options.IncludeStackTrace = builder.Environment.IsDevelopment();
    });

    // ============================================================================
    // TRANSVERSAL SERVICES - Audit & Error clients
    // ============================================================================
    builder.Services.AddHttpClient<VehiclesSaleService.Application.Interfaces.IAuditServiceClient, VehiclesSaleService.Infrastructure.External.AuditServiceClient>(client =>
    {
        var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "http://auditservice:8080";
        client.BaseAddress = new Uri(auditServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient<VehiclesSaleService.Application.Interfaces.IErrorServiceClient, VehiclesSaleService.Infrastructure.External.ErrorServiceClient>(client =>
    {
        var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"] ?? "http://errorservice:8080";
        client.BaseAddress = new Uri(errorServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // AlertService client for price change notifications
    builder.Services.AddHttpClient("AlertService", client =>
    {
        var alertServiceUrl = builder.Configuration["ServiceUrls:AlertService"] ?? "http://alertservice:8080";
        client.BaseAddress = new Uri(alertServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // NHTSA VPIC API client for VIN decoding (replaces static HttpClient — DNS rotation + resilience)
    builder.Services.AddHttpClient("NHTSA", client =>
    {
        client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov");
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // KYCService client for dealer KYC verification checks (used by Publish endpoint).
    // Consistent with frontend useCanSell hook which also checks KYCService.
    builder.Services.AddHttpClient<VehiclesSaleService.Application.Interfaces.IDealerVerificationClient, VehiclesSaleService.Infrastructure.External.DealerVerificationClient>(client =>
    {
        var kycServiceUrl = builder.Configuration["ServiceUrls:KYCService"] ?? "http://kycservice:8080";
        client.BaseAddress = new Uri(kycServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // ============================================================================
    // TIER 3 EXTERNAL API INTEGRATIONS — Config-driven provider switching
    // Set "ExternalApis:{Service}:Provider" in appsettings/secrets to switch:
    //   "Mock" (default) | "VinAudit" | "CARFAX" | "Edmunds" | "MarketCheck"
    // All real providers fall back to Mock when API key is missing (FallbackToMock=true)
    // ============================================================================

    // Configuration binding for external API options
    builder.Services.Configure<VehiclesSaleService.Infrastructure.External.Configuration.ExternalApiOptions>(
        builder.Configuration.GetSection("ExternalApis"));

    // HttpClient registration for external API providers
    builder.Services.AddHttpClient("Edmunds", client =>
    {
        var baseUrl = builder.Configuration["ExternalApis:VehicleSpecs:BaseUrl"] ?? "https://api.edmunds.com";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient("MarketCheck", client =>
    {
        var baseUrl = builder.Configuration["ExternalApis:MarketPrice:BaseUrl"] ?? "https://api.marketcheck.com";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient("Carfax", client =>
    {
        var baseUrl = builder.Configuration["ExternalApis:Carfax:BaseUrl"] ?? "https://api.carfax.com";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient("VinAudit", client =>
    {
        var baseUrl = builder.Configuration["ExternalApis:VehicleHistory:BaseUrl"] ?? "https://api.vinaudit.com";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // BCRD — Banco Central República Dominicana Exchange Rate API (DOP/USD)
    builder.Services.AddHttpClient("BCRD", client =>
    {
        var baseUrl = builder.Configuration["ExternalApis:ExchangeRate:BaseUrl"] ?? "https://api.bancentral.gov.do";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // IExchangeRateService — live DOP/USD rate from BCRD with cache + fallback chain
    builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IExchangeRateService,
        VehiclesSaleService.Infrastructure.External.BcrdExchangeRateService>();

    // CARFAX/VinAudit — Vehicle History Reports
    var historyProvider = builder.Configuration["ExternalApis:VehicleHistory:Provider"] ?? "Mock";
    if (historyProvider.Equals("VinAudit", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IVehicleHistoryService,
            VehiclesSaleService.Infrastructure.External.VinAuditVehicleHistoryService>();
    }
    else if (historyProvider.Equals("CARFAX", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IVehicleHistoryService,
            VehiclesSaleService.Infrastructure.External.CarfaxVehicleHistoryService>();
    }
    else
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IVehicleHistoryService,
            VehiclesSaleService.Infrastructure.External.MockVehicleHistoryService>();
    }

    // Edmunds — Vehicle Technical Specifications
    var specsProvider = builder.Configuration["ExternalApis:VehicleSpecs:Provider"] ?? "Mock";
    if (specsProvider.Equals("Edmunds", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IVehicleSpecsService,
            VehiclesSaleService.Infrastructure.External.EdmundsVehicleSpecsService>();
    }
    else
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IVehicleSpecsService,
            VehiclesSaleService.Infrastructure.External.MockVehicleSpecsService>();
    }

    // MarketCheck — Market Price Comparison & Trends
    var priceProvider = builder.Configuration["ExternalApis:MarketPrice:Provider"] ?? "Mock";
    if (priceProvider.Equals("MarketCheck", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IMarketPriceService,
            VehiclesSaleService.Infrastructure.External.MarketCheckPriceService>();
    }
    else
    {
        builder.Services.AddSingleton<VehiclesSaleService.Application.Interfaces.IMarketPriceService,
            VehiclesSaleService.Infrastructure.External.MockMarketPriceService>();
    }

    // NHTSA — Free Vehicle Data API (recalls, VIN decode, complaints)
    var nhtsaEnabled = bool.TryParse(
        builder.Configuration["ExternalApis:Nhtsa:Enabled"], out var ne) ? ne : true;
    if (nhtsaEnabled)
    {
        builder.Services.AddSingleton<VehiclesSaleService.Infrastructure.External.INhtsaVehicleDataService,
            VehiclesSaleService.Infrastructure.External.NhtsaVehicleDataService>();
    }

    Log.Information("External API Providers configured: History={HistoryProvider}, Specs={SpecsProvider}, " +
        "Price={PriceProvider}, NHTSA={NhtsaEnabled}",
        historyProvider, specsProvider, priceProvider, nhtsaEnabled);

    var app = builder.Build();

    // ============================================================================
    // MIDDLEWARE PIPELINE
    // ============================================================================

    // FASE 2: Global Error Handling - PRIMERO para capturar todas las excepciones
    app.UseGlobalErrorHandling();

    // Response Compression — early in pipeline to compress all responses
    app.UseResponseCompression();

    // FASE 2: Request Logging con enrichment de TraceId, UserId, CorrelationId
    app.UseRequestLogging();

    // Swagger en desarrollo
    // OWASP Security Headers
    app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

    if (app.Environment.IsDevelopment())
    {

        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Audit Middleware
    app.UseAuditMiddleware();

    app.MapControllers();

    // ============= HEALTH CHECKS (Triple Pattern) =============
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => !check.Tags.Contains("external")
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    // ========================================
    // DATABASE MIGRATION
    // ========================================

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migration completed for {ServiceName}", ServiceName);

            // Seed catalog data (makes, models) if empty
            await VehiclesSaleService.Infrastructure.Persistence.CatalogDataSeeder.SeedAsync(dbContext, startupLogger);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration error for {ServiceName}", ServiceName);
        }
    }

    // ========================================
    // START
    // ========================================

    Log.Information("Starting {ServiceName} v{ServiceVersion}", ServiceName, ServiceVersion);

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application {ServiceName} terminated unexpectedly", "VehiclesSaleService");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so it can be accessed by tests
public partial class Program { }
