using CarDealer.Shared.Middleware;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using FluentValidation;
using MediatR;
using ChatbotService.Application;
using ChatbotService.Infrastructure;
using ChatbotService.Infrastructure.Persistence;
using ChatbotService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ChatbotService")
    .WriteTo.Console()
    .WriteTo.File("logs/chatbotservice-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatbotService API",
        Version = "v1",
        Description = "API for Chatbot with LLM integration - OKLA Marketplace",
        Contact = new OpenApiContact
        {
            Name = "OKLA Team",
            Email = "dev@okla.com.do"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
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

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy: Chat messages — max 20 requests per minute per IP
    options.AddPolicy("ChatMessage", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    // Policy: Session start — max 5 per minute per IP (prevent session flooding)
    options.AddPolicy("SessionStart", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Global fallback — 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// Add Hosted Services for maintenance tasks
if (builder.Configuration.GetValue<bool>("Maintenance:EnableAutomatedTasks"))
{
    builder.Services.AddHostedService<MaintenanceWorkerService>();
}

// Redis Distributed Cache — required by LlmResponseCacheService (IDistributedCache)
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "redis:6379";
try
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "ChatbotService:";
    });
}
catch
{
    // Fallback to in-memory distributed cache if Redis is not available
    builder.Services.AddDistributedMemoryCache();
}

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "db", "sql", "postgresql" })
    .AddRedis(
        builder.Configuration["Redis:ConnectionString"] ?? "redis:6379",
        name: "redis",
        tags: new[] { "cache", "redis" });

var app = builder.Build();

// Configure the HTTP request pipeline
// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatbotService API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();

app.UseCors("AllowedOrigins");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// Apply migrations and seed on startup (all environments).
// The seeder is idempotent: it checks AnyAsync() before inserting.
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatbotDbContext>();
    try
    {
        // Always apply pending EF migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        else
        {
            // In case the schema was never created (new empty DB)
            await dbContext.Database.EnsureCreatedAsync();
            Log.Information("Database schema verified via EnsureCreated");
        }

        // Idempotent column additions — safe for existing production databases
        // (EnsureCreated does not alter existing tables; this handles schema evolution)
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE chatbot_configurations ADD COLUMN IF NOT EXISTS contact_email VARCHAR(255)");
        Log.Information("Schema evolution checks completed");

        // Seed default configurations (idempotent)
        await ChatbotDataSeeder.SeedAsync(dbContext);
        Log.Information("Database seed check completed");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply migrations, trying EnsureCreated...");
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            Log.Information("Database schema created via EnsureCreated (fallback)");

            await ChatbotDataSeeder.SeedAsync(dbContext);
            Log.Information("Database seed check completed (fallback path)");
        }
        catch (Exception ex2)
        {
            Log.Error(ex2, "Failed to create database schema");
        }
    }
}

Log.Information("ChatbotService starting on port 8080");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ChatbotService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
