using CarDealer.Shared.Middleware;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Secrets;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using VehicleIntelligenceService.Application.Features.Pricing.Queries;
using VehicleIntelligenceService.Domain.Interfaces;
using VehicleIntelligenceService.Infrastructure.Persistence;
using VehicleIntelligenceService.Infrastructure.Persistence.Repositories;
using VehicleIntelligenceService.Infrastructure.Services;

const string ServiceName = "VehicleIntelligenceService";
const string ServiceVersion = "1.0.0";

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

    builder.Services.AddControllers();

    // Database — NO hardcoded fallback (secrets via K8s or env vars)
    builder.Services.AddDbContext<VehicleIntelligenceDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured. Use K8s secrets or environment variables."),
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(3)));

    // Repositories
    builder.Services.AddScoped<IPriceAnalysisRepository, PriceAnalysisRepository>();
    builder.Services.AddScoped<IDemandPredictionRepository, DemandPredictionRepository>();

    // Services
    builder.Services.AddScoped<IPricingEngine, PricingEngine>();

    // MediatR
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(GetPriceSuggestionQuery).Assembly);
    });

    // SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
    builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(VehicleIntelligenceService.Application.Behaviors.ValidationBehavior<,>));
    builder.Services.AddValidatorsFromAssembly(typeof(GetPriceSuggestionQuery).Assembly);

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

    builder.Services.AddAuthorization();

    // CORS — configurable origins from appsettings
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
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With")
                  .AllowCredentials();
        });
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "VehicleIntelligenceService API",
            Version = "v1",
            Description = "API para sugerencias de pricing, demanda y tiempo estimado de venta"
        });
    });

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

    // Health Checks
    builder.Services.AddHealthChecks();

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

    // 5. HTTPS Redirection — only outside K8s
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // 6. Swagger — development only (OWASP API8)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 6.5. Forwarded Headers — required for correct client IP behind K8s/LB (OWASP)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // 7. CORS — before auth
    app.UseCors();

    // 7.5. Rate Limiting — after CORS, before auth (OWASP API4:2023)
    app.UseRateLimiter();

    // 8. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 9. Audit middleware — AFTER auth (has userId context)
    app.UseAuditMiddleware();

    // 10. Endpoints
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

    // Ensure database schema exists
    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VehicleIntelligenceDbContext>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            Log.Information("Database schema verified for {ServiceName}", ServiceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database init failed for {ServiceName}", ServiceName);
        }
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
