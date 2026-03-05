using CarDealer.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using RateLimitingService.Api.Middleware;
using RateLimitingService.Core.Interfaces;
using RateLimitingService.Core.Models;
using RateLimitingService.Core.Services;
using RateLimitingService.Infrastructure.Data;
using RateLimitingService.Infrastructure.Repositories;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "RateLimitingService")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Rate Limiting Service API",
        Version = "v1",
        Description = "Distributed rate limiting service with sliding window algorithm"
    });
});

// Configure Rate Limiting
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection(RateLimitOptions.SectionName));

// Configure Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnection);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 3;
    configuration.ConnectTimeout = 5000;
    return ConnectionMultiplexer.Connect(configuration);
});

// Configure PostgreSQL
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") ??
    "Host=localhost;Port=5432;Database=ratelimiting;Username=postgres;Password=postgres";
builder.Services.AddDbContext<RateLimitDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// Register storage
builder.Services.AddSingleton<IRateLimitStorage, RedisRateLimitStorage>();

// Register repositories
builder.Services.AddScoped<IRateLimitViolationRepository, RateLimitViolationRepository>();

// Register algorithms
builder.Services.AddSingleton<TokenBucketRateLimiter>();
builder.Services.AddSingleton<SlidingWindowRateLimiter>();
builder.Services.AddSingleton<FixedWindowRateLimiter>();
builder.Services.AddSingleton<LeakyBucketRateLimiter>();

// Register services
builder.Services.AddSingleton<IRateLimitRuleService, RateLimitRuleService>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection, name: "redis", tags: new[] { "db", "redis" })
    .AddNpgSql(postgresConnection, name: "postgresql", tags: new[] { "db", "postgresql" });

// Configure CORS
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

var app = builder.Build();

// Configure the HTTP request pipeline
// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rate Limiting Service API v1");
    });
}

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

// Apply rate limiting middleware
app.UseRateLimiting();

app.MapControllers();

app.MapHealthChecks("/health");

// Log startup
Log.Information("RateLimitingService starting on port {Port}",
    builder.Configuration.GetValue<int>("Port", 15097));

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
