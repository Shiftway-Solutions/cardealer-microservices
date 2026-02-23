using CarDealer.Shared.Middleware;
using DealerManagementService.Application.DTOs;
using DealerManagementService.Domain.Interfaces;
using DealerManagementService.Infrastructure.Persistence;
using DealerManagementService.Infrastructure.Persistence.Repositories;
using DealerManagementService.Infrastructure.Services.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using Serilog;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;

const string ServiceName = "DealerManagementService";
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

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dealer Management Service API",
        Version = "v1",
        Description = "API for managing dealer accounts, verification, and subscriptions"
    });
    
    // JWT Bearer authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
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
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=postgres_db;Port=5432;Database=dealermanagement_db;Username=postgres;Password=postgres";
builder.Services.AddDbContext<DealerDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IDealerRepository, DealerRepository>();
builder.Services.AddScoped<IDealerDocumentRepository, DealerDocumentRepository>();
builder.Services.AddScoped<IDealerLocationRepository, DealerLocationRepository>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DealerDto).Assembly));

// SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(DealerManagementService.Application.Behaviors.ValidationBehavior<,>));

// Background Services - Event Consumers
// Consumer for UserRegisteredEvent from AuthService - auto-creates dealer profiles
builder.Services.AddHostedService<UserRegisteredEventConsumer>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://okla.com.do",
                "https://www.okla.com.do")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "database");

    // ========== JWT AUTHENTICATION ==========
    // Key is provided via K8s secret jwt-secrets as Jwt__Key env var (maps to Jwt:Key).
    // NOTE: Do NOT add Jwt:SecretKey to appsettings.json — it would shadow the env var via ?? fallback.
    var jwtKey = builder.Configuration["Jwt:Key"]
              ?? throw new InvalidOperationException("JWT Key must be configured via Jwt__Key env var / K8s secret. Do NOT use hardcoded keys.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]
              ?? throw new InvalidOperationException("JWT Issuer must be configured via Jwt__Issuer env var / K8s secret.");
    var jwtAudience = builder.Configuration["Jwt:Audience"]
              ?? throw new InvalidOperationException("JWT Audience must be configured via Jwt__Audience env var / K8s secret.");

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
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

    var app = builder.Build();

    // ============= MIDDLEWARE =============
    // Global exception handling (first in pipeline)
    app.UseGlobalErrorHandling();

    // Audit middleware
    app.UseAuditMiddleware();

    // Configure the HTTP request pipeline
    // OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dealer Management Service API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseCors();

    // Serve uploaded documents (dealer verification files)
    var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });

    app.UseAuthentication();
    app.UseAuthorization();

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

    // Auto-create database schema on startup (all environments)
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
        try
        {
            dbContext.Database.EnsureCreated();
            Log.Information("Database created/verified for {ServiceName}", ServiceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating the database for {ServiceName}", ServiceName);
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
