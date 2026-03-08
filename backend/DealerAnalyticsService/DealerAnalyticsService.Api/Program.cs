using CarDealer.Shared.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Secrets;
using DealerAnalyticsService.Application.Features.Analytics.Queries;
using DealerAnalyticsService.Application.Services;
using DealerAnalyticsService.Domain.Interfaces;
using DealerAnalyticsService.Infrastructure.Messaging;
using DealerAnalyticsService.Infrastructure.Persistence;
using DealerAnalyticsService.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

const string ServiceName = "DealerAnalyticsService";
const string ServiceVersion = "1.1.0";

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

    // ============= DATABASE (from centralized secrets, NOT hardcoded) =============
    var connectionString = builder.Configuration["Database:ConnectionStrings:PostgreSQL"]
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string must be configured. Do NOT use hardcoded credentials.");

    // Register both DbContext classes
    builder.Services.AddDbContext<AnalyticsDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("DealerAnalyticsService.Infrastructure");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        }));

    builder.Services.AddDbContext<DealerAnalyticsService.Infrastructure.Persistence.DealerAnalyticsDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("DealerAnalyticsService.Infrastructure");
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        }));

    // ============= MediatR =============
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(GetDashboardAnalyticsQuery).Assembly);
    });

    // SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
    builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(DealerAnalyticsService.Application.Behaviors.ValidationBehavior<,>));
    builder.Services.AddValidatorsFromAssembly(typeof(GetDashboardAnalyticsQuery).Assembly);

    // ============= REPOSITORIES =============
    builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
    builder.Services.AddScoped<IDealerAnalyticsRepository, DealerAnalyticsRepository>();
    builder.Services.AddScoped<IConversionFunnelRepository, ConversionFunnelRepository>();
    builder.Services.AddScoped<IMarketBenchmarkRepository, MarketBenchmarkRepository>();
    builder.Services.AddScoped<IDealerInsightRepository, DealerInsightRepository>();
    builder.Services.AddScoped<IDealerSnapshotRepository, DealerSnapshotRepository>();
    builder.Services.AddScoped<IVehiclePerformanceRepository, VehiclePerformanceRepository>();
    builder.Services.AddScoped<ILeadFunnelRepository, LeadFunnelRepository>();
    builder.Services.AddScoped<IDealerBenchmarkRepository, DealerBenchmarkRepository>();
    builder.Services.AddScoped<IDealerAlertRepository, DealerAlertRepository>();
    builder.Services.AddScoped<IInventoryAgingRepository, InventoryAgingRepository>();

    // ============= EVENT PUBLISHING (RabbitMQ) =============
    var rabbitMqEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
    if (rabbitMqEnabled)
        builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
    else
        builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

    // ============= BACKGROUND SERVICES =============
    var enableBackgroundJobs = builder.Configuration.GetValue<bool>("Analytics:EnableBackgroundJobs", true);
    if (enableBackgroundJobs)
    {
        builder.Services.AddHostedService<DailySnapshotService>();
        builder.Services.AddHostedService<AlertAnalysisService>();
    }

    // ============= CORS — configurable origins from appsettings =============
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

    builder.Services.AddAuthorization();

    // ============= CONTROLLERS & SWAGGER =============
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpContextAccessor();

    // ============= RATE LIMITING (Per-IP) =============
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("fixed", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                }));
        options.OnRejected = async (context, ct) =>
        {
            Log.Warning("Rate limit exceeded for {RemoteIp} on {Path}",
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.Request.Path);
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/429",
                title = "Demasiadas solicitudes",
                status = 429,
                detail = "Has excedido el límite de solicitudes. Por favor intenta de nuevo en un momento."
            }, ct);
        };
    });

    // ============= HEALTH CHECKS =============
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
    app.UseGlobalErrorHandling();
    app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());
    app.UseResponseCompression();
    app.UseRequestLogging();

    if (!app.Environment.IsProduction())
        app.UseHttpsRedirection();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dealer Analytics API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
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

    // ============= DATABASE MIGRATION =============
    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();

        var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        try
        {
            analyticsDb.Database.EnsureCreated();
            Log.Information("AnalyticsDbContext database created/verified for {ServiceName}", ServiceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AnalyticsDbContext migration failed for {ServiceName}", ServiceName);
        }

        var dealerAnalyticsDb = scope.ServiceProvider.GetRequiredService<DealerAnalyticsService.Infrastructure.Persistence.DealerAnalyticsDbContext>();
        try
        {
            var creator = dealerAnalyticsDb.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
            try
            {
                creator.CreateTables();
                Log.Information("DealerAnalyticsDbContext tables created for {ServiceName}", ServiceName);
            }
            catch
            {
                Log.Information("DealerAnalyticsDbContext tables already exist for {ServiceName}", ServiceName);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DealerAnalyticsDbContext setup failed for {ServiceName}", ServiceName);
        }
    }

    Log.Information("Starting {ServiceName} v{ServiceVersion} — Dealer Intelligence Platform", ServiceName, ServiceVersion);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application {ServiceName} terminated unexpectedly", "DealerAnalyticsService");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
