using ContactService.Domain.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;
using ContactService.Infrastructure.Persistence;
using ContactService.Infrastructure.Repositories;
using ContactService.Application.Clients;
using ContactService.Application.Behaviors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using CarDealer.Shared.MultiTenancy;
using Serilog;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using FluentValidation;
using MediatR;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Encryption;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using ContactService.Infrastructure.Messaging;

const string ServiceName = "ContactService";
const string ServiceVersion = "1.0.2";

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============= CENTRALIZED LOGGING (Serilog → Seq) =============
    builder.UseStandardSerilog(ServiceName);

    // ============= SECRETS PROVIDER =============
    builder.Services.AddSecretProvider();

    // ============= OBSERVABILITY (OpenTelemetry → Jaeger) =============
    builder.Services.AddStandardObservability(builder.Configuration, ServiceName, ServiceVersion);

    // ============= ERROR HANDLING (→ ErrorService) =============
    builder.Services.AddStandardErrorHandling(builder.Configuration, ServiceName);

    // ============= AUDIT (→ AuditService via RabbitMQ) =============
    builder.Services.AddAuditPublisher(builder.Configuration);

    // ============= PII ENCRYPTION — Ley 172-13 =============
    builder.Services.AddPiiEncryption(builder.Configuration);

    // ============= MediatR + FluentValidation =============
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(ContactService.Application.Behaviors.ValidationBehavior<,>).Assembly));
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    builder.Services.AddValidatorsFromAssembly(typeof(ContactService.Application.Behaviors.ValidationBehavior<,>).Assembly);

    // Add services to the container.
    builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpContextAccessor(); // Required for TenantContext

    // Database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Multi-tenancy
    builder.Services.AddScoped<ITenantContext, TenantContext>();

    // Repositories
    builder.Services.AddScoped<IContactRequestRepository, ContactRequestRepository>();
    builder.Services.AddScoped<IContactMessageRepository, ContactMessageRepository>();

    // RabbitMQ Event Publisher — publishes LeadCreatedEvent for dealer notifications (email + WhatsApp)
    builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

    // ── CONTRA #5 FIX: Conversation limit enforcement services ──────────────
    builder.Services.AddScoped<IConversationUsageTracker, ContactService.Infrastructure.Services.ConversationUsageTracker>();
    builder.Services.AddSingleton<IDealerPlanResolver, ContactService.Infrastructure.Services.DealerPlanResolver>();

    // ── CONTRA #5 / OVERAGE BILLING FIX: Overage detail persistence & billing job ──
    builder.Services.AddScoped<IConversationOverageRepository, ContactService.Infrastructure.Services.ConversationOverageRepository>();
    builder.Services.AddHostedService<ContactService.Infrastructure.Services.OverageBillingJob>();

    // ── LEY 172-13: User data deletion consumer (cascade anonymization) ──
    builder.Services.AddHostedService<UserDataDeletionConsumer>();

    var gatewayUrl = builder.Configuration["Gateway:Url"] ?? "http://gateway:8080";

    builder.Services.AddResilientHttpClient<INotificationServiceClient, NotificationServiceClient>(
        builder.Configuration,
        clientName: "NotificationService",
        configureClient: client =>
        {
            client.BaseAddress = new Uri(gatewayUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
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
                ClockSkew = TimeSpan.Zero // No tolerance — tokens expire exactly at exp claim
            };
        });

    // CORS — configurable origins from appsettings
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? (builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "http://localhost:5173" }
            : new[] { "https://okla.com.do" });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  // Security: Restrict to specific HTTP methods and headers (OWASP)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
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

    // Health Checks
    builder.Services.AddHealthChecks();

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
    // 1. Global exception handling — ALWAYS FIRST
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

    // 11. Endpoints
    app.MapControllers();
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

    // Apply database migrations conditionally (disabled in production to avoid race conditions with HPA replicas)
    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            context.Database.Migrate();
            Log.Information("Database migration completed for {ServiceName}", ServiceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database for {ServiceName}", ServiceName);
        }
    }
    else
    {
        Log.Information("Database auto-migration disabled for {ServiceName}. Run migrations via CI/CD pipeline.", ServiceName);
    }

    Log.Information("Starting {ServiceName} v{ServiceVersion}", ServiceName, ServiceVersion);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application {ServiceName} terminated unexpectedly", ServiceName);
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
