using System.Text;
using System.Threading.RateLimiting;
using AdvertisingService.Application.Clients;
using AdvertisingService.Application.Features.Campaigns.Commands.CreateCampaign;
using AdvertisingService.Application.Validators;
using AdvertisingService.Infrastructure;
using AdvertisingService.Infrastructure.Persistence;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.Audit.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. External configuration (K8s secrets — auto-loaded via env vars)
// Configuration is loaded via standard ConfigurationManager from environment variables

// 2. Serilog — NO CreateBootstrapLogger()
builder.UseStandardSerilog("AdvertisingService");

// 3. Database (PostgreSQL + EF Core)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=advertisingservice_db;Username=okla_admin;Password=okla_secret";
builder.Services.AddDbContext<AdvertisingDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

// 4. Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
    StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));

// 5. RabbitMQ
var rabbitHostName = builder.Configuration["RabbitMQ:HostName"] ?? "rabbitmq";
var rabbitUserName = builder.Configuration["RabbitMQ:UserName"] ?? builder.Configuration["RabbitMQ:User"] ?? "okla_admin";
var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? "okla_secret";
var rabbitPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var port) ? port : 5672;
builder.Services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
{
    var factory = new RabbitMQ.Client.ConnectionFactory
    {
        HostName = rabbitHostName,
        UserName = rabbitUserName,
        Password = rabbitPassword,
        Port = rabbitPort,
        DispatchConsumersAsync = true
    };
    return factory.CreateConnection();
});

// 6. OpenTelemetry (1.9.0)
builder.Services.AddStandardObservability("AdvertisingService", options =>
{
    options.TracingEnabled = true;
    options.MetricsEnabled = true;
    options.PrometheusEnabled = true;
    options.ExcludedPaths = new[] { "/health", "/health/ready", "/health/live", "/metrics", "/swagger" };
});

// 7. Global error handling
builder.Services.AddStandardErrorHandling(options =>
{
    options.ServiceName = "AdvertisingService";
    options.Environment = builder.Environment.EnvironmentName;
    options.PublishToErrorService = builder.Configuration.GetValue<bool>("ErrorHandling:PublishToErrorService", true);
    options.RabbitMQHost = builder.Configuration["RabbitMQ:HostName"] ?? "rabbitmq";
    options.RabbitMQPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var errPort) ? errPort : 5672;
    options.RabbitMQUser = builder.Configuration["RabbitMQ:UserName"] ?? "okla_admin";
    options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? "okla_secret";
    options.IncludeStackTrace = builder.Environment.IsDevelopment();
});

// 7b. Audit Publisher — usa credenciales de RabbitMQ existentes
builder.Services.AddAuditPublisher(options =>
{
    options.ServiceName = "AdvertisingService";
    options.Enabled = true;
    options.RabbitMq = new CarDealer.Shared.Audit.Configuration.RabbitMqConfig
    {
        Host = builder.Configuration["RabbitMQ:HostName"] ?? "rabbitmq",
        Port = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var auditPort) ? auditPort : 5672,
        Username = builder.Configuration["RabbitMQ:UserName"] ?? "okla_admin",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "okla_secret",
        VirtualHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/"
    };
});

// 8. Infrastructure layer (repos, services, jobs, consumer)
builder.Services.AddInfrastructure(builder.Configuration);

// 9. MediatR + ValidationBehavior
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateCampaignCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateCampaignCommandValidator).Assembly);

// 10. JWT Authentication
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
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// 11. CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://okla.com.do" };

        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token",
                           "X-Requested-With", "X-Idempotency-Key")
              .AllowCredentials();
    });
});

// 11b. Rate Limiting — tracking endpoints (100 requests per minute per IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("tracking", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// 12. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OKLA AdvertisingService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// 13. Controllers — camelCase JSON to match TypeScript frontend expectations (E-001 fix)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 14. Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: new[] { "ready" })
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });

// 15. HTTP clients
builder.Services.AddHttpClient<VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehiclesSaleService"] ?? "http://vehiclessaleservice:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<BillingServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BillingService"] ?? "http://billingservice:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<AuditServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuditService"] ?? "http://auditservice:8080");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<NotificationServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:NotificationService"] ?? "http://notificationservice:8080");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<UserServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"] ?? "http://userservice:8080");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// ========= MIDDLEWARE PIPELINE (CANONICAL ORDER) =========

// 1. Global error handling — ALWAYS FIRST
app.UseGlobalErrorHandling();

// 2. Request logging
app.UseRequestLogging();

// 3. Security headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

// 4. HTTPS redirection — only outside K8s
if (!app.Environment.IsProduction()) app.UseHttpsRedirection();

// 5. Swagger — dev only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6. CORS — BEFORE auth
app.UseCors();

// 6b. Rate Limiting
app.UseRateLimiter();

// 7. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Audit middleware
app.UseAuditMiddleware();

// 9. Endpoints
app.MapControllers();

// 10. Health checks (3 mandatory endpoints)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("external")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// 11. Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AdvertisingDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("AdvertisingService database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database migration failed — will retry on next startup");
    }
}

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
