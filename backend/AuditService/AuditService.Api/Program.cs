using CarDealer.Shared.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using CarDealer.Shared.Messaging;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Logging.Extensions;
using AuditService.Infrastructure.Extensions;
using AuditService.Infrastructure.Persistence;
using AuditService.Infrastructure.Messaging;
using AuditService.Infrastructure.BackgroundServices;
using AuditService.Infrastructure.Metrics;
using AuditService.Domain.Interfaces;
using AuditService.Shared;
using AuditService.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Enrichers.Span;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using AuditService.Api.Middleware;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

try
{

    // Configure Serilog con enriquecimiento de TraceId/SpanId
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithSpan() // ← AGREGADO: TraceId y SpanId de OpenTelemetry
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j} " +
            "TraceId={TraceId} SpanId={SpanId}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();

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

    // Configure Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Audit Service API",
            Version = "v1",
            Description = "Microservice for handling audit logs and security events",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Audit Service Team",
                Email = "audit-service@cargurus.com"
            }
        });

        // Enable XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "https://localhost:3000" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Add Infrastructure services (Database, Repositories, etc.)
    builder.Services.AddInfrastructure(builder.Configuration);

    // ========== JWT AUTHENTICATION ==========
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    var jwtAudience = builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });
    builder.Services.AddAuthorization();

    // ========== SERVICE DISCOVERY ==========

    // Consul Client
    builder.Services.AddSingleton<IConsulClient>(sp => new ConsulClient(config =>
    {
        config.Address = new Uri(builder.Configuration["Consul:Address"] ?? "http://localhost:8500");
    }));

    // Service Discovery Services
    builder.Services.AddScoped<IServiceRegistry, ConsulServiceRegistry>();
    builder.Services.AddScoped<IServiceDiscovery, ConsulServiceDiscovery>();
    builder.Services.AddHttpClient("HealthCheck");
    builder.Services.AddScoped<IHealthChecker, HttpHealthChecker>();

    // ========================================

    // ========== ADVANCED FEATURES ==========

    // Dead Letter Queue — PostgreSQL-backed (survives pod restarts during auto-scaling)
    builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "AuditService");

    // Shared RabbitMQ connection (1 connection per pod instead of N per class)
    builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

    // Event Consumer — listens to cardealer.events exchange, routes all events to audit.all-events queue
    builder.Services.AddHostedService<AuditService.Infrastructure.Messaging.RabbitMqEventConsumer>();

    // Background Service para procesar DLQ
    builder.Services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();
    builder.Services.AddHostedService<DeadLetterQueueProcessor>();

    // Métricas personalizadas (Singleton para compartir estado)
    builder.Services.AddSingleton<AuditServiceMetrics>();

    // Configurar OpenTelemetry
    var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "AuditService";
    var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
    var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["service.namespace"] = "cardealer"
            }))
        .WithTracing(tracing => tracing
            .SetSampler(new ParentBasedSampler(
                // Estrategia de muestreo: 10% en producción, 100% en desarrollo
                new TraceIdRatioBasedSampler(
                    builder.Environment.IsProduction() ? 0.1 : 1.0)))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context =>
                {
                    // Filtrar health checks para reducir ruido
                    return !context.Request.Path.StartsWithSegments("/health");
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddSource("AuditService.*")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("AuditService.*")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            }));

    // ========================================

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AuditDbContext>(
            name: "database",
            tags: new[] { "ready", "external" });

    // Add Health Checks UI - DISABLED due to missing IdentityModel dependency
    /*
    builder.Services.AddHealthChecksUI(setup =>
    {
        setup.SetHeaderText("Audit Service - Health Status");
        setup.AddHealthCheckEndpoint("Audit Service", "/health");
        setup.SetEvaluationTimeInSeconds(60);
        setup.SetApiMaxActiveRequests(3);
        setup.MaximumHistoryEntriesPerEndpoint(50);
    })
    .AddInMemoryStorage();
    */

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

    // Global Error Handling — RFC 7807 ProblemDetails
    builder.Services.AddStandardErrorHandling(builder.Configuration, "AuditService");

    var app = builder.Build();

    // Configure the HTTP request pipeline
    // 1. Global Error Handling — ALWAYS FIRST (catches all middleware + controller exceptions)
    app.UseGlobalErrorHandling();

    // 2. Request Logging — structured with TraceId, UserId, CorrelationId
    app.UseRequestLogging();

    // OWASP Security Headers
    app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

    // Response Compression — early, after error handling
    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audit Service API v1");
            c.RoutePrefix = "swagger";
        });

        // Apply migrations in development
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    if (app.Environment.IsProduction())
    {
        // HTTPS redirect disabled in production — TLS terminates at K8s Ingress, internal traffic is HTTP on port 8080

        // Apply migrations in production if enabled
        var databaseSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseSettings>>().Value;
        if (databaseSettings.AutoMigrate)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }

    // Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseCors("CorsPolicy");

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Service Discovery Auto-Registration - DISABLED (Consul not available)
    // app.UseMiddleware<ServiceRegistrationMiddleware>();

    app.MapControllers();

    // Health check endpoints
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
        Predicate = _ => false // Only verifies process is alive — no real checks
    });

    // MapHealthChecksUI - DISABLED (UI configuration commented out above)
    /*
    app.MapHealthChecksUI(setup =>
    {
        setup.UIPath = "/health-ui";
        // setup.AddCustomStylesheet("healthchecks-ui.css"); // Comentar si no existe el archivo
    });
    */

    // NOTE: Inline exception handler REMOVED — UseGlobalErrorHandling() at the top of the pipeline
    // now catches all exceptions and returns RFC 7807 ProblemDetails responses.

    Log.Information("Audit Service starting up...");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "💀 AuditService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program class for integration testing
public partial class Program { }