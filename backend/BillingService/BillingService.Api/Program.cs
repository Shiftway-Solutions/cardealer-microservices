using CarDealer.Shared.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using BillingService.Application.Services;
using BillingService.Application.Configuration;
using BillingService.Application.Interfaces;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Persistence;
using BillingService.Infrastructure.Repositories;
using BillingService.Infrastructure.Services;
using BillingService.Infrastructure.External;
using BillingService.Infrastructure.Messaging;
using BillingService.Infrastructure.Azul;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Idempotency.Extensions;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using Polly;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.Text;

const string ServiceName = "BillingService";
const string ServiceVersion = "1.1.0";

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Add Secret Provider for Docker secrets
    builder.Services.AddSecretProvider();

    // ============================================================================
    // FASE 2: OBSERVABILITY - Logging centralizado con Serilog + Seq
    // ============================================================================
    builder.UseStandardSerilog("BillingService", options =>
    {
        options.SeqEnabled = true;
        options.SeqServerUrl = builder.Configuration["Logging:Seq:ServerUrl"] ?? "http://seq:5341";
        options.FileEnabled = builder.Configuration.GetValue<bool>("Logging:File:Enabled", false);
        options.FilePath = builder.Configuration["Logging:File:Path"] ?? "logs/billingservice-.log";
        options.RabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
        options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.RabbitMQUser = builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is not configured");
        options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
    });

    // Add services to the container.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();

    // Register FluentValidation validators
    builder.Services.AddValidatorsFromAssembly(
        typeof(BillingService.Application.Validators.SecurityValidators).Assembly);

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "BillingService API", Version = "v1" });
    });

    // ============================================================================
    // FASE 2: OBSERVABILITY - OpenTelemetry Tracing + Metrics
    // ============================================================================
    builder.Services.AddStandardObservability("BillingService", options =>
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
        options.ServiceName = "BillingService";
        options.Environment = builder.Environment.EnvironmentName;
        options.PublishToErrorService = builder.Configuration.GetValue<bool>("ErrorHandling:PublishToErrorService", true);
        options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.RabbitMQUser = builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is not configured");
        options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
        options.IncludeStackTrace = builder.Environment.IsDevelopment();
    });

    // ============================================================================
    // FASE 5: Audit Publisher - Eventos de auditoría para pagos
    // ============================================================================
    builder.Services.AddAuditPublisher(builder.Configuration);

    // CORS — configurable origins from appsettings
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? (builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "http://localhost:5173" }
            : new[] { "https://okla.com.do", "https://www.okla.com.do" });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

    // ========== JWT AUTHENTICATION (from centralized secrets, NOT hardcoded) ==========
    var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                ClockSkew = TimeSpan.Zero
            };
        });

    // Add HttpContextAccessor
    builder.Services.AddHttpContextAccessor();

    // Configure Stripe
    builder.Services.Configure<StripeSettings>(
        builder.Configuration.GetSection("Stripe"));

    // Configure AZUL
    builder.Services.Configure<AzulConfiguration>(
        builder.Configuration.GetSection("Azul"));

    // Add AZUL Services
    builder.Services.AddScoped<IAzulHashGenerator, AzulHashGenerator>();
    builder.Services.AddScoped<IAzulPaymentService, AzulPaymentService>();

    // Add DbContext with secrets support
    var connectionString = MicroserviceSecretsConfiguration.GetDatabaseConnectionString(builder.Configuration, "BillingService");
    builder.Services.AddDbContext<BillingDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Add Repositories
    builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
    builder.Services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
    builder.Services.AddScoped<IEarlyBirdRepository, EarlyBirdRepository>();
    builder.Services.AddScoped<IAzulTransactionRepository, AzulTransactionRepository>();
    builder.Services.AddScoped<IReportPurchaseRepository, ReportPurchaseRepository>();

    // CONTRA #6 FIX: Payment Reconciliation — daily Stripe↔OKLA audit
    builder.Services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
    builder.Services.AddScoped<IPaymentReconciliationService, PaymentReconciliationService>();

    // Add Stripe Services
    builder.Services.AddScoped<IStripeService, StripeService>();
    builder.Services.AddScoped<BillingApplicationService>();

    // ═══════════════════════════════════════════════════════════════
    // STRIPE CIRCUIT BREAKER — protects against Stripe API degradation
    // Opens after 5 consecutive failures in 60s, breaks for 30s
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddResiliencePipeline("stripe-circuit-breaker", pipelineBuilder =>
    {
        pipelineBuilder
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<Stripe.StripeException>(ex =>
                        ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        ex.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        ex.HttpStatusCode == System.Net.HttpStatusCode.BadGateway ||
                        ex.HttpStatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
            })
            .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(60),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder()
                    .Handle<Stripe.StripeException>(ex =>
                        ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        ex.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        ex.HttpStatusCode == System.Net.HttpStatusCode.BadGateway ||
                        ex.HttpStatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
            });
    });

    // Add RabbitMQ Event Publisher
    builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

    // Add UserService Client for syncing subscriptions
    var userServiceUrl = builder.Configuration["Services:UserService"] ?? "http://localhost:5020";
    builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
    {
        client.BaseAddress = new Uri(userServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddStandardResilience(builder.Configuration);

    // ========================================
    // RATE LIMITING
    // ========================================
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("BillingPolicy", limiterOptions =>
        {
            limiterOptions.PermitLimit = builder.Configuration.GetValue<int>("Security:RateLimit:RequestsPerMinute", 30);
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

    // Add Health Checks
    builder.Services.AddHealthChecks();

    // Add Idempotency for payment protection
    var idempotencyEnabled = builder.Configuration.GetValue<bool>("Idempotency:Enabled", true);
    if (idempotencyEnabled)
    {
        builder.Services.AddIdempotency(options =>
        {
            options.Enabled = true;
            options.RedisConnection = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
            options.HeaderName = "X-Idempotency-Key";
            options.DefaultTtlSeconds = 86400; // 24 hours
            options.RequireIdempotencyKey = true;
            options.KeyPrefix = "billing:idempotency";
            options.ValidateRequestHash = true;
            options.ProcessingTimeoutSeconds = 120; // 2 minutes for payment processing
        });
        Log.Information("Idempotency enabled for BillingService - Payment protection active");
    }

    // ============================================================================
    // TRANSVERSAL SERVICES - Audit & Error clients
    // ============================================================================
    builder.Services.AddHttpClient<BillingService.Application.Interfaces.IAuditServiceClient, BillingService.Infrastructure.External.AuditServiceClient>(client =>
    {
        var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "http://auditservice:8080";
        client.BaseAddress = new Uri(auditServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient<BillingService.Application.Interfaces.IErrorServiceClient, BillingService.Infrastructure.External.ErrorServiceClient>(client =>
    {
        var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"] ?? "http://errorservice:8080";
        client.BaseAddress = new Uri(errorServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
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

    // RETENTION FIX: Background worker for proactive subscription lifecycle management
    // Handles: trial expiring warnings, payment failure escalation, auto-suspend, auto-expire
    builder.Services.AddHostedService<BillingService.Api.BackgroundServices.SubscriptionRenewalWorker>();

    // CONTRA #6 FIX: Daily payment reconciliation job (runs at 03:00 UTC)
    builder.Services.AddHostedService<DailyReconciliationJob>();

    // Link guest report purchases to newly registered users (listens for auth.user.registered)
    builder.Services.AddHostedService<ReportPurchaseLinkingConsumer>();

    var app = builder.Build();

    // ============================================================================
    // MIDDLEWARE PIPELINE
    // ============================================================================

    // FASE 2: Global Error Handling - PRIMERO para capturar todas las excepciones
    app.UseGlobalErrorHandling();

    // FASE 2: Request Logging con enrichment de TraceId, UserId, CorrelationId
    app.UseRequestLogging();

    // Swagger en desarrollo
    // OWASP Security Headers
    app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

    // Response Compression — early, after error handling
    app.UseResponseCompression();

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

    if (!app.Environment.IsProduction())
        app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    // Idempotency middleware — AFTER auth (scoped to authenticated user to prevent key pollution)
    if (idempotencyEnabled)
    {
        app.UseIdempotency();
    }

    // FASE 5: Audit Middleware
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

    // Apply migrations on startup in development OR when explicitly enabled via Database__AutoMigrate=true
    // BUG-D005 fix: was only running in Development, missing production migrations
    var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate", false)
        || app.Environment.IsDevelopment();
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        try
        {
            db.Database.Migrate();
            Log.Information("Database migration completed for {ServiceName}", ServiceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration error for {ServiceName}", ServiceName);
        }
    }

    Log.Information("Starting {ServiceName} v{ServiceVersion} — Payment & Billing Platform", ServiceName, ServiceVersion);
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application {ServiceName} terminated unexpectedly", "BillingService");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so it can be accessed by tests
public partial class Program { }
