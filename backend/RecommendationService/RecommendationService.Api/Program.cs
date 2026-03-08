using CarDealer.Shared.Middleware;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.Audit.Extensions;
using FluentValidation;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecommendationService.Application.Features.Recommendations.Commands;
using RecommendationService.Domain.Interfaces;
using RecommendationService.Infrastructure.Persistence;
using RecommendationService.Infrastructure.Persistence.Repositories;
using System.Text;
using Serilog;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

const string ServiceName = "RecommendationService";

try
{

var builder = WebApplication.CreateBuilder(args);

// ============= SECRETS PROVIDER =============
builder.Services.AddSecretProvider();

// Controllers
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<RecommendationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
builder.Services.AddScoped<IVehicleInteractionRepository, VehicleInteractionRepository>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GenerateRecommendationsCommand).Assembly));

// SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(RecommendationService.Application.Behaviors.ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(GenerateRecommendationsCommand).Assembly);

// JWT Authentication — uses shared MicroserviceSecretsConfiguration for consistent key resolution
var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://okla.com.do", "https://www.okla.com.do" };

        policy.WithOrigins(allowedOrigins)
              // Security: Restrict to specific HTTP methods and headers (OWASP)
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
              .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RecommendationService API",
        Version = "v1",
        Description = "API para sistema de recomendaciones de OKLA Marketplace"
    });

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

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured"),
        name: "postgresql",
        tags: new[] { "ready", "external" });

// Global Error Handling — RFC 7807 ProblemDetails + error publishing to ErrorService
builder.Services.AddGlobalErrorHandling(builder.Configuration);

// Audit Publisher — sends audit events to AuditService via RabbitMQ
builder.Services.AddAuditPublisher(builder.Configuration);

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

// Middleware
// 1. Global Error Handling — ALWAYS FIRST (catches all middleware + controller exceptions)
app.UseGlobalErrorHandling();

// 2. Request Logging — structured with TraceId, UserId, CorrelationId
app.UseRequestLogging();

// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

// Response Compression — early, after error handling
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();

// Forwarded Headers — required for correct client IP behind K8s/LB (OWASP)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors();

// Rate Limiting — after CORS, before auth (OWASP API4:2023)
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Audit middleware — after auth (has userId context)
app.UseAuditMiddleware();

app.MapControllers();

// Health Check Triple (K8s probes)
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
    Predicate = _ => false // Liveness: always return healthy
});

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

public partial class Program { }
