using CRMService.Domain.Interfaces;
using CRMService.Infrastructure.Persistence;
using CRMService.Infrastructure.Persistence.Repositories;
using CarDealer.Shared.Extensions;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Database;
using CarDealer.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using Serilog;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "CRMService";
const string ServiceVersion = "1.0.0";

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============= CENTRALIZED LOGGING (Serilog → Seq) =============
    builder.UseStandardSerilog(ServiceName);

    // ============= OBSERVABILITY (OpenTelemetry → Jaeger) =============
    builder.Services.AddStandardObservability(builder.Configuration, ServiceName, ServiceVersion);

    // ============= ERROR HANDLING (→ ErrorService) =============
    builder.Services.AddStandardErrorHandling(builder.Configuration, ServiceName);

    // ============= AUDIT (→ AuditService via RabbitMQ) =============
    builder.Services.AddAuditPublisher(builder.Configuration);

    // Add secret provider for Docker secrets
    builder.Services.AddSecretProvider();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "CRM Service API", Version = "v1" });
    });

    // Configure DbContext with multi-provider support
    builder.Services.AddDatabaseProvider<CRMDbContext>(builder.Configuration);

    // Multi-tenancy
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITenantContext, TenantContext>();

    // Register repositories
    builder.Services.AddScoped<ILeadRepository, LeadRepository>();
    builder.Services.AddScoped<IDealRepository, DealRepository>();
    builder.Services.AddScoped<IPipelineRepository, PipelineRepository>();
    builder.Services.AddScoped<IActivityRepository, ActivityRepository>();

    // Configure MediatR
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(CRMService.Application.DTOs.LeadDto).Assembly));

    // SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
    builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(CRMService.Application.Behaviors.ValidationBehavior<,>));

    // Register FluentValidation validators
    builder.Services.AddValidatorsFromAssembly(
        typeof(CRMService.Application.Validators.SecurityValidators).Assembly);

    // Add CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? (builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "http://localhost:5173" }
            : new[] { "https://okla.com.do", "https://www.okla.com.do", "https://api.okla.com.do" });

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

    // Add Health Checks
    builder.Services.AddHealthChecks();

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

            // Support HttpOnly cookies
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("auth_token", out var token))
                        context.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    // Module Access (for paid feature gating) - disabled in development
    // builder.Services.AddModuleAccessServices(builder.Configuration);

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

    // ============= RATE LIMITING (OWASP API4:2023) =============
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("StandardPolicy", opt =>
        {
            opt.PermitLimit = 60;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
    });

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

    // 4. HTTPS Redirection — only outside K8s (TLS terminates at Ingress)
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // 5. Swagger — development only (OWASP API8)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 5.5. Forwarded Headers — required for correct client IP behind K8s/LB (OWASP)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // 6. CORS — before auth
    app.UseCors();

    // 6.5. Rate Limiting — after CORS, before auth (OWASP API4:2023)
    app.UseRateLimiter();

    // 7. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 8. Audit middleware — AFTER auth (has userId context)
    app.UseAuditMiddleware();

    // Module access verification - enable in production, configurable via appsettings
    if (builder.Configuration.GetValue<bool>("ModuleAccess:Enabled", false))
    {
        app.UseModuleAccess("crm-advanced");
    }

    // 9. Endpoints
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

    // Apply database migrations conditionally
    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CRMDbContext>();
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

// Make the implicit Program class public so it can be accessed by tests
public partial class Program { }
 