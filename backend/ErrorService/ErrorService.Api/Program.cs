using ErrorService.Domain.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;
using ErrorService.Infrastructure.Messaging;
using ErrorService.Infrastructure.Persistence;
using ErrorService.Infrastructure.Services;
using ErrorService.Infrastructure.Services.Messaging;
using ErrorService.Shared.Extensions;
using ErrorService.Shared.Middleware;
using ErrorService.Shared.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using Serilog.Enrichers.Span;
using CarDealer.Shared.Database;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Messaging;
using CarDealer.Shared.Observability.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using FluentValidation;
using ErrorService.Application.Behaviors;
using MediatR;

using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using ErrorService.Api.Middleware;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "ErrorService";
const string ServiceVersion = "1.0.0";

try
{
var builder = WebApplication.CreateBuilder(args);

// Secret Provider for externalized configuration
builder.Services.AddSecretProvider();

// ============= CENTRALIZED LOGGING (Serilog → Seq) =============
builder.UseStandardSerilog(ServiceName);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
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

// Configurar Swagger con soporte JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ErrorService API",
        Version = "v1",
        Description = "API para gestión centralizada de errores en CarDealer Microservices"
    });

    // Configurar autenticación JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurar autenticación JWT - using secrets
var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,              // SECURITY: never config-driven
        ValidateAudience = true,            // SECURITY: never config-driven
        ValidateLifetime = true,            // SECURITY: never config-driven
        ValidateIssuerSigningKey = true,    // SECURITY: never config-driven
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // No tolerance — tokens expire exactly at exp claim
    };

    // Logging para debugging
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

// Configurar políticas de autorización
builder.Services.AddAuthorization(options =>
{
    // Política para acceso general al ErrorService
    options.AddPolicy("ErrorServiceAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("service", "errorservice", "all");
    });

    // Política para operaciones administrativas
    options.AddPolicy("ErrorServiceAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("role", "admin", "errorservice-admin");
    });

    // Política para solo lectura
    options.AddPolicy("ErrorServiceRead", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// Database Context (multi-provider configuration)
builder.Services.AddDatabaseProvider<ApplicationDbContext>(builder.Configuration);

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

// Application Services
builder.Services.AddScoped<IErrorLogRepository, EfErrorLogRepository>();
builder.Services.AddScoped<IErrorReporter, ErrorReporter>();

// Métricas personalizadas (Singleton para compartir estado)
builder.Services.AddSingleton<ErrorService.Application.Metrics.ErrorServiceMetrics>();

// Dead Letter Queue para eventos fallidos (PostgreSQL-backed, survives pod restarts)
builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "ErrorService");

// Shared RabbitMQ connection (1 connection per pod instead of N per class)
builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

// RabbitMQ Configuration - Conditional based on Enabled flag
var rabbitMqEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);

if (rabbitMqEnabled)
{
    // RabbitMQ enabled - use real implementations with DLQ
    Log.Information("🐰 RabbitMQ ENABLED - Using real messaging implementations");
    builder.Services.AddSingleton<ErrorService.Infrastructure.Messaging.RabbitMqEventPublisher>();
    builder.Services.AddSingleton<IEventPublisher>(sp =>
        sp.GetRequiredService<ErrorService.Infrastructure.Messaging.RabbitMqEventPublisher>());

    // Background Service para procesar DLQ
    builder.Services.AddSingleton<ErrorService.Infrastructure.Messaging.IDeadLetterQueue, ErrorService.Infrastructure.Messaging.InMemoryDeadLetterQueue>();
    builder.Services.AddHostedService<ErrorService.Infrastructure.Messaging.DeadLetterQueueProcessor>();
}
else
{
    // RabbitMQ disabled - use NoOp implementation
    Log.Information("🚫 RabbitMQ DISABLED - Using NoOp messaging implementation (events will be logged only)");
    builder.Services.AddSingleton<IEventPublisher, ErrorService.Infrastructure.Messaging.NoOpEventPublisher>();
}

// Agregar MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ErrorService.Application.UseCases.LogError.LogErrorCommand).Assembly));

// Registrar FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(ErrorService.Application.UseCases.LogError.LogErrorCommandValidator).Assembly);

// Agregar behavior de validación para MediatR
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Configurar OpenTelemetry (standardized via shared library)
builder.Services.AddStandardObservability(builder.Configuration, ServiceName, ServiceVersion);

// Configurar el manejo de errores
builder.Services.AddErrorHandling("ErrorService");

// ErrorService IS the error service — register a NoOp IErrorPublisher to avoid
// circular dependency (it shouldn't publish errors to itself via RabbitMQ).
// This is needed because UseGlobalErrorHandling() injects IErrorPublisher.
builder.Services.AddSingleton<CarDealer.Shared.ErrorHandling.Interfaces.IErrorPublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    return new ErrorService.Api.Services.NoOpErrorPublisher(logger);
});

// Configurar Audit Publisher
builder.Services.AddAuditPublisher(builder.Configuration);

// Configurar Rate Limiting
var rateLimitingConfig = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingConfiguration>()
    ?? new RateLimitingConfiguration();
builder.Services.AddCustomRateLimiting(rateLimitingConfig);

// Configurar RabbitMQ
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<ErrorServiceRabbitMQSettings>(builder.Configuration.GetSection("ErrorService"));

// Registrar el consumidor RabbitMQ como hosted service (solo si RabbitMQ está habilitado)
if (rabbitMqEnabled)
{
    builder.Services.AddHostedService<RabbitMQErrorConsumer>();
    Log.Information("🐰 RabbitMQErrorConsumer registered as hosted service");
}
else
{
    Log.Information("🚫 RabbitMQErrorConsumer NOT registered - RabbitMQ is disabled");
}

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
// 1. Global error handling — ALWAYS FIRST (shared library)
app.UseGlobalErrorHandling();

// 2. Request Logging (shared library)
app.UseRequestLogging();

// 3. Security Headers (OWASP) — early in pipeline
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

// 4. Response Compression — early, after error handling
app.UseResponseCompression();

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

// 7. CORS — BEFORE rate limiting (so OPTIONS preflight isn't blocked)
app.UseCors();

// 7.5. Rate Limiting — after CORS, before auth
app.UseMiddleware<RateLimitBypassMiddleware>();
app.UseCustomRateLimiting(rateLimitingConfig);

// 8. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 9. Audit middleware — after auth (has userId context)
app.UseAuditMiddleware();

// 10. Response capture middleware
app.UseMiddleware<ResponseCaptureMiddleware>();

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

// Apply migrations conditionally (disabled in production to avoid race conditions with HPA replicas)
var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Log.Information("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying database migrations.");
    }
}
else
{
    Log.Information("Database auto-migration disabled for ErrorService. Run migrations via CI/CD pipeline.");
}

Log.Information("ErrorService starting up...");
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

// Make Program class accessible for integration testing
public partial class Program { }

