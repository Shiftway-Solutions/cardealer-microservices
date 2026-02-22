using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthService.Infrastructure.Extensions;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.BackgroundServices;
using AuthService.Infrastructure.Metrics;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Entities;
using Serilog;
using System.Reflection;
using FluentValidation;
using AuthService.Shared;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Cors;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.Messaging;
using System.Threading.RateLimiting;
using AuthService.Infrastructure.Services.Messaging;
using AuthService.Infrastructure.Middleware;
using AuthService.Domain.Interfaces.Services;
using AuthService.Infrastructure.External;
using AuthService.Infrastructure.Services.Notification;
using AuthService.Infrastructure.Services.GeoLocation;
using AuthService.Application.Services;
using Microsoft.Extensions.Options;
using CarDealer.Shared.Database;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using AuthService.Api.Middleware;
using AuthService.Application.Common.Interfaces;
using AuthService.Infrastructure.Services.Security;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// FASE 2: OBSERVABILITY - Logging centralizado con Serilog + Seq
// ============================================================================
builder.UseStandardSerilog("AuthService", options =>
{
    options.SeqEnabled = true;
    options.SeqServerUrl = builder.Configuration["Logging:Seq:ServerUrl"] ?? "http://seq:5341";
    options.FileEnabled = builder.Configuration.GetValue<bool>("Logging:File:Enabled", false);
    options.FilePath = builder.Configuration["Logging:File:Path"] ?? "logs/authservice-.log";
    options.RabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
    options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ:HostName"] ?? "rabbitmq";
    options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
    options.RabbitMQUser = builder.Configuration["RabbitMQ:UserName"] ?? builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured");
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
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddLogging();

// ============================================================================
// FASE 2: OBSERVABILITY - OpenTelemetry Tracing + Metrics
// ============================================================================
builder.Services.AddStandardObservability("AuthService", options =>
{
    options.TracingEnabled = true;
    options.MetricsEnabled = true;
    options.OtlpEndpoint = builder.Configuration["Observability:Otlp:Endpoint"] ?? "http://jaeger:4317";
    options.SamplingRatio = builder.Configuration.GetValue<double>("Observability:SamplingRatio", builder.Environment.IsProduction() ? 0.1 : 1.0);
    options.PrometheusEnabled = builder.Configuration.GetValue<bool>("Observability:Prometheus:Enabled", true);
    options.ExcludedPaths = new[] { "/health", "/health/ready", "/health/live", "/metrics", "/swagger" };
});

// ============================================================================
// FASE 2: OBSERVABILITY - Error Handling centralizado
// ============================================================================
builder.Services.AddStandardErrorHandling(options =>
{
    options.ServiceName = "AuthService";
    options.Environment = builder.Environment.EnvironmentName;
    options.PublishToErrorService = builder.Configuration.GetValue<bool>("ErrorHandling:PublishToErrorService", true);
    options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ:HostName"] ?? "rabbitmq";
    options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
    options.RabbitMQUser = builder.Configuration["RabbitMQ:UserName"] ?? builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured");
    options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
    options.IncludeStackTrace = builder.Environment.IsDevelopment();
});

// ============================================================================
// FASE 5: Audit Publisher - Eventos de auditoría
// ============================================================================
builder.Services.AddAuditPublisher(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
    {
        var rateLimitSettings = builder.Configuration.GetSection("Security:RateLimit").Get<RateLimitSettings>();
        limiterOptions.PermitLimit = rateLimitSettings?.RequestsPerMinute ?? 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// CORS 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
        var allowedMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? new string[0];
        var allowedHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? new string[0];
        var allowCredentials = builder.Configuration.GetValue<bool>("Cors:AllowCredentials");

        policy.WithOrigins(allowedOrigins)
              .WithMethods(allowedMethods)
              .WithHeaders(allowedHeaders);

        if (allowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});

// TODA LA CONFIGURACIÓN EN UN SOLO LUGAR
builder.Services.AddInfrastructure(builder.Configuration);

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

// Database Context (multi-provider configuration)
builder.Services.AddDatabaseProvider<ApplicationDbContext>(builder.Configuration);

// ✅ NUEVO: Custom Metrics
builder.Services.AddSingleton<AuthServiceMetrics>();

// MediatR & FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("AuthService.Application")));
builder.Services.AddValidatorsFromAssembly(Assembly.Load("AuthService.Application"));

// ValidationBehavior — ensures FluentValidation validators (NoSqlInjection, NoXss) run automatically in MediatR pipeline
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AuthService.Application.Behaviors.ValidationBehavior<,>));

// HttpClientFactory for OAuth providers (Google, Microsoft, Facebook, Apple)
builder.Services.AddHttpClient();

// SecurityConfigProvider – reads security settings from ConfigurationService (port 15124)
var configServiceUrl = builder.Configuration["ConfigurationService:BaseUrl"] ?? "http://localhost:15124";
builder.Services.AddHttpClient<ISecurityConfigProvider, SecurityConfigProvider>(client =>
{
    client.BaseAddress = new Uri(configServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
Log.Information("SecurityConfigProvider registered → {Url}", configServiceUrl);

// Settings (JwtSettings is configured in AddInfrastructure with secrets merged in)
builder.Services.Configure<NotificationServiceSettings>(builder.Configuration.GetSection("NotificationService"));

// RabbitMQ Configuration - Conditional based on Enabled flag
var rabbitMqEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<ErrorServiceRabbitMQSettings>(builder.Configuration.GetSection("ErrorService"));
builder.Services.Configure<NotificationServiceRabbitMQSettings>(builder.Configuration.GetSection("NotificationService"));

// Shared RabbitMQ connection (1 connection per pod instead of N per class)
builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

// PostgreSQL-backed Dead Letter Queue (survives pod restarts during auto-scaling)
builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "AuthService");

if (rabbitMqEnabled)
{
    // RabbitMQ enabled - use real implementations with DLQ
    builder.Services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();
    builder.Services.AddHostedService<DeadLetterQueueProcessor>();
    builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
    builder.Services.AddSingleton<IErrorEventProducer, RabbitMQErrorProducer>();
    builder.Services.AddSingleton<INotificationEventProducer, RabbitMQNotificationProducer>();
    Log.Information("RabbitMQ ENABLED - Using real messaging implementations");
}
else
{
    // RabbitMQ disabled - use NoOp implementations
    builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
    builder.Services.AddSingleton<IErrorEventProducer, NoOpErrorProducer>();
    builder.Services.AddSingleton<INotificationEventProducer, NoOpNotificationProducer>();
    Log.Information("RabbitMQ DISABLED - Using NoOp messaging implementations");
}

// Registrar AuthNotificationService con el constructor CORRECTO (5 parámetros)
builder.Services.AddScoped<IAuthNotificationService>(provider =>
{
    var notificationClient = provider.GetRequiredService<NotificationServiceClient>();
    var notificationProducer = provider.GetRequiredService<INotificationEventProducer>();
    var settings = provider.GetRequiredService<IOptions<NotificationServiceSettings>>();
    var rabbitMqSettings = provider.GetRequiredService<IOptions<NotificationServiceRabbitMQSettings>>();
    var logger = provider.GetRequiredService<ILogger<AuthNotificationService>>();

    return new AuthNotificationService(
        notificationClient,
        notificationProducer,
        settings,
        rabbitMqSettings,
        logger
    );
});

// AUTH-SEC-005: Revoked device service for tracking revoked devices
builder.Services.AddScoped<IRevokedDeviceService, RevokedDeviceService>();

// Geolocation service for IP-based location lookup
builder.Services.AddHttpClient<IGeoLocationService, IpApiGeoLocationService>();

// Audit Service Client for centralized audit logging
builder.Services.AddHttpClient<AuthService.Application.Interfaces.IAuditServiceClient, AuthService.Infrastructure.External.AuditServiceClient>(client =>
{
    var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "http://auditservice:8080";
    client.BaseAddress = new Uri(auditServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 🚨 CONSTRUIR LA APLICACIÓN
var app = builder.Build();

// Migraciones (solo para proveedores relacionales, no InMemory)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var authContext = services.GetRequiredService<ApplicationDbContext>();

        // Check if we should auto-migrate (disabled for InMemory testing)
        var configuration = services.GetRequiredService<IConfiguration>();
        var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate", true);

        // Only migrate if we're using a relational provider and auto-migrate is enabled
        if (autoMigrate && authContext.Database.IsRelational())
        {
            authContext.Database.Migrate();
            Log.Information("AuthService database migrations applied successfully.");
        }
        else if (!authContext.Database.IsRelational())
        {
            authContext.Database.EnsureCreated();
            Log.Information("AuthService using non-relational database (InMemory), EnsureCreated called.");
        }

        // Seed default admin user and roles
        var seedAdmin = configuration.GetValue<bool>("Database:SeedDefaultAdmin", true);
        if (seedAdmin)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            await AdminSeeder.SeedAsync(authContext, userManager, roleManager, logger);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database migration");
    }
}

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================

// FASE 2: Global Error Handling - PRIMERO para capturar todas las excepciones
app.UseGlobalErrorHandling();

// FASE 2: Request Logging con enrichment de TraceId, UserId, CorrelationId
app.UseRequestLogging();

// Security Headers (OWASP) — before routing and auth
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

// Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(); // CORS debe ir primero
app.UseHttpsRedirection();
// NOTA: ErrorHandlingMiddleware local reemplazado por UseGlobalErrorHandling()
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Service Discovery Auto-Registration
app.UseMiddleware<ServiceRegistrationMiddleware>();

// FASE 5: Audit Middleware
app.UseAuditMiddleware();

// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("external") // Exclude external service checks (Consul not available)
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapControllers();

Log.Information("AuthService starting up...");
app.Run();

public partial class Program { }