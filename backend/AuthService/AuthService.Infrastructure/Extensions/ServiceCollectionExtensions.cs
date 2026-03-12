using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Configuration;
using AuthService.Infrastructure.External;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Services.Identity;
using AuthService.Infrastructure.Services.Notification;
using AuthService.Infrastructure.Services.Security;
using AuthService.Shared;
using CarDealer.Shared.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Infrastructure.Services.ExternalAuth;
using AuthService.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using AuthService.Application.Features.ExternalAuth.Commands.ExternalAuth;
using Polly;
using Polly.Extensions.Http;
using AuthService.Application.Common.Interfaces;
using AuthService.Infrastructure.Services;
using AuthService.Infrastructure.Services.GeoLocation;

namespace AuthService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MemoryCache - Required for IGeoLocationService
        services.AddMemoryCache();

        // Secret Provider (reads from ENV vars and Docker secrets)
        services.AddSecretProvider();

        // Database Context - connection string from secrets has priority
        var connectionString = AuthSecretsConfiguration.GetDatabaseConnectionString(configuration);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Redis Configuration - from secrets
        var redisConnection = AuthSecretsConfiguration.GetRedisConnectionString(configuration);
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnection));
        }

        // Identity Configuration
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings from configuration
            var passwordPolicy = configuration.GetSection("Security:PasswordPolicy").Get<PasswordPolicySettings>();
            options.Password.RequiredLength = passwordPolicy?.RequiredLength ?? 8;
            options.Password.RequireDigit = passwordPolicy?.RequireDigit ?? true;
            options.Password.RequireLowercase = passwordPolicy?.RequireLowercase ?? true;
            options.Password.RequireUppercase = passwordPolicy?.RequireUppercase ?? true;
            options.Password.RequireNonAlphanumeric = passwordPolicy?.RequireNonAlphanumeric ?? true;

            // Lockout settings from configuration
            var lockoutPolicy = configuration.GetSection("Security:LockoutPolicy").Get<LockoutPolicySettings>();
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutPolicy?.DefaultLockoutMinutes ?? 30);
            options.Lockout.MaxFailedAccessAttempts = lockoutPolicy?.MaxFailedAccessAttempts ?? 5;
            options.Lockout.AllowedForNewUsers = lockoutPolicy?.Enabled ?? true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication - keys from secrets
        var (jwtKey, jwtIssuer, jwtAudience) = AuthSecretsConfiguration.GetJwtSettings(configuration);
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

        // Override with secrets
        jwtSettings.Key = jwtKey;
        jwtSettings.Issuer = jwtIssuer;
        jwtSettings.Audience = jwtAudience;

        services.AddAuthentication(options =>
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
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Security (CWE-922): Support reading JWT from HttpOnly cookie
                    if (!context.Request.Headers.ContainsKey("Authorization"))
                    {
                        var accessToken = context.Request.Cookies["okla_access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireVerifiedEmail", policy =>
                policy.RequireClaim("email_verified", "true"));
        });

        // Health Checks
        services.AddHealthChecks(configuration);

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
        services.AddScoped<ITrustedDeviceRepository, TrustedDeviceRepository>(); // US-18.4
        services.AddScoped<IUserSessionRepository, UserSessionRepository>(); // Sessions management
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>(); // Login history for security page

        // Services
        services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<object>, Microsoft.AspNetCore.Identity.PasswordHasher<object>>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<IAuthNotificationService, AuthNotificationService>();
        services.AddScoped<ITokenService, TokenService>();

        // Request Context (for IP/UserAgent tracking)
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestContext, HttpRequestContext>();

        // Servicios 2FA
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IQRCodeService, QRCodeService>();

        // US-18.3: CAPTCHA Service (Google reCAPTCHA v3)
        services.AddHttpClient<ICaptchaService, CaptchaService>()
            .AddPolicyHandler(GetRetryPolicy());

        // US-18.4: Device Fingerprinting Service
        services.AddScoped<IDeviceFingerprintService, DeviceFingerprintService>();

        // US-18.5: Security Audit Service for SIEM integration
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();

        // GeoLocation Service (ip-api.com for session location tracking)
        services.AddHttpClient<IGeoLocationService, IpApiGeoLocationService>()
            .AddPolicyHandler(GetRetryPolicy());

        // External Services - NotificationServiceClient
        services.AddHttpClient<NotificationServiceClient>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.Configure<NotificationServiceSettings>(configuration.GetSection("NotificationService"));

        // Register INotificationService interface (for SMS 2FA)
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationServiceClient>());

        // External Authentication Services
        services.AddHttpClient<ExternalTokenValidator>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.AddScoped<IExternalTokenValidator, ExternalTokenValidator>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();

        // Configuration - JwtSettings needs secrets merged in
        // Get base settings from config, then override with secrets
        services.Configure<JwtSettings>(options =>
        {
            var baseSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
            var (key, issuer, audience) = AuthSecretsConfiguration.GetJwtSettings(configuration);

            options.Key = key;
            options.Issuer = issuer;
            options.Audience = audience;
            options.ExpiresMinutes = baseSettings.ExpiresMinutes > 0 ? baseSettings.ExpiresMinutes : 60;
            options.RefreshTokenExpiresDays = baseSettings.RefreshTokenExpiresDays > 0 ? baseSettings.RefreshTokenExpiresDays : 7;
            options.ClockSkewMinutes = baseSettings.ClockSkewMinutes > 0 ? baseSettings.ClockSkewMinutes : 5;
        });
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));
        services.Configure<CacheSettings>(configuration.GetSection("Cache"));
        services.Configure<RateLimitSettings>(configuration.GetSection("Security:RateLimit"));

        // Caching - using redis connection from secrets
        var cacheSettings = configuration.GetSection("Cache").Get<CacheSettings>();
        if (cacheSettings?.EnableDistributedCache == true && !string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "AuthService_";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ExternalAuthCommandHandler).Assembly));

        return services;
    }

    // NUEVO: Método para configurar Health Checks
    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application", HealthStatus.Unhealthy, new[] { "app", "live", "ready" });

        // Redis Health Check
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecksBuilder.AddCheck<RedisHealthCheck>("redis", HealthStatus.Unhealthy, new[] { "cache", "redis", "ready" });
        }

        // External Services Health Check (NOT part of readiness — should not block pod from receiving traffic)
        var errorServiceUrl = configuration["ErrorService:BaseUrl"];
        var notificationServiceUrl = configuration["NotificationService:BaseUrl"];
        if (!string.IsNullOrEmpty(errorServiceUrl) || !string.IsNullOrEmpty(notificationServiceUrl))
        {
            services.AddHttpClient<ExternalServiceHealthCheck>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3); // Fast-fail for health checks
            });
            healthChecksBuilder.AddCheck<ExternalServiceHealthCheck>("external-services", HealthStatus.Degraded, new[] { "external" });
        }

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
