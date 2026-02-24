using CarDealer.Shared.Middleware;
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
using CarDealer.Shared.Observability.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;

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

// RabbitMQ Campaign Events Consumer — syncs vehicle promotion flags (IsPremium, IsFeatured)
// when AdvertisingService publishes campaign lifecycle events.
builder.Services.AddHostedService<CampaignEventsConsumer>();

// ========================================
// JWT AUTHENTICATION
// ========================================

var jwtSecret = builder.Configuration["Jwt:Key"] 
    ?? builder.Configuration["JWT:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException(
        "JWT secret key is not configured. Set 'Jwt:Key' in appsettings, 'JWT:Secret', or 'JWT_SECRET' environment variable.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? builder.Configuration["JWT:Issuer"] 
    ?? "CarDealerPlatform";

var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? builder.Configuration["JWT:Audience"] 
    ?? "CarDealerAPI";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // Security: No tolerance for expired tokens (CWE-613)
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

// ========================================
// CORS
// ========================================

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        if (isDev)
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                    "https://okla.com.do",
                    "https://www.okla.com.do",
                    "https://api.okla.com.do")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
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
});

builder.Services.AddHttpClient<VehiclesSaleService.Application.Interfaces.IErrorServiceClient, VehiclesSaleService.Infrastructure.External.ErrorServiceClient>(client =>
{
    var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"] ?? "http://errorservice:8080";
    client.BaseAddress = new Uri(errorServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

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

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// ========================================
// DATABASE MIGRATION
// ========================================

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✅ Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
    }
}

// ========================================
// START
// ========================================

Console.WriteLine("🚀 VehiclesSaleService API starting...");
Console.WriteLine($"📦 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🗄️  Database: {connectionString}");

app.Run();
