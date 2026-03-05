using CarDealer.Shared.Middleware;
using CarDealer.Shared.Messaging;
using RoleService.Domain.Interfaces;
using RoleService.Infrastructure.Messaging;
using RoleService.Infrastructure.Persistence;
// using RoleService.Infrastructure.Services;
// using RoleService.Infrastructure.Services.Messaging;
using RoleService.Shared.Extensions;
using RoleService.Shared.Middleware;
using RoleService.Shared.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Enrichers.Span;
using CarDealer.Shared.Database;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Audit.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using FluentValidation;
using RoleService.Application.Behaviors;
using MediatR;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using RoleService.Api.Middleware;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add Secret Provider for externalized secrets
builder.Services.AddSecretProvider();

// Configurar Serilog con enriquecimiento de TraceId
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan() // Agregar TraceId, SpanId de OpenTelemetry
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j} TraceId={TraceId} SpanId={SpanId}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

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
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configurar Swagger con soporte JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RoleService API",
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

// Configurar autenticación JWT con secretos externalizados
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
        ValidateIssuer = jwtSettings.GetValue<bool>("ValidateIssuer", true),
        ValidateAudience = jwtSettings.GetValue<bool>("ValidateAudience", true),
        ValidateLifetime = jwtSettings.GetValue<bool>("ValidateLifetime", true),
        ValidateIssuerSigningKey = jwtSettings.GetValue<bool>("ValidateIssuerSigningKey", true),
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(5)
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

// Configurar políticas de autorización RBAC
// NOTA: AuthService genera JWT con claim "account_type" (int) en vez de "role" (string).
//   AccountType: Guest=0, Individual=1, Dealer=2, DealerEmployee=3, Admin=4, PlatformEmployee=5
//   Se verifican AMBOS formatos para compatibilidad con tokens de test y producción.
builder.Services.AddAuthorization(options =>
{
    // Política para acceso general al RoleService (lectura)
    options.AddPolicy("RoleServiceAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    // Política para gestión de roles (admin:manage-roles)
    options.AddPolicy("ManageRoles", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            // Admin AccountType (4) tiene acceso — incluye SuperAdmin y Admin
            if (context.User.HasClaim("account_type", "4"))
                return true;

            // Fallback: verificar claim "role" (tokens de test/legacy)
            if (context.User.HasClaim("role", "SuperAdmin") ||
                context.User.HasClaim("role", "Admin") ||
                context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin") ||
                context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Admin"))
                return true;

            // Verificar claim específico de permiso
            if (context.User.HasClaim("permission", "admin:manage-roles"))
                return true;

            return false;
        });
    });

    // Política para gestión de permisos (solo SuperAdmin / Admin)
    options.AddPolicy("ManagePermissions", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            // Admin AccountType (4) tiene acceso
            if (context.User.HasClaim("account_type", "4"))
                return true;

            // Fallback: verificar claim "role" (tokens de test/legacy)
            if (context.User.HasClaim("role", "SuperAdmin") ||
                context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin"))
                return true;

            // Verificar claim específico de permiso
            if (context.User.HasClaim("permission", "admin:manage-permissions"))
                return true;

            return false;
        });
    });

    // Política para operaciones administrativas generales
    options.AddPolicy("AdminAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            // Admin (4) o PlatformEmployee (5)
            if (context.User.HasClaim("account_type", "4") ||
                context.User.HasClaim("account_type", "5"))
                return true;

            // Fallback: verificar claim "role" (tokens de test/legacy)
            return context.User.HasClaim("role", "SuperAdmin") ||
                   context.User.HasClaim("role", "Admin") ||
                   context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin") ||
                   context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Admin") ||
                   context.User.HasClaim("permission", "admin:access");
        });
    });
});

// Database Context (multi-provider configuration)
builder.Services.AddDatabaseProvider<ApplicationDbContext>(builder.Configuration);

// Service Discovery Configuration
builder.Services.AddSingleton<IConsulClient>(sp =>
{
    var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
    return new ConsulClient(config => config.Address = new Uri(consulAddress));
});

builder.Services.AddScoped<IServiceRegistry, ConsulServiceRegistry>();
builder.Services.AddScoped<IServiceDiscovery, ConsulServiceDiscovery>();
builder.Services.AddHttpClient("HealthCheck");
builder.Services.AddScoped<IHealthChecker, HttpHealthChecker>();

// Application Services - Repositories
builder.Services.AddScoped<IRoleRepository, RoleService.Infrastructure.Repositories.EfRoleRepository>();
builder.Services.AddScoped<IPermissionRepository, RoleService.Infrastructure.Repositories.EfPermissionRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RoleService.Infrastructure.Repositories.EfRolePermissionRepository>();
builder.Services.AddScoped<IRoleLogRepository, RoleService.Infrastructure.Persistence.EfRoleLogRepository>();

// User Context Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RoleService.Application.Interfaces.IUserContextService, RoleService.Infrastructure.Services.UserContextService>();

// Permission Cache Service (usando Redis si está disponible, sino memoria)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "RoleService:";
    });
    Log.Information("Using Redis for permission caching: {RedisConnection}", redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
    Log.Warning("Redis not configured - using in-memory cache for permissions (not recommended for production)");
}
builder.Services.AddScoped<RoleService.Application.Interfaces.IPermissionCacheService, RoleService.Infrastructure.Services.PermissionCacheService>();

// External Service Clients
builder.Services.AddHttpClient<RoleService.Application.Interfaces.IAuditServiceClient, RoleService.Infrastructure.External.AuditServiceClient>(client =>
{
    var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "https://localhost:7287";
    client.BaseAddress = new Uri(auditServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<RoleService.Application.Interfaces.INotificationServiceClient, RoleService.Infrastructure.External.NotificationServiceClient>(client =>
{
    var notificationServiceUrl = builder.Configuration["ServiceUrls:NotificationService"] ?? "https://localhost:45954";
    client.BaseAddress = new Uri(notificationServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<RoleService.Application.Interfaces.IErrorServiceClient, RoleService.Infrastructure.External.ErrorServiceClient>(client =>
{
    var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"] ?? "https://localhost:45952";
    client.BaseAddress = new Uri(errorServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Error Reporter - envía errores al ErrorService
var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"];
if (!string.IsNullOrEmpty(errorServiceUrl))
{
    builder.Services.AddHttpClient<IErrorReporter, RoleService.Infrastructure.Services.HttpErrorReporter>(client =>
    {
        client.BaseAddress = new Uri(errorServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
    });
}
else
{
    builder.Services.AddSingleton<IErrorReporter, RoleService.Infrastructure.Services.NoOpErrorReporter>();
}

// Other Services
// builder.Services.AddScoped<IRoleReporter, RoleReporter>();

// Métricas personalizadas (Singleton para compartir estado)
builder.Services.AddSingleton<RoleService.Application.Metrics.RoleServiceMetrics>();

// Dead Letter Queue — PostgreSQL-backed (survives pod restarts during auto-scaling)
// Shared RabbitMQ connection (1 connection per pod instead of N per class)
builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "RoleService");
builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

// Event Publisher for RabbitMQ (con DLQ integrado)
// NOTE: Using NoOpEventPublisher for development. Enable RabbitMQ in production.
var useRabbitMq = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
if (useRabbitMq)
{
    builder.Services.AddSingleton<RoleService.Infrastructure.Messaging.RabbitMqEventPublisher>();
    builder.Services.AddSingleton<IEventPublisher>(sp =>
        sp.GetRequiredService<RoleService.Infrastructure.Messaging.RabbitMqEventPublisher>());
    builder.Services.AddSingleton<RoleService.Infrastructure.Messaging.IDeadLetterQueue, RoleService.Infrastructure.Messaging.InMemoryDeadLetterQueue>();
    builder.Services.AddHostedService<RoleService.Infrastructure.Messaging.DeadLetterQueueProcessor>();
}
else
{
    // Use NoOp publisher for development without RabbitMQ
    builder.Services.AddSingleton<IEventPublisher, RoleService.Infrastructure.Messaging.NoOpEventPublisher>();
}

// Background Service para procesar DLQ
// (Moved into conditional block above)

// Agregar MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RoleService.Application.UseCases.Roles.CreateRole.CreateRoleCommand).Assembly));

// Registrar FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(RoleService.Application.UseCases.Roles.CreateRole.CreateRoleCommandValidator).Assembly);

// Agregar behavior de validación para MediatR
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Configurar OpenTelemetry
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "RoleService";
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
            // Estrategia de muestreo basada en ratio
            // En producción: captura 10% de traces normales, 100% de errores
            new TraceIdRatioBasedSampler(
                builder.Environment.IsProduction() ? 0.1 : 1.0))) // Dev: 100%, Prod: 10%
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
        .AddSource("RoleService.*")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("RoleService.*")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }));

// Configurar el manejo de errores
builder.Services.AddErrorHandling("RoleService");

// Configurar Audit Publisher
builder.Services.AddAuditPublisher(builder.Configuration);

// Configurar Rate Limiting
var rateLimitingConfig = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingConfiguration>()
    ?? new RateLimitingConfiguration();
builder.Services.AddCustomRateLimiting(rateLimitingConfig);

// Configurar RabbitMQ
// builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
// builder.Services.Configure<RoleServiceRabbitMQSettings>(builder.Configuration.GetSection("RoleService"));

// Registrar el consumidor RabbitMQ como hosted service
// builder.Services.AddHostedService<RabbitMQErrorConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Security (CWE-532): UseRequestLogging() from shared lib masks sensitive headers
app.UseRequestLogging();

// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware para bypass de Rate Limiting (debe estar antes del middleware de rate limiting)
app.UseMiddleware<RateLimitBypassMiddleware>();

// Middleware para Rate Limiting (debe estar antes de otros middlewares)
app.UseCustomRateLimiting(rateLimitingConfig);

app.UseHttpsRedirection();

// CORS middleware (before auth)
app.UseCors();

// Agregar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Service Discovery Auto-Registration (only if Consul is enabled)
var consulEnabled = app.Configuration.GetValue<bool>("Consul:Enabled", false);
if (consulEnabled)
{
    app.UseMiddleware<ServiceRegistrationMiddleware>();
}

// Middleware para capturar respuestas
app.UseMiddleware<ResponseCaptureMiddleware>();

// Middleware para manejo de errores
app.UseErrorHandling();

// Middleware para auditoría
app.UseAuditMiddleware();

app.MapControllers();

// Health check endpoints - acceso anónimo
app.MapHealthChecks("/health");

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
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

Log.Information("RoleService starting up...");
app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
