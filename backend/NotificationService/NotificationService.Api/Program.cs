using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using NotificationService.Infrastructure.Extensions;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Providers;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Domain.Interfaces;
using Serilog;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.Messaging;
using CarDealer.Shared.Configuration;
using System.Reflection;
using FluentValidation;
using NotificationService.Shared;
using CarDealer.Shared.Database;
using CarDealer.Shared.Secrets;

using NotificationService.Infrastructure.BackgroundServices;
using NotificationService.Infrastructure.Metrics;
using Polly;
using Polly.CircuitBreaker;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using NotificationService.Api.Middleware;
using NotificationService.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "NotificationService";
const string ServiceVersion = "1.0.0";

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============= CENTRALIZED LOGGING (Serilog → Seq) =============
    builder.UseStandardSerilog(ServiceName);

    // ============= SECRETS PROVIDER =============
    builder.Services.AddSecretProvider();

    // Add services to the container.
    builder.Services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks();

    // Note: AddLogging() removed — UseStandardSerilog() already configures logging via Serilog

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "http://localhost:5173" };

            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  // Security: Restrict to specific headers (OWASP)
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

    // ✅ USAR DEPENDENCY INJECTION DE INFRASTRUCTURE (INCLUYE RABBITMQ)
    builder.Services.AddInfrastructure(builder.Configuration);

    // ============= TRANSVERSAL SERVICES =============
    // Error Handling (→ ErrorService)
    builder.Services.AddStandardErrorHandling(builder.Configuration, ServiceName);

    // Audit (→ AuditService via RabbitMQ)
    builder.Services.AddAuditPublisher(builder.Configuration);

    // ========== SERVICE DISCOVERY ==========

    // Service Discovery Configuration - with fallback to NoOp when Consul is disabled
    var consulEnabled = builder.Configuration.GetValue<bool>("Consul:Enabled", false);
    if (consulEnabled)
    {
        builder.Services.AddSingleton<IConsulClient>(sp =>
        {
            var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
            return new ConsulClient(config => config.Address = new Uri(consulAddress));
        });

        builder.Services.AddScoped<IServiceRegistry, ConsulServiceRegistry>();
        builder.Services.AddScoped<IServiceDiscovery, ConsulServiceDiscovery>();
        builder.Services.AddHttpClient("HealthCheck");
        builder.Services.AddScoped<IHealthChecker, HttpHealthChecker>();
    }
    else
    {
        // Use NoOp implementations when Consul is disabled
        builder.Services.AddScoped<IServiceRegistry, NoOpServiceRegistry>();
        builder.Services.AddScoped<IServiceDiscovery, NoOpServiceDiscovery>();
    }

    // ========================================

    // 🔧 Register RabbitMQ Consumers as Hosted Services
    builder.Services.AddHostedService<ErrorCriticalEventConsumer>();
    builder.Services.AddHostedService<UserRegisteredNotificationConsumer>();
    builder.Services.AddHostedService<VehicleCreatedNotificationConsumer>();
    builder.Services.AddHostedService<PaymentReceiptNotificationConsumer>();
    builder.Services.AddHostedService<InvoiceNotificationConsumer>(); // Factura electrónica e-CF por email
                                                                      // KYCStatusChangedNotificationConsumer — already registered in AddInfrastructure() (ServiceCollectionExtensions.cs)
    builder.Services.AddHostedService<PriceAlertTriggeredConsumer>();
    builder.Services.AddHostedService<SavedSearchActivatedConsumer>();
    builder.Services.AddHostedService<VehiclePublishedAlertMatcherConsumer>(); // 🔔 NEW: Matches published vehicles against saved searches & price alerts
    builder.Services.AddHostedService<UserLoggedInEventConsumer>(); // Security in-app notifications on login
    builder.Services.AddHostedService<UserSettingsChangedEventConsumer>(); // In-app confirmation when user saves settings

    // �️ BROKEN IMAGES ALERT: WhatsApp + Email + In-app when >50% of listing photos are broken
    builder.Services.AddHostedService<BrokenImageAlertConsumer>();

    // �🔔 LEAD NOTIFICATIONS: Email + WhatsApp to dealer when buyer contacts about a vehicle
    builder.Services.AddHostedService<LeadCreatedNotificationConsumer>();

    // 📊 ONBOARDING: 7-day dealer performance report (email + WhatsApp + in-app)
    builder.Services.AddHostedService<DealerOnboardingReportConsumer>();

    // 📈 UPSELL: Daily check for LIBRE-plan dealers with 5+ inquiries → personalized ROI email
    builder.Services.AddHostedService<LeadThresholdUpsellWorker>();

    // ⭐ FEATURED UPSELL: Daily check for vehicles published 45+ days → suggest Featured Listing ($6/mo)
    builder.Services.AddHostedService<ListingInactivityUpsellWorker>();

    // � MONTHLY BENCHMARK: 1st of each month → sends dealer metrics vs. VISIBLE-plan average email
    builder.Services.AddHostedService<MonthlyBenchmarkReportWorker>();
    // 📬 WEEKLY RECOMMENDATION: Every Monday 8AM DR → sends top actionable recommendation via WhatsApp (Pro/Elite dealers)
    builder.Services.AddHostedService<WeeklyRecommendationWorker>();
    // �🔄 RETENTION: Subscription lifecycle notification consumers
    builder.Services.AddHostedService<TrialEndingNotificationConsumer>();               // Trial expiry warnings (3-day + 1-day)
    builder.Services.AddHostedService<PaymentFailedNotificationConsumer>();              // Payment failure alerts with update CTA
    builder.Services.AddHostedService<SubscriptionCancelledNotificationConsumer>();      // Cancellation confirmation + feedback request
    builder.Services.AddHostedService<RevenueThresholdAlertConsumer>();                  // Revenue < OPEX threshold alert → founder (CONTRA #7)
    builder.Services.AddHostedService<InfrastructureCostAlertConsumer>();                // Cloud cost > $210 budget alert → CTO (CONTRA #8)
    builder.Services.AddHostedService<UserDataDeletionConsumer>();                       // Ley 172-13 cascade user data deletion
    builder.Services.AddHostedService<ReportPurchaseReceiptConsumer>();                  // OKLA Score report purchase receipt email

    // Dead Letter Queue — PostgreSQL-backed (survives pod restarts during auto-scaling)
    builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "NotificationService");
    builder.Services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();
    builder.Services.AddHostedService<DeadLetterQueueProcessor>();

    // Shared RabbitMQ connection (1 connection per pod instead of N per class)
    builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

    // Metrics
    builder.Services.AddSingleton<NotificationServiceMetrics>();

    // Polly 8.x Circuit Breaker
    builder.Services.AddResiliencePipeline("notification-circuit-breaker", pipelineBuilder =>
    {
        pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        });
    });

    // ============= OBSERVABILITY (OpenTelemetry → shared library) =============
    builder.Services.AddStandardObservability(builder.Configuration, ServiceName, ServiceVersion);

    // Database Context (multi-provider configuration)
    builder.Services.AddDatabaseProvider<ApplicationDbContext>(builder.Configuration);

    // MediatR - Cargar assemblies de Application
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(Assembly.Load("NotificationService.Application")));

    // FluentValidation - Validators
    builder.Services.AddValidatorsFromAssembly(Assembly.Load("NotificationService.Application"));

    // ValidationBehavior — ensures FluentValidation validators (NoSqlInjection, NoXss) run automatically in MediatR pipeline
    builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(NotificationService.Application.Behaviors.ValidationBehavior<,>));

    // NotificationSettings already registered in AddInfrastructure — no duplicate Configure<> needed

    // ========== JWT AUTHENTICATION (from centralized secrets, NOT hardcoded) ==========
    try
    {
        var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero // No tolerance — tokens expire exactly at exp claim
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Debug("JWT Token validated for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                }
            };
        });

        Log.Information("JWT Authentication configured successfully for {ServiceName}", ServiceName);
    }
    catch (InvalidOperationException ex)
    {
        Log.Fatal("JWT Authentication FAILED to configure for {ServiceName}: {Message}. Service will NOT start without proper JWT configuration.", ServiceName, ex.Message);
        throw; // Fail fast — do NOT start without auth (NIST IA-5)
    }

    // Authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("NotificationServiceRead", policy =>
        {
            policy.RequireAuthenticatedUser();
        });

        options.AddPolicy("NotificationServiceAdmin", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("role", "admin", "notification-admin");
        });
    });

    // ============= RATE LIMITING =============
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 60;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
        });
        options.OnRejected = async (context, ct) =>
        {
            Log.Warning("Rate limit exceeded for {RemoteIp} on {Path}",
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.Request.Path);
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", ct);
        };
    });

    // Audit Service Client for centralized audit logging
    builder.Services.AddHttpClient<NotificationService.Application.Interfaces.IAuditServiceClient, NotificationService.Infrastructure.External.AuditServiceClient>(client =>
    {
        var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "http://auditservice:8080";
        client.BaseAddress = new Uri(auditServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // User Consent Client — checks CommunicationPreferences before sending marketing (Ley 172-13)
    builder.Services.AddHttpClient<NotificationService.Application.Interfaces.IUserConsentClient, NotificationService.Infrastructure.External.UserConsentClient>(client =>
    {
        var userServiceUrl = builder.Configuration["ServiceUrls:UserService"] ?? "http://userservice:8080";
        client.BaseAddress = new Uri(userServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // ============= RESPONSE COMPRESSION (Brotli + Gzip) =============
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
        "application/json",
        "text/json",
        "application/problem+json"
        });
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

    var app = builder.Build();

    // Apply database migrations conditionally (disabled in production to avoid race conditions with HPA replicas)
    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying database migrations for NotificationService...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
    else
    {
        Log.Information("Database auto-migration disabled for {ServiceName}. Run migrations via CI/CD pipeline.", ServiceName);
    }

    // ============= MIDDLEWARE PIPELINE (Canonical Order — Microsoft/OWASP) =============
    // 1. Global Error Handling — ALWAYS FIRST
    app.UseGlobalErrorHandling();

    // 2. Security Headers (OWASP) — early in pipeline
    app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

    // 3. Response Compression — early, after error handling
    app.UseResponseCompression();

    // 4. Request Logging
    app.UseRequestLogging();

    // 5. HTTPS Redirection — only outside K8s (TLS terminates at Ingress in production)
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // 6. Swagger — development only
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 6.5. Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // 7. CORS — before auth
    app.UseCors();

    // 8. Rate Limiting
    app.UseRateLimiter();

    // 9. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 9.1. Internal API Key validation — defense-in-depth for /api/internal/* endpoints
    app.UseInternalApiKeyValidation();

    // 10. Audit middleware — after auth (has userId context)
    app.UseAuditMiddleware();

    // 11. Service Discovery Auto-Registration
    app.UseMiddleware<ServiceRegistrationMiddleware>();

    // 12. Endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = check => !check.Tags.Contains("external")
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapControllers();

    Log.Information("NotificationService starting up with ErrorService middleware and RabbitMQ Consumer...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💀 {ServiceName} terminated unexpectedly", ServiceName);
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program class for integration testing
public partial class Program { }