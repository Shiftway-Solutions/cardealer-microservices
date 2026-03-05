using CarDealer.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using ReviewService.Infrastructure.Persistence;
using ReviewService.Domain.Interfaces;
using ReviewService.Infrastructure.Persistence.Repositories;
using FluentValidation;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

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

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = jwtSettings["SecretKey"];

if (!string.IsNullOrEmpty(secretKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddAuthorization();

// CORS
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

var app = builder.Build();

// Configure the HTTP request pipeline.
// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReviewService API v1");
        c.RoutePrefix = string.Empty; // Swagger UI en la raíz
    });
}

// Middlewares
// Security (CWE-532): Configure Serilog request logging to avoid logging sensitive headers
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
        // DO NOT log Authorization, Cookie, or other sensitive headers
    };
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

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

// Root endpoint
app.MapGet("/", () => new
{
    service = "ReviewService",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow,
    endpoints = new[]
    {
        "GET /health - Health check",
        "GET /swagger - API documentation",
        "GET /api/reviews/seller/{sellerId} - Get seller reviews",
        "GET /api/reviews/seller/{sellerId}/summary - Get review statistics",
        "POST /api/reviews - Create review (auth required)",
        "PUT /api/reviews/{reviewId} - Update review (auth required)",
        "DELETE /api/reviews/{reviewId} - Delete review (auth required)"
    }
});

// Apply database migrations on startup (all environments)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Log.Information("ReviewService database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "ReviewService migration failed — continuing startup");
    }
}

Log.Information("ReviewService starting...");

app.Run();