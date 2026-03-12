using MediaService.Application;
using Microsoft.AspNetCore.HttpOverrides;
using MediaService.Infrastructure;
using MediaService.Infrastructure.Extensions;
using MediaService.Infrastructure.Middleware;
using MediaService.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MediaService.Infrastructure.HealthChecks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.Logging.Extensions;
using MediaService.Domain.Interfaces;
using MediaService.Infrastructure.BackgroundServices;
using MediaService.Infrastructure.Metrics;
using Polly;
using Polly.CircuitBreaker;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using MediaService.Api.Middleware;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using CarDealer.Shared.Messaging;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "MediaService";
const string ServiceVersion = "1.0.0";

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============= CENTRALIZED LOGGING (Serilog → Seq) =============
    builder.UseStandardSerilog(ServiceName);

    // ============= SECRETS PROVIDER =============
    builder.Services.AddSecretProvider();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks();

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "https://okla.com.do" };

            policy.WithOrigins(allowedOrigins)
                  // Security: Restrict to specific HTTP methods and headers (OWASP)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

    // Add application and infrastructure services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

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
        // Strict limit for uploads
        options.AddFixedWindowLimiter("uploads", opt =>
        {
            opt.PermitLimit = 20;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
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

    // Register ConfigurationServiceClient for dynamic admin-panel config
    builder.Services.AddConfigurationServiceClient(builder.Configuration, "MediaService");

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
        });

        builder.Services.AddAuthorization();
        Log.Information("JWT Authentication configured successfully for {ServiceName}", ServiceName);
    }
    catch (InvalidOperationException ex)
    {
        Log.Fatal("JWT Authentication FAILED to configure for {ServiceName}: {Message}. Service will NOT start without proper JWT configuration.", ServiceName, ex.Message);
        throw; // Fail fast — do NOT start without auth (NIST IA-5)
    }

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

    // Dead Letter Queue and RabbitMQ - conditional registration
    var rabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled");

    // Shared RabbitMQ connection (1 connection per pod instead of N per class)
    builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

    if (rabbitMQEnabled)
    {
        // PostgreSQL-backed Dead Letter Queue (survives pod restarts during auto-scaling)
        builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "MediaService");
        builder.Services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();
        builder.Services.AddHostedService<DeadLetterQueueProcessor>();
    }

    // Metrics
    builder.Services.AddSingleton<MediaServiceMetrics>();

    // Polly 8.x Circuit Breaker
    builder.Services.AddResiliencePipeline("media-circuit-breaker", pipelineBuilder =>
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


    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    // Add RabbitMQ configuration
    builder.Services.Configure<RabbitMQSettings>(options =>
    {
        var rabbitMQConfig = builder.Configuration.GetSection("RabbitMQ");
        options.HostName = rabbitMQConfig["HostName"] ?? "localhost";
        options.Port = int.Parse(rabbitMQConfig["Port"] ?? "5672");
        options.UserName = rabbitMQConfig["UserName"] ?? throw new InvalidOperationException("RabbitMQ:UserName is required. Do NOT use default 'guest' credentials.");
        options.Password = rabbitMQConfig["Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is required. Do NOT use default 'guest' credentials.");
        options.VirtualHost = rabbitMQConfig["VirtualHost"] ?? "/";
        options.MediaEventsExchange = rabbitMQConfig["MediaEventsExchange"] ?? "media.events";
        options.MediaCommandsExchange = rabbitMQConfig["MediaCommandsExchange"] ?? "media.commands";
        options.MediaUploadedQueue = rabbitMQConfig["MediaUploadedQueue"] ?? "media.uploaded.queue";
        options.MediaProcessedQueue = rabbitMQConfig["MediaProcessedQueue"] ?? "media.processed.queue";
        options.MediaDeletedQueue = rabbitMQConfig["MediaDeletedQueue"] ?? "media.deleted.queue";
        options.ProcessMediaQueue = rabbitMQConfig["ProcessMediaQueue"] ?? "process.media.queue";
        options.MediaUploadedRoutingKey = rabbitMQConfig["MediaUploadedRoutingKey"] ?? "media.uploaded";
        options.MediaProcessedRoutingKey = rabbitMQConfig["MediaProcessedRoutingKey"] ?? "media.processed";
        options.MediaDeletedRoutingKey = rabbitMQConfig["MediaDeletedRoutingKey"] ?? "media.deleted";
        options.ProcessMediaRoutingKey = rabbitMQConfig["ProcessMediaRoutingKey"] ?? "media.process";
    });

    // Add RabbitMQ services - only if enabled
    if (rabbitMQEnabled)
    {
        builder.Services.AddSingleton<IRabbitMQMediaProducer, RabbitMQMediaProducer>();
        builder.Services.AddSingleton<MediaService.Domain.Interfaces.IEventPublisher, MediaService.Infrastructure.Messaging.RabbitMqEventPublisher>();
        builder.Services.AddHostedService<RabbitMQMediaConsumer>();
    }

    // Audit Service Client for centralized audit logging
    builder.Services.AddHttpClient<MediaService.Application.Interfaces.IAuditServiceClient, MediaService.Infrastructure.External.AuditServiceClient>(client =>
    {
        var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "http://auditservice:8080";
        client.BaseAddress = new Uri(auditServiceUrl);
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

    var app = builder.Build();

    // ============= MIDDLEWARE PIPELINE (Canonical Order — Microsoft/OWASP) =============
    // 1. Global error handling — ALWAYS FIRST to catch exceptions from all middleware
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

    // 10. Audit middleware — after auth (has userId context)
    app.UseAuditMiddleware();

    // 11. Service Discovery Auto-Registration
    app.UseMiddleware<ServiceRegistrationMiddleware>();

    // 12. Endpoints
    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = check => !check.Tags.Contains("external")
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

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


