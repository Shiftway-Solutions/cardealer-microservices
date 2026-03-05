using CarDealer.Shared.Middleware;
using Amazon.S3;
using BackgroundRemovalService.Application.DTOs;
using BackgroundRemovalService.Application.Features.Handlers;
using BackgroundRemovalService.Application.Interfaces;
using BackgroundRemovalService.Application.Validators;
using BackgroundRemovalService.Domain.Interfaces;
using BackgroundRemovalService.Infrastructure.Configuration;
using BackgroundRemovalService.Infrastructure.Persistence;
using BackgroundRemovalService.Infrastructure.Persistence.Repositories;
using BackgroundRemovalService.Infrastructure.Providers;
using BackgroundRemovalService.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// === Serilog ===
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// === Database ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=backgroundremovalservice;Username=postgres;Password=postgres";

builder.Services.AddDbContext<BackgroundRemovalDbContext>(options =>
    options.UseNpgsql(connectionString));

// === Repositories ===
builder.Services.AddScoped<IBackgroundRemovalJobRepository, BackgroundRemovalJobRepository>();
builder.Services.AddScoped<IProviderConfigurationRepository, ProviderConfigurationRepository>();
builder.Services.AddScoped<IUsageRecordRepository, UsageRecordRepository>();

// === Secrets (API Keys centralizados) ===
builder.Services.Configure<SecretsSettings>(
    builder.Configuration.GetSection(SecretsSettings.SectionName));

// === Provider Settings ===
// ClipDrop (DEFAULT)
builder.Services.Configure<ClipDropSettings>(
    builder.Configuration.GetSection(ClipDropSettings.SectionName));
// RemoveBg
builder.Services.Configure<RemoveBgSettings>(
    builder.Configuration.GetSection(RemoveBgSettings.SectionName));
// Photoroom
builder.Services.Configure<PhotoroomSettings>(
    builder.Configuration.GetSection(PhotoroomSettings.SectionName));
// Slazzer
builder.Services.Configure<SlazzerSettings>(
    builder.Configuration.GetSection(SlazzerSettings.SectionName));
// Storage
builder.Services.Configure<S3StorageSettings>(
    builder.Configuration.GetSection(S3StorageSettings.SectionName));

// === HTTP Clients for Providers ===
// ClipDrop (DEFAULT - registered first)
builder.Services.AddHttpClient<ClipDropProvider>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<RemoveBgProvider>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<PhotoroomProvider>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<SlazzerProvider>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<ImageStorageService>();

// === Register Providers (ClipDrop is first/default) ===
builder.Services.AddScoped<IBackgroundRemovalProvider, ClipDropProvider>();
builder.Services.AddScoped<IBackgroundRemovalProvider, RemoveBgProvider>();
builder.Services.AddScoped<IBackgroundRemovalProvider, PhotoroomProvider>();
builder.Services.AddScoped<IBackgroundRemovalProvider, SlazzerProvider>();

// === Provider Factory ===
builder.Services.AddScoped<IBackgroundRemovalProviderFactory, BackgroundRemovalProviderFactory>();

// === S3 Client (optional) ===
var s3Config = builder.Configuration.GetSection(S3StorageSettings.SectionName).Get<S3StorageSettings>();
var secretsConfig = builder.Configuration.GetSection(SecretsSettings.SectionName).Get<SecretsSettings>();
if (s3Config != null && !s3Config.UseLocalPath && secretsConfig != null && !string.IsNullOrEmpty(secretsConfig.S3.AccessKey))
{
    builder.Services.AddSingleton<IAmazonS3>(sp =>
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Config.Region)
        };
        return new AmazonS3Client(secretsConfig.S3.AccessKey, secretsConfig.S3.SecretKey, config);
    });
}
else
{
    // Registrar null de forma segura
    builder.Services.AddSingleton<IAmazonS3>(sp => null!);
}

// === Services ===
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IBackgroundRemovalOrchestrator, BackgroundRemovalOrchestrator>();

// === Validators ===
builder.Services.AddScoped<IValidator<CreateRemovalJobRequest>, CreateRemovalJobRequestValidator>();

// === MediatR ===
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateRemovalJobCommandHandler).Assembly);
});

// SecurityValidation — ensures FluentValidation validators (NoSqlInjection, NoXss) run in MediatR pipeline
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(BackgroundRemovalService.Application.Behaviors.ValidationBehavior<,>));

// === Controllers ===
builder.Services.AddControllers();

// === Swagger ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Background Removal Service API",
        Version = "v1",
        Description = "Microservicio escalable para remoción de fondos de imágenes. " +
                      "Soporta múltiples proveedores: Remove.bg, Photoroom, Slazzer, y más. " +
                      "Arquitectura basada en Strategy Pattern para fácil extensibilidad."
    });
    
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// === CORS ===
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

// === Health Checks ===
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// === Authentication (optional) ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// === Swagger ===
// OWASP Security Headers
app.UseApiSecurityHeaders(isProduction: !app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

// === Middleware ===
// Security (CWE-532): UseRequestLogging() from shared lib masks sensitive headers
app.UseRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// === Endpoints ===
app.MapControllers();
app.MapHealthChecks("/health");

// === Database Migration ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BackgroundRemovalDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Log.Information("Database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database migration failed, will try EnsureCreated");
        await db.Database.EnsureCreatedAsync();
    }
}

Log.Information("Background Removal Service started on {Url}", 
    builder.Configuration["Urls"] ?? "http://localhost:5080");

app.Run();
