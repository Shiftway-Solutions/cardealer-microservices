using AdminService.Api.Handlers;
using AdminService.Application.Interfaces;
using AdminService.Application.Services;
using AdminService.Infrastructure.External;
using AdminService.Infrastructure.Services;
using AdminService.Infrastructure.Persistence;
using AdminService.Domain.Interfaces;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using AdminService.Api.Middleware;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Audit.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "AdminService";
const string ServiceVersion = "1.0.1"; // fix: valid appsettings.Production.json

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    // ============= CENTRALIZED LOGGING (Serilog → Seq) =============
    builder.UseStandardSerilog(ServiceName);

    // ============= OBSERVABILITY (OpenTelemetry → Jaeger) =============
    builder.Services.AddStandardObservability(configuration, ServiceName, ServiceVersion);

    // ============= ERROR HANDLING (→ ErrorService) =============
    builder.Services.AddStandardErrorHandling(configuration, ServiceName);

    // ============= AUDIT (→ AuditService via RabbitMQ) =============
    builder.Services.AddAuditPublisher(configuration);

    // ============= CORS =============
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? (builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000" }
            : new[] { "https://okla.com.do" });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  // Security: Restrict to specific HTTP methods and headers (OWASP)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

    // ============= HEALTH CHECKS =============
    builder.Services.AddHealthChecks();

    // ============= IN-MEMORY CACHE (for dashboard snapshots & KPI deltas) =============
    builder.Services.AddMemoryCache();

    // Add secret provider for secure configuration
    builder.Services.AddSecretProvider();

    // ========== JWT AUTHENTICATION (from centralized secrets, NOT hardcoded) ==========
    try
    {
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
                ClockSkew = TimeSpan.Zero // No tolerance — tokens expire exactly at exp claim
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Debug("JWT Token validated for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                }
            };
        });

        Log.Information("JWT Authentication configured successfully for {ServiceName}", ServiceName);
    }
    catch (InvalidOperationException ex)
    {
        Log.Fatal("JWT Authentication FAILED to configure for {ServiceName}: {Message}. Service will NOT start without proper JWT configuration.", ServiceName, ex.Message);
        throw; // Fail fast — do NOT start without auth (NIST IA-5)
    }

    // ============= RATE LIMITING (defense-in-depth) =============
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Fixed window: 100 requests per minute per user/IP
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
        });

        // Strict policy for sensitive admin operations (approve, reject, delete)
        options.AddFixedWindowLimiter("strict", opt =>
        {
            opt.PermitLimit = 20;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            Log.Warning("Rate limit exceeded for {RemoteIp} on {Path}",
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.Request.Path);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.", cancellationToken);
        };
    });

    // Add services to the container.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
        typeof(AdminService.Application.UseCases.Vehicles.ApproveVehicle.ApproveVehicleCommand).Assembly));

    // Register FluentValidation validators
    builder.Services.AddValidatorsFromAssembly(
        typeof(AdminService.Application.Validators.SecurityValidators).Assembly);

    // ValidationBehavior — ensures FluentValidation validators (NoSqlInjection, NoXss) run automatically in MediatR pipeline
    builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AdminService.Application.Behaviors.ValidationBehavior<,>));

    // Register Repositories
    builder.Services.AddScoped<IStatisticsRepository, EfStatisticsRepository>();
    builder.Services.AddScoped<IAdminActionLogRepository, EfAdminActionLogRepository>();
    builder.Services.AddScoped<IModerationRepository, EfModerationRepository>();
    builder.Services.AddScoped<IPlatformEmployeeRepository, EfPlatformEmployeeRepository>();
    builder.Services.AddScoped<IAdminUserRepository, EfAdminUserRepository>();
    // Banner repository — singleton so in-memory data survives across requests in the same pod
    builder.Services.AddSingleton<AdminService.Domain.Interfaces.IBannerRepository,
        AdminService.Infrastructure.Persistence.InMemoryBannerRepository>();
    // Advertising service — singleton in-memory (same pattern as banner repository)
    builder.Services.AddSingleton<AdminService.Application.Interfaces.IAdvertisingService,
        AdminService.Infrastructure.Services.InMemoryAdvertisingService>();

    // Reports Service Client (for admin content moderation reports → ReportsService)
    builder.Services.AddHttpClient<IReportsServiceClient, ReportsServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:ReportsService"] ?? "http://reportsservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    // Service Discovery Configuration - with fallback to NoOp when Consul is disabled
    var consulEnabled = builder.Configuration.GetValue<bool>("Consul:Enabled", false);
    if (consulEnabled)
    {
        builder.Services.AddSingleton<IConsulClient>(sp =>
        {
            var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
            return new ConsulClient(config => config.Address = new Uri(consulAddress));
        });

        builder.Services.AddScoped<IServiceRegistry, ConsulServiceRegistry>();
        builder.Services.AddScoped<IServiceDiscovery, ConsulServiceDiscovery>();
        builder.Services.AddHttpClient("HealthCheck");
        builder.Services.AddScoped<IHealthChecker, HttpHealthChecker>();
    }
    else
    {
        // Use NoOp implementations when Consul is disabled
        builder.Services.AddScoped<IServiceRegistry, NoOpServiceRegistry>();
        builder.Services.AddScoped<IServiceDiscovery, NoOpServiceDiscovery>();
    }

    // Configure HttpClients for external services
    builder.Services.AddHttpClient<IAuditServiceClient, AuditServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:AuditService"] ?? "https://localhost:7287";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:NotificationService"] ?? "https://localhost:45954";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    builder.Services.AddHttpClient<IErrorServiceClient, ErrorServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:ErrorService"] ?? "https://localhost:45952";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    // Platform User Service (for admin user management)
    builder.Services.AddHttpClient<IPlatformUserService, PlatformUserService>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:UserService"] ?? "http://userservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    // Auth Service Client (for admin user creation and security operations)
    builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:AuthService"] ?? "http://authservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    // Vehicle Service Client (for admin vehicle management → VehiclesSaleService)
    builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:VehiclesSaleService"] ?? "http://vehiclessaleservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    // Dealer Service Client (for admin dealer management → UserService)
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddTransient<AuthHeaderForwardingHandler>();
    builder.Services.AddHttpClient<IDealerService, DealerService>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:DealerManagementService"] ?? "http://userservice:80";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddHttpMessageHandler<AuthHeaderForwardingHandler>()
    .AddStandardResilience(configuration);

    // Review Service Client (for admin review management → ReviewService)
    builder.Services.AddHttpClient<IReviewServiceClient, ReviewServiceClient>(client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:ReviewService"] ?? "http://reviewservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

    // Financial Data Provider — CONTRA #5 FIX: Unified financial dashboard
    // Aggregates costs from Gateway (LLM), BillingService (marketing), and config (infra/dev)
    builder.Services.AddScoped<IFinancialDataProvider, FinancialDataProvider>();

    // Revenue Projection + Threshold Alert — CONTRA #7 FIX
    // Monitors projected monthly revenue against OPEX $2,215 threshold
    builder.Services.AddScoped<IRevenueProjectionService, RevenueProjectionService>();

    // Infrastructure Cost Monitor — CONTRA #8 FIX
    // Monitors projected DigitalOcean costs against $210 monthly budget
    builder.Services.AddScoped<IInfrastructureCostMonitorService, InfrastructureCostMonitorService>();

    // RabbitMQ Event Publisher (for revenue threshold alerts → NotificationService)
    var rabbitMqEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
    if (rabbitMqEnabled)
        builder.Services.AddSingleton<AdminService.Domain.Interfaces.IEventPublisher,
            AdminService.Infrastructure.Messaging.RabbitMqEventPublisher>();
    else
        builder.Services.AddSingleton<AdminService.Domain.Interfaces.IEventPublisher,
            AdminService.Infrastructure.Messaging.NoOpEventPublisher>();

    // Revenue Threshold Alert Background Job — runs every 6h
    builder.Services.AddHostedService<AdminService.Api.Workers.RevenueThresholdAlertJob>();

    // Infrastructure Cost Alert Background Job — CONTRA #8, runs every 4h
    builder.Services.AddHostedService<AdminService.Api.Workers.InfrastructureCostAlertJob>();

    // Named HTTP clients for financial data aggregation
    builder.Services.AddHttpClient("Gateway", client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:Gateway"] ?? "http://gateway:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);
    builder.Services.AddHttpClient("BillingService", client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:BillingService"] ?? "http://billingservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);
    builder.Services.AddHttpClient("ContactService", client =>
    {
        var baseAddress = builder.Configuration["ServiceUrls:ContactService"] ?? "http://contactservice:8080";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(configuration);

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

    // 3.5. Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // 4. CORS — before routing
    app.UseCors();

    // 5. Request Logging
    app.UseRequestLogging();

    // 6. HTTPS Redirection — only outside K8s (TLS terminates at Ingress in production)
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // 7. Swagger — development only
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 8. Rate Limiting (defense-in-depth)
    app.UseRateLimiter();

    // 9. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 10. Audit middleware — after auth (has userId context)
    app.UseAuditMiddleware();

    // 11. Service Discovery Auto-Registration
    app.UseMiddleware<ServiceRegistrationMiddleware>();

    // 12. Endpoints
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
