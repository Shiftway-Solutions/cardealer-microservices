using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Serilog;
using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;
using Gateway.Api.Middleware;
using Gateway.Domain.Interfaces;
using Gateway.Infrastructure.Services;
using Gateway.Application.UseCases;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Middleware;
using CarDealer.Shared.RateLimiting.Extensions;
using CarDealer.Shared.RateLimiting.Models;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using CarDealer.Shared.Audit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// FASE 2: OBSERVABILITY - Logging centralizado con Serilog + Seq
// ============================================================================
builder.UseStandardSerilog("Gateway", options =>
{
    options.SeqEnabled = true;
    options.SeqServerUrl = builder.Configuration["Logging:Seq:ServerUrl"] ?? "http://seq:5341";
    options.FileEnabled = builder.Configuration.GetValue<bool>("Logging:File:Enabled", false);
    options.FilePath = builder.Configuration["Logging:File:Path"] ?? "logs/gateway-.log";
    options.RabbitMQEnabled = false; // Gateway uses direct Seq, not RabbitMQ for logs
});

// 1. Determinar entorno
var isDevelopment = builder.Environment.IsDevelopment();
var configFile = isDevelopment ? "ocelot.dev.json" : "ocelot.prod.json";
//var configFile = isDevelopment ? "ocelot.dev.json" : "ocelot.dev.json";

// 2. Cargar configuración
builder.Configuration.AddJsonFile(configFile, optional: false, reloadOnChange: true);

// 3. Configuración esencial
builder.Services.AddResponseCompression();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// Clean Architecture - Domain Services
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

// Clean Architecture - Application Use Cases
builder.Services.AddScoped<CheckRouteExistsUseCase>();
builder.Services.AddScoped<ResolveDownstreamPathUseCase>();
builder.Services.AddScoped<CheckServiceHealthUseCase>();
builder.Services.AddScoped<GetServicesHealthUseCase>();
builder.Services.AddScoped<RecordRequestMetricsUseCase>();
builder.Services.AddScoped<RecordDownstreamCallMetricsUseCase>();

// ============================================================================
// FASE 2: OBSERVABILITY - OpenTelemetry Tracing + Metrics
// ============================================================================
builder.Services.AddStandardObservability("Gateway", options =>
{
    options.TracingEnabled = true;
    options.MetricsEnabled = true;
    options.OtlpEndpoint = builder.Configuration["Observability:Otlp:Endpoint"] ?? "http://jaeger:4317";
    options.SamplingRatio = builder.Configuration.GetValue<double>("Observability:SamplingRatio", 0.1);
    options.PrometheusEnabled = builder.Configuration.GetValue<bool>("Observability:Prometheus:Enabled", true);
    options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger" };
});

// ============================================================================
// FASE 2: OBSERVABILITY - Error Handling centralizado
// ============================================================================
builder.Services.AddStandardErrorHandling(options =>
{
    options.ServiceName = "Gateway";
    options.Environment = builder.Environment.EnvironmentName;
    options.PublishToErrorService = builder.Configuration.GetValue<bool>("ErrorHandling:PublishToErrorService", true);
    options.RabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ:HostName"] ?? "rabbitmq";
    options.RabbitMQPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
    options.RabbitMQUser = builder.Configuration["RabbitMQ:UserName"] ?? builder.Configuration["RabbitMQ:User"] ?? "guest";
    options.RabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    options.IncludeStackTrace = builder.Environment.IsDevelopment();

    // Propagate convenience properties to nested RabbitMQ options used by RabbitMQErrorPublisher
    options.RabbitMQ.Hostname = options.RabbitMQHost;
    options.RabbitMQ.Port = options.RabbitMQPort;
    options.RabbitMQ.Username = options.RabbitMQUser;
    options.RabbitMQ.Password = options.RabbitMQPassword;
});

// 4. CORS configurado para múltiples frontends
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        if (isDevelopment)
        {
            // Permitir múltiples puertos de desarrollo
            policy.WithOrigins(
                    "http://localhost:5173",    // Vite default
                    "http://localhost:5174",    // Frontend original
                    "http://localhost:3000",    // React alternative
                    "http://localhost:4200",    // Angular (si aplica)
                    "http://localhost:8080"     // Frontend Docker
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Producción — URLs autorizadas de OKLA
            policy.WithOrigins(
                    "https://okla.com.do",
                    "https://www.okla.com.do",
                    "https://inelcasrl.com.do",
                    "https://www.inelcasrl.com.do",
                    "https://cardealer.app",
                    "https://www.cardealer.app"
                  )
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With", "X-CSRF-Token", "X-Correlation-Id")
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromHours(1));
        }
    });
});

// 4.1 JWT Authentication from externalized secrets
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

        // Security (CWE-922): Support reading JWT from HttpOnly cookie
        // if no Authorization header is present (new default auth flow).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // If Authorization header is already present, use it (backward compatibility)
                if (context.Request.Headers.ContainsKey("Authorization"))
                    return Task.CompletedTask;

                // Otherwise, read from HttpOnly cookie
                var accessToken = context.Request.Cookies["okla_access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

    Log.Information("JWT Authentication configured successfully for Gateway");
}
catch (InvalidOperationException ex)
{
    Log.Fatal("JWT Authentication FAILED to configure: {Message}. Gateway will NOT start without proper JWT configuration.", ex.Message);
    throw; // Fail fast — do NOT start Gateway without auth (NIST IA-5)
}

// 5. Configuración Ocelot con Polly
builder.Services
    .AddOcelot(builder.Configuration)
    .AddPolly();

// 6. Swagger para Ocelot
builder.Services.AddSwaggerForOcelot(builder.Configuration);

// 7. Configure Consul Client
var consulAddress = builder.Configuration["Consul:Address"] ?? "http://localhost:8500";
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(config =>
{
    config.Address = new Uri(consulAddress);
}));

// 8. Configure Service Discovery
builder.Services.AddScoped<IServiceRegistry, ConsulServiceRegistry>();
builder.Services.AddScoped<IServiceDiscovery, ConsulServiceDiscovery>();
builder.Services.AddScoped<IHealthChecker, HttpHealthChecker>();

// 9. Configure Rate Limiting
var rateLimitEnabled = builder.Configuration.GetValue<bool>("RateLimiting:Enabled", true);
if (rateLimitEnabled)
{
    builder.Services.AddRateLimiting(options =>
    {
        options.Enabled = true;
        options.RedisConnection = builder.Configuration["ConnectionStrings:Redis"]
            ?? builder.Configuration["Redis:Connection"]
            ?? $"{builder.Configuration["Cache:Redis:Host"] ?? "redis"}:{builder.Configuration["Cache:Redis:Port"] ?? "6379"},abortConnect=false";
        options.DefaultLimit = builder.Configuration.GetValue<int>("RateLimiting:DefaultLimit", 100);
        options.DefaultWindowSeconds = builder.Configuration.GetValue<int>("RateLimiting:DefaultWindowSeconds", 60);
        options.KeyPrefix = "gateway:ratelimit";
        options.ExcludedPaths = new List<string>
        {
            "/health",
            "/swagger",
            "/metrics",
            "/.well-known"
        };
        
        // Add default API policies
        options.AddDefaultApiPolicies();
        
        Log.Information("Rate Limiting configured with Redis: {RedisConnection}", options.RedisConnection);
    });
}
else
{
    Log.Warning("Rate Limiting is DISABLED. API is unprotected against abuse.");
}

// 10. Audit Publisher for security monitoring
builder.Services.AddAuditPublisher(builder.Configuration);

var app = builder.Build();

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================

// HTTPS Redirect — enforce HTTPS in production (OWASP)
if (!isDevelopment)
{
    app.UseHttpsRedirection();
}

// Performance: Enable response compression early in pipeline (saves 60-80% bandwidth)
app.UseResponseCompression();

// FASE 2: Global Error Handling - PRIMERO para capturar todas las excepciones
app.UseGlobalErrorHandling();

// Graceful Degradation — intercepta circuit breakers y timeouts para
// devolver respuestas degradadas (503 con Retry-After) en lugar de errores 500
app.UseGracefulDegradation();

// FASE 2: Request Logging con enrichment de TraceId, UserId, CorrelationId
app.UseRequestLogging();

// Security Headers (OWASP) — before CORS and routing
app.UseApiSecurityHeaders(isProduction: !isDevelopment);

// CORS debe ir primero antes de cualquier otro middleware de routing
app.UseCors("ReactPolicy");

// Rate Limiting middleware (before all other processing)
if (rateLimitEnabled)
{
    app.UseRateLimiting();
    Log.Information("Rate Limiting middleware enabled");
}

// Health check middleware BEFORE Ocelot to intercept /health requests
app.UseHealthCheckMiddleware();

// BFF Cookie → Bearer Authorization transformation
// The browser sends the JWT as an HttpOnly cookie (okla_access_token).
// Next.js rewrites forward this cookie to the Gateway, but downstream
// microservices expect `Authorization: Bearer {token}` — not a cookie.
// This middleware bridges the gap: if there is no Authorization header
// but the auth cookie is present, it is injected as a Bearer token so
// that ALL downstream services (UserService, VehiclesSaleService, etc.)
// can validate it with their standard [Authorize] middleware.
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        var accessToken = context.Request.Cookies["okla_access_token"];
        if (!string.IsNullOrEmpty(accessToken))
        {
            context.Request.Headers.Append("Authorization", $"Bearer {accessToken}");
        }
    }
    await next();
});

// Use routing for other endpoints
app.UseRouting();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// CSRF Validation — after auth, before Ocelot routing (validates Double Submit Cookie for POST/PUT/DELETE)
app.UseCsrfValidation();

// Audit Middleware — logs all gateway requests for security monitoring
app.UseAuditMiddleware();

// Swagger only in Development — NEVER expose in production (OWASP API8)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI();
}

// Service Discovery Registration
app.UseMiddleware<ServiceRegistrationMiddleware>();

// 10. Ocelot como último middleware
await app.UseOcelot();

app.Run();

// Make Program class accessible for testing
public partial class Program { }