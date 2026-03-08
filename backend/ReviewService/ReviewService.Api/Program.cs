using CarDealer.Shared.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Secrets;
using Microsoft.EntityFrameworkCore;
using ReviewService.Infrastructure.Persistence;
using ReviewService.Domain.Interfaces;
using ReviewService.Infrastructure.Persistence.Repositories;
using FluentValidation;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.IO.Compression;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "ReviewService";
const string ServiceVersion = "1.0.1";

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

    // ============= APPLICATION SERVICES =============
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    // Entity Framework
builder.Services.AddDbContext<ReviewDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ReviewService.Infrastructure"));
});

// Repositories
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewSummaryRepository, ReviewSummaryRepository>();
builder.Services.AddScoped<IReviewResponseRepository, ReviewResponseRepository>();
// Sprint 15 - Repositorios adicionales
builder.Services.AddScoped<IReviewHelpfulVoteRepository, ReviewHelpfulVoteRepository>();
builder.Services.AddScoped<ISellerBadgeRepository, SellerBadgeRepository>();
builder.Services.AddScoped<IReviewRequestRepository, ReviewRequestRepository>();
builder.Services.AddScoped<IFraudDetectionLogRepository, FraudDetectionLogRepository>();

// MediatR
builder.Services.AddMediatR(cfg => {

// SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ReviewService.Application.Behaviors.ValidationBehavior<,>));
    cfg.RegisterServicesFromAssembly(Assembly.Load("ReviewService.Application"));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.Load("ReviewService.Application"));

// AutoMapper (si se usa)
// builder.Services.AddAutoMapper(Assembly.Load("ReviewService.Application"));

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
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

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

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ReviewDbContext>(tags: new[] { "ready", "external" });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ReviewService API",
        Version = "v1",
        Description = "API para gestión de reviews de vendedores/dealers en OKLA",
        Contact = new OpenApiContact
        {
            Name = "OKLA Development Team",
            Email = "dev@okla.com.do"
        }
    });

    // Incluir comentarios XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // JWT Bearer configuration
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // 4. HTTPS Redirection — only outside K8s
    if (!app.Environment.IsProduction())
        app.UseHttpsRedirection();

    // 5. Swagger — development only
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReviewService API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    // 5.5. Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // 6. CORS — before auth
    app.UseCors();

    // 7. Rate Limiting
    app.UseRateLimiter();

    // 8. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 9. Audit middleware — after auth (has userId context)
    app.UseAuditMiddleware();

    // 10. Endpoints
    app.MapControllers();

// Health Check endpoints
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
        var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
        try
        {
            context.Database.Migrate();
            Log.Information("Database migration completed for {ServiceName}", ServiceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration error for {ServiceName}. Will use EnsureCreated fallback.", ServiceName);
            try { context.Database.EnsureCreated(); }
            catch { /* Ignore — may already exist */ }
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
    Log.Fatal(ex, "Application {ServiceName} terminated unexpectedly", "ReviewService");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }