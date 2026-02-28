using CarDealer.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OpenApi.Models;
using FluentValidation;
using KYCService.Infrastructure.Persistence;
using KYCService.Infrastructure.Repositories;
using KYCService.Infrastructure;
using KYCService.Domain.Interfaces;
using KYCService.Application.Validators;
using KYCService.Application.Services;
using KYCService.Application.Clients;
using KYCService.Infrastructure.Messaging;
using KYCService.Api.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CarDealer.Shared.Configuration;
using KYCService.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "KYC Service API", 
        Version = "v1",
        Description = "API para gestión de KYC según Ley 155-17 de Prevención de Lavado de Activos"
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

// Database
builder.Services.AddDbContext<KYCDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(KYCService.Application.Handlers.CreateKYCProfileHandler).Assembly));

// SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(KYCService.Application.Behaviors.ValidationBehavior<,>));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateKYCProfileValidator>();

// Repositories
builder.Services.AddScoped<IKYCProfileRepository, KYCProfileRepository>();
builder.Services.AddScoped<IKYCDocumentRepository, KYCDocumentRepository>();
builder.Services.AddScoped<IKYCVerificationRepository, KYCVerificationRepository>();
builder.Services.AddScoped<IKYCRiskAssessmentRepository, KYCRiskAssessmentRepository>();
builder.Services.AddScoped<ISuspiciousTransactionReportRepository, SuspiciousTransactionReportRepository>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();

// Security Repositories (Rate Limiting, Saga) - local storage for performance
builder.Services.AddScoped<IRateLimitRepository, RateLimitRepository>();
builder.Services.AddScoped<IKYCSagaRepository, KYCSagaRepository>();

// Draft Repository (KYC wizard autosave)
builder.Services.AddScoped<IKYCProfileDraftRepository, KYCProfileDraftRepository>();

// ==========================================================================
// External Microservice Clients (AuditService, IdempotencyService)
// ==========================================================================

// AuditService Client - Centralized audit logging
var auditServiceUrl = builder.Configuration["Services:AuditService:Url"] ?? "http://auditservice:8080";
builder.Services.AddHttpClient<IAuditServiceClient, AuditServiceClient>(client =>
{
    client.BaseAddress = new Uri(auditServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5); // Short timeout to not block main operations
    client.DefaultRequestHeaders.Add("X-Service-Name", "KYCService");
});

// IdempotencyService Client - Centralized idempotency management
var idempotencyServiceUrl = builder.Configuration["Services:IdempotencyService:Url"] ?? "http://idempotencyservice:8080";
builder.Services.AddHttpClient<IIdempotencyServiceClient, IdempotencyServiceClient>(client =>
{
    client.BaseAddress = new Uri(idempotencyServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(3); // Very short timeout - fallback if unavailable
    client.DefaultRequestHeaders.Add("X-Service-Name", "KYCService");
});

// MediaService Client - For generating fresh pre-signed URLs
// Default URL for Docker: mediaservice:8080 (container-to-container, matches K8s)
var mediaServiceUrl = builder.Configuration["Services:MediaService:BaseUrl"] ?? "http://mediaservice:8080";
builder.Services.AddHttpClient<IMediaServiceClient, MediaServiceClient>(client =>
{
    client.BaseAddress = new Uri(mediaServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("X-Service-Name", "KYCService");
});

// KYC Event Publisher — publishes RabbitMQ events consumed by NotificationService
builder.Services.AddSingleton<IKYCEventPublisher, KYCEventPublisher>();

// Saga Orchestrator (uses local DB for saga state)
builder.Services.AddScoped<IKYCSagaOrchestrator, KYCSagaOrchestrator>();
builder.Services.AddHttpContextAccessor(); // Required for middleware

// External Services (JCE, OCR, Face Comparison)
if (builder.Environment.IsDevelopment())
{
    // En desarrollo, usar simulación
    builder.Services.AddKYCInfrastructureDevelopment(builder.Configuration);
}
else
{
    // En producción, usar servicios reales
    builder.Services.AddKYCInfrastructureProduction(builder.Configuration);
}

// ==========================================================================
// ConfigurationService Client (dynamic config from admin panel)
// ==========================================================================
builder.Services.AddConfigurationServiceClient(builder.Configuration, "KYCService");

// Register KYCConfigurationService for dynamic config access from handlers
builder.Services.AddScoped<IKYCConfigurationService, KYCConfigurationService>();

// Register IdentityVerificationConfig from ConfigurationService (loaded dynamically)
// This overrides the default values with admin-panel settings
builder.Services.AddOptions<IdentityVerificationConfig>()
    .Configure<IConfigurationServiceClient>((config, configClient) =>
    {
        // Load KYC config values from ConfigurationService (with defaults)
        config.MaxAttempts = configClient.GetIntAsync("kyc.max_verification_attempts", 3).GetAwaiter().GetResult();
        config.SessionTimeoutMinutes = configClient.GetIntAsync("kyc.verification_timeout_minutes", 30).GetAwaiter().GetResult();
        
        config.FaceMatch.MinimumScore = configClient.GetIntAsync("kyc.face_match_threshold", 80).GetAwaiter().GetResult();
        config.FaceMatch.HighConfidenceScore = configClient.GetIntAsync("kyc.high_confidence_threshold", 95).GetAwaiter().GetResult();
        
        config.Liveness.Enabled = configClient.IsEnabledAsync("kyc.require_liveness_check", defaultValue: true).GetAwaiter().GetResult();
        
        config.Document.ExpirationDays = configClient.GetIntAsync("kyc.document_expiration_days", 365).GetAwaiter().GetResult();
    });

// JWT Authentication
// JWT Authentication - Use MicroserviceSecretsConfiguration for secure key management
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
            ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
        };
    });

// Authorization policies based on account_type claim
// AccountType enum: Guest=0, Individual=1, Dealer=2, DealerEmployee=3, Admin=4, PlatformEmployee=5
builder.Services.AddAuthorization(options =>
{
    // Admin policy: account_type = 4 (Admin)
    options.AddPolicy("Admin", policy => 
        policy.RequireClaim("account_type", "4"));
    
    // Compliance policy: account_type = 4 (Admin) or 5 (PlatformEmployee)
    options.AddPolicy("Compliance", policy => 
        policy.RequireClaim("account_type", "4", "5"));
    
    // AdminOrCompliance: Either Admin or Compliance (used in controllers)
    options.AddPolicy("AdminOrCompliance", policy => 
        policy.RequireClaim("account_type", "4", "5"));
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "", tags: new[] { "ready", "external" })
    .AddDbContextCheck<KYCDbContext>(tags: new[] { "ready", "external" });

// CORS - Restricted to known origins (defense-in-depth)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://okla.com.do", "https://www.okla.com.do", "https://api.okla.com.do" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Error Service Client for centralized error logging
builder.Services.AddHttpClient<KYCService.Domain.Interfaces.IErrorServiceClient, KYCService.Infrastructure.External.ErrorServiceClient>(client =>
{
    var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"] ?? "http://errorservice:8080";
    client.BaseAddress = new Uri(errorServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// ============================================================================
// SECURITY: Middleware pipeline (order matters!)
// ============================================================================

// 1. Global exception handler - must be first to catch all exceptions
app.UseKYCExceptionHandler();

// 2. Rate limiting - before authentication to block abusive requests early
app.UseKYCRateLimit();

// Configure the HTTP request pipeline
// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// SECURITY: Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 3. Idempotency - after auth so we have user context
app.UseIdempotency();

app.MapControllers();
app.MapHealthChecks("/health");

// Apply migrations on startup (all environments)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<KYCDbContext>();
    try
    {
        dbContext.Database.Migrate();
        logger.LogInformation("KYCService database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "KYCService migration failed, trying EnsureCreated");
        try
        {
            dbContext.Database.EnsureCreated();
        }
        catch (Exception ex2)
        {
            logger.LogError(ex2, "KYCService DB init failed — continuing startup");
        }
    }
}

app.Run();
