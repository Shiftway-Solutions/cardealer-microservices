using CarDealer.Shared.Middleware;
using CarDealer.Shared.Encryption;
using Microsoft.AspNetCore.HttpOverrides;
using CarDealer.Shared.Messaging;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Services.Messaging;
using UserService.Shared.Extensions;
using UserService.Shared.Middleware;
using UserService.Shared.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CarDealer.Shared.Database;
using CarDealer.Shared.Secrets;
using CarDealer.Shared.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using FluentValidation;
using UserService.Application.Behaviors;
using MediatR;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using UserService.Api.Middleware;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using CarDealer.Shared.Audit.Extensions;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

const string ServiceName = "UserService";
const string ServiceVersion = "1.0.1";

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Add Secret Provider for externalized secrets
    builder.Services.AddSecretProvider();

    // ============================================================================
    // FASE 2: OBSERVABILITY - Logging centralizado con Serilog + Seq
    // ============================================================================
    builder.UseStandardSerilog("UserService", options =>
    {
        options.SeqEnabled = true;
        options.SeqServerUrl = builder.Configuration["Logging:Seq:ServerUrl"] ?? "http://seq:5341";
        options.FileEnabled = builder.Configuration.GetValue<bool>("Logging:File:Enabled", false);
        options.FilePath = builder.Configuration["Logging:File:Path"] ?? "logs/userservice-.log";
        options.RabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
        options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.RabbitMQUser = builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is not configured");
        options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
    });

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHealthChecks();

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "https://okla.com.do" };

            policy.WithOrigins(allowedOrigins)
                  // Security: Restrict to specific HTTP methods and headers (OWASP)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-CSRF-Token", "X-Requested-With", "X-Idempotency-Key")
                  .AllowCredentials();
        });
    });

    // Configurar Swagger con soporte JWT
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "UserService API",
            Version = "v1",
            Description = "API para gestión centralizada de errores en CarDealer Microservices"
        });

        // Configurar autenticación JWT en Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese el token JWT en el formato: Bearer {token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Configurar autenticación JWT with externalized secrets
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);
    var secretKey = jwtKey ?? throw new InvalidOperationException("JWT Key not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,              // SECURITY: never config-driven
            ValidateAudience = true,            // SECURITY: never config-driven
            ValidateLifetime = true,            // SECURITY: never config-driven
            ValidateIssuerSigningKey = true,    // SECURITY: never config-driven
            ValidIssuer = jwtIssuer ?? jwtSettings["Issuer"],
            ValidAudience = jwtAudience ?? jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero           // No tolerance — tokens expire exactly at exp claim
        };

        // Logging para debugging
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

    // Configurar políticas de autorización
    builder.Services.AddAuthorization(options =>
    {
        // Política para acceso general al UserService
        options.AddPolicy("UserServiceAccess", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("service", "UserService", "all");
        });

        // Política para operaciones administrativas
        options.AddPolicy("UserServiceAdmin", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("role", "admin", "UserService-admin");
        });

        // Política para solo lectura
        options.AddPolicy("UserServiceRead", policy =>
        {
            policy.RequireAuthenticatedUser();
        });
    });

    // Database Context (multi-provider configuration)
    builder.Services.AddDatabaseProvider<ApplicationDbContext>(builder.Configuration);

    // PII Encryption at rest — Ley 172-13 (AES-256-GCM)
    // Key: OKLA_PII_ENCRYPTION_KEY env var or Encryption:PiiKey config
    builder.Services.AddPiiEncryption(builder.Configuration);

    // Service Discovery Configuration
    builder.Services.AddSingleton<IConsulClient>(sp =>
    {
        var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
        return new ConsulClient(config => config.Address = new Uri(consulAddress));
    });

    builder.Services.AddScoped<IServiceRegistry, ConsulServiceRegistry>();
    builder.Services.AddScoped<IServiceDiscovery, ConsulServiceDiscovery>();
    builder.Services.AddHttpClient("HealthCheck");
    builder.Services.AddScoped<IHealthChecker, HttpHealthChecker>();

    // Application Services - Repositories
    builder.Services.AddScoped<IUserRepository, UserService.Infrastructure.Persistence.UserRepository>();
    builder.Services.AddScoped<IUserRoleRepository, UserService.Infrastructure.Persistence.UserRoleRepository>();
    builder.Services.AddScoped<IRoleRepository, UserService.Infrastructure.Persistence.EfRoleRepository>();
    builder.Services.AddScoped<IDealerRepository, UserService.Infrastructure.Persistence.DealerRepository>();
    builder.Services.AddScoped<ISellerProfileRepository, UserService.Infrastructure.Repositories.SellerProfileRepository>();
    builder.Services.AddScoped<ISellerConversionRepository, UserService.Infrastructure.Repositories.SellerConversionRepository>();
    builder.Services.AddScoped<IIdentityDocumentRepository, UserService.Infrastructure.Persistence.IdentityDocumentRepository>();
    builder.Services.AddScoped<IDealerEmployeeRepository, UserService.Infrastructure.Persistence.Repositories.DealerEmployeeRepository>();
    builder.Services.AddScoped<IDealerOnboardingRepository, UserService.Infrastructure.Persistence.Repositories.DealerOnboardingRepository>();
    builder.Services.AddScoped<IDealerModuleRepository, UserService.Infrastructure.Persistence.Repositories.DealerModuleRepository>();
    builder.Services.AddScoped<IModuleRepository, UserService.Infrastructure.Persistence.Repositories.ModuleRepository>();
    builder.Services.AddScoped<IUserOnboardingRepository, UserService.Infrastructure.Repositories.UserOnboardingRepository>();
    builder.Services.AddScoped<IPrivacyRequestRepository, UserService.Infrastructure.Persistence.PrivacyRequestRepository>();
    builder.Services.AddScoped<ICommunicationPreferenceRepository, UserService.Infrastructure.Persistence.CommunicationPreferenceRepository>();
    builder.Services.AddScoped<IConsentRecordRepository, UserService.Infrastructure.Persistence.ConsentRecordRepository>();
    builder.Services.AddScoped<IErrorReporter, UserService.Infrastructure.Services.ErrorReporter>();

    // Application Services - External clients
    builder.Services.AddHttpClient<UserService.Application.Interfaces.IRoleServiceClient, UserService.Infrastructure.External.RoleServiceClient>(client =>
    {
        var roleServiceUrl = builder.Configuration["ServiceUrls:RoleService"] ?? "https://localhost:45952";
        client.BaseAddress = new Uri(roleServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddStandardResilienceHandler(options =>
    {
        // Retry 3 times with exponential backoff
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        // Circuit breaker: open after 5 failures, stay open for 30s
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20); // Must be >= 2x AttemptTimeout
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.MinimumThroughput = 5;

        // Timeout of 10 seconds per attempt
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

        // Total timeout of 30 seconds
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddHttpClient<UserService.Application.Interfaces.IAuditServiceClient, UserService.Infrastructure.External.AuditServiceClient>(client =>
    {
        var auditServiceUrl = builder.Configuration["ServiceUrls:AuditService"] ?? "https://localhost:7287";
        client.BaseAddress = new Uri(auditServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient<UserService.Application.Interfaces.INotificationServiceClient, UserService.Infrastructure.External.NotificationServiceClient>(client =>
    {
        var notificationServiceUrl = builder.Configuration["ServiceUrls:NotificationService"] ?? "https://localhost:45954";
        client.BaseAddress = new Uri(notificationServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    builder.Services.AddHttpClient<UserService.Application.Interfaces.IErrorServiceClient, UserService.Infrastructure.External.ErrorServiceClient>(client =>
    {
        var errorServiceUrl = builder.Configuration["ServiceUrls:ErrorService"] ?? "https://localhost:45952";
        client.BaseAddress = new Uri(errorServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // VehiclesSaleService Client - Para obtener listings del vendedor
    builder.Services.AddHttpClient<UserService.Application.Interfaces.IVehiclesSaleServiceClient, UserService.Infrastructure.External.VehiclesSaleServiceClient>(client =>
    {
        var vehiclesServiceUrl = builder.Configuration["ServiceUrls:VehiclesSaleService"] ?? "http://vehiclessaleservice:8080";
        client.BaseAddress = new Uri(vehiclesServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // ReviewService Client - Para obtener reviews del vendedor
    builder.Services.AddHttpClient<UserService.Application.Interfaces.IReviewServiceClient, UserService.Infrastructure.External.ReviewServiceClient>(client =>
    {
        var reviewServiceUrl = builder.Configuration["ServiceUrls:ReviewService"] ?? "http://reviewservice:8080";
        client.BaseAddress = new Uri(reviewServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilience(builder.Configuration);

    // ReportsService Client - Para verificar reportes de fraude (Badge Verificado)
    builder.Services.AddHttpClient("ReportsService", client =>
    {
        var reportsServiceUrl = builder.Configuration["ServiceUrls:ReportsService"] ?? "http://reportsservice:8080";
        client.BaseAddress = new Uri(reportsServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // Dealer Badge Evaluator - Evalúa los 4 criterios del badge "Verificado OKLA"
    builder.Services.AddScoped<UserService.Application.Interfaces.IDealerBadgeEvaluator, UserService.Infrastructure.External.DealerBadgeEvaluator>();

    // Métricas personalizadas (Singleton para compartir estado)
    builder.Services.AddSingleton<UserService.Application.Metrics.UserServiceMetrics>();

    // Dead Letter Queue — PostgreSQL-backed (survives pod restarts during auto-scaling)
    // Shared RabbitMQ connection (1 connection per pod instead of N per class)
    builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration, "UserService");
    builder.Services.AddSingleton<UserService.Infrastructure.Messaging.IDeadLetterQueue, UserService.Infrastructure.Messaging.InMemoryDeadLetterQueue>();
    builder.Services.AddSharedRabbitMqConnection(builder.Configuration);

    // Event Publisher for RabbitMQ (con DLQ integrado)
    // NOTE: Using NoOpEventPublisher for development. Enable RabbitMQ in production.
    var useRabbitMq = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
    if (useRabbitMq)
    {
        builder.Services.AddSingleton<UserService.Infrastructure.Messaging.RabbitMqEventPublisher>();
        builder.Services.AddSingleton<IEventPublisher>(sp =>
            sp.GetRequiredService<UserService.Infrastructure.Messaging.RabbitMqEventPublisher>());
        builder.Services.AddHostedService<UserService.Infrastructure.Messaging.DeadLetterQueueProcessor>();

        // Consumer for UserRegisteredEvent from AuthService - syncs users automatically
        builder.Services.AddHostedService<UserService.Infrastructure.Services.Messaging.UserRegisteredEventConsumer>();

        // Consumer for UserLoggedInEvent from AuthService - updates LastLoginAt
        builder.Services.AddHostedService<UserService.Infrastructure.Services.Messaging.UserLoggedInEventConsumer>();

        // Consumer for KYCProfileStatusChangedEvent from KYCService - sets IsVerified on User
        builder.Services.AddHostedService<UserService.Infrastructure.Services.Messaging.KYCProfileApprovedEventConsumer>();

        // Daily job — anonymizes accounts after 15-day grace period (Ley 172-13)
        builder.Services.AddHostedService<UserService.Infrastructure.BackgroundJobs.AccountDeletionWorker>();
    }
    else
    {
        // Use NoOp publisher for development without RabbitMQ
        builder.Services.AddSingleton<IEventPublisher, UserService.Infrastructure.Messaging.NoOpEventPublisher>();
    }

    // Background worker for data export ZIP generation (Ley 172-13, Art. 5 — Portabilidad)
    // Registered outside RabbitMQ guard: only depends on DB repositories, not messaging.
    builder.Services.AddHostedService<UserService.Infrastructure.BackgroundJobs.DataExportWorker>();

    // Agregar MediatR
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(UserService.Application.UseCases.Users.CreateUser.CreateUserCommand).Assembly));

    // Registrar FluentValidation
    builder.Services.AddValidatorsFromAssembly(
        typeof(UserService.Application.UseCases.Sellers.ConvertBuyerToSeller.ConvertBuyerToSellerCommand).Assembly);

    // Agregar behavior de validación para MediatR
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // ============================================================================
    // FASE 2: OBSERVABILITY - OpenTelemetry Tracing + Metrics
    // ============================================================================
    builder.Services.AddStandardObservability("UserService", options =>
    {
        options.TracingEnabled = true;
        options.MetricsEnabled = true;
        options.OtlpEndpoint = builder.Configuration["Observability:Otlp:Endpoint"] ?? "http://jaeger:4317";
        options.SamplingRatio = builder.Configuration.GetValue<double>("Observability:SamplingRatio", builder.Environment.IsProduction() ? 0.1 : 1.0);
        options.PrometheusEnabled = builder.Configuration.GetValue<bool>("Observability:Prometheus:Enabled", true);
        options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger" };
    });

    // ============================================================================
    // FASE 2: OBSERVABILITY - Error Handling centralizado
    // ============================================================================
    builder.Services.AddStandardErrorHandling(options =>
    {
        options.ServiceName = "UserService";
        options.Environment = builder.Environment.EnvironmentName;
        options.PublishToErrorService = builder.Configuration.GetValue<bool>("ErrorHandling:PublishToErrorService", true);
        options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.RabbitMQUser = builder.Configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is not configured");
        options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured");
        options.IncludeStackTrace = builder.Environment.IsDevelopment();
    });

    // Configurar Rate Limiting
    var rateLimitingConfig = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingConfiguration>()
        ?? new RateLimitingConfiguration();
    builder.Services.AddCustomRateLimiting(rateLimitingConfig);

    // Audit Publisher — sends audit events to AuditService via RabbitMQ
    builder.Services.AddAuditPublisher(builder.Configuration);

    // Configurar RabbitMQ
    builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.Configure<UserServiceRabbitMQSettings>(builder.Configuration.GetSection("UserService"));

    // Registrar el consumidor RabbitMQ como hosted service
    // TEMPORARY: Commented out for testing without RabbitMQ
    // builder.Services.AddHostedService<RabbitMQErrorConsumer>();

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

    // ============================================================================
    // MIDDLEWARE PIPELINE
    // ============================================================================

    // FASE 2: Global Error Handling - PRIMERO para capturar todas las excepciones
    app.UseGlobalErrorHandling();

    // FASE 2: Request Logging con enrichment de TraceId, UserId, CorrelationId
    app.UseRequestLogging();

    // Swagger en desarrollo
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

    // Forwarded Headers — required for correct client IP behind K8s/LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // CORS middleware — BEFORE rate limiting (so OPTIONS preflight isn't blocked)
    app.UseCors();

    // Rate Limiting — after CORS, before auth
    app.UseMiddleware<RateLimitBypassMiddleware>();
    app.UseCustomRateLimiting(rateLimitingConfig);

    // Agregar autenticación y autorización
    app.UseAuthentication();
    app.UseAuthorization();

    // Service Discovery Auto-Registration (only if Consul is enabled)
    var consulEnabled = app.Configuration.GetValue<bool>("Consul:Enabled", false);
    if (consulEnabled)
    {
        app.UseMiddleware<ServiceRegistrationMiddleware>();
    }

    // Middleware para capturar respuestas (ResponseCaptureMiddleware local)
    app.UseMiddleware<ResponseCaptureMiddleware>();

    // NOTA: ErrorHandling local reemplazado por UseGlobalErrorHandling() de Fase 2

    // Audit middleware — after auth (has userId context)
    app.UseAuditMiddleware();

    app.MapControllers();

    // Health check endpoints - acceso anónimo (triple: liveness, readiness, startup)
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

    // Apply migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
            Log.Information("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying database migrations.");
        }
    }

    Log.Information("{ServiceName} v{ServiceVersion} starting up...", ServiceName, ServiceVersion);
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "{ServiceName} terminated unexpectedly", ServiceName);
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration testing
public partial class Program { }
