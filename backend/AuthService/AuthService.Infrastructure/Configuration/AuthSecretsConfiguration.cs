using CarDealer.Shared.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Configuration;

/// <summary>
/// Extension methods para configurar AuthService desde secretos.
/// </summary>
public static class AuthSecretsConfiguration
{
    /// <summary>
    /// Obtiene la connection string de la base de datos desde secretos.
    /// Prioridad: ENV > Docker Secrets > appsettings.json
    /// </summary>
    public static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var secretProvider = CompositeSecretProvider.CreateDefault();

        // 1. Intentar obtener connection string completa
        var connectionString = secretProvider.GetSecret(SecretKeys.DatabaseConnectionString)
            ?? secretProvider.GetSecret("AUTHSERVICE_DATABASE_CONNECTION_STRING");

        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;

        // 2. Intentar construir desde variables individuales
        var host = secretProvider.GetSecret(SecretKeys.DatabaseHost);
        var password = secretProvider.GetSecret(SecretKeys.DatabasePassword);

        if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(password))
        {
            var port = secretProvider.GetSecret(SecretKeys.DatabasePort) ?? "5432";
            var database = secretProvider.GetSecret(SecretKeys.DatabaseName) ?? "authservice";
            var user = secretProvider.GetSecret(SecretKeys.DatabaseUser) ?? "postgres";

            return $"Host={host};Port={port};Database={database};Username={user};Password={password};Pooling=true;Minimum Pool Size=5;Maximum Pool Size=20";
        }

        // 3. Fallback a configuración tradicional
        return configuration.GetConnectionString("DefaultConnection")
            ?? configuration["Database:ConnectionStrings:PostgreSQL"]
            ?? throw new InvalidOperationException("No database connection string configured");
    }

    /// <summary>
    /// Obtiene la connection string de Redis desde secretos.
    /// </summary>
    public static string? GetRedisConnectionString(IConfiguration configuration)
    {
        var secretProvider = CompositeSecretProvider.CreateDefault();

        var connectionString = secretProvider.GetSecret(SecretKeys.RedisConnectionString);
        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;

        var host = secretProvider.GetSecret(SecretKeys.RedisHost);
        if (!string.IsNullOrEmpty(host))
        {
            var port = secretProvider.GetSecret(SecretKeys.RedisPort) ?? "6379";
            var password = secretProvider.GetSecret(SecretKeys.RedisPassword);

            var redisCs = $"{host}:{port}";
            if (!string.IsNullOrEmpty(password))
                redisCs += $",password={password}";
            return redisCs;
        }

        return configuration.GetConnectionString("Redis")
            ?? configuration["Cache:RedisConnectionString"];
    }

    /// <summary>
    /// Obtiene configuración de JWT desde secretos.
    /// </summary>
    public static (string Key, string Issuer, string Audience) GetJwtSettings(IConfiguration configuration)
    {
        var secretProvider = CompositeSecretProvider.CreateDefault();

        var key = secretProvider.GetSecret(SecretKeys.JwtSecretKey)
            ?? configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key not configured");

        var issuer = secretProvider.GetSecret(SecretKeys.JwtIssuer)
            ?? configuration["Jwt:Issuer"]
            ?? "AuthService";

        var audience = secretProvider.GetSecret(SecretKeys.JwtAudience)
            ?? configuration["Jwt:Audience"]
            ?? "AuthServiceClients";

        return (key, issuer, audience);
    }

    /// <summary>
    /// Obtiene configuración de OAuth desde secretos.
    /// </summary>
    public static OAuthCredentials GetOAuthCredentials(IConfiguration configuration)
    {
        var secretProvider = CompositeSecretProvider.CreateDefault();

        return new OAuthCredentials
        {
            GoogleClientId = secretProvider.GetSecret("GOOGLE_CLIENT_ID")
                ?? configuration["Authentication:Google:ClientId"],
            GoogleClientSecret = secretProvider.GetSecret("GOOGLE_CLIENT_SECRET")
                ?? configuration["Authentication:Google:ClientSecret"],

            MicrosoftClientId = secretProvider.GetSecret("MICROSOFT_CLIENT_ID")
                ?? configuration["Authentication:Microsoft:ClientId"],
            MicrosoftClientSecret = secretProvider.GetSecret("MICROSOFT_CLIENT_SECRET")
                ?? configuration["Authentication:Microsoft:ClientSecret"],
            MicrosoftTenantId = secretProvider.GetSecret("MICROSOFT_TENANT_ID")
                ?? configuration["Authentication:Microsoft:TenantId"] ?? "common",

            FacebookClientId = secretProvider.GetSecret("FACEBOOK_CLIENT_ID")
                ?? configuration["Authentication:Facebook:ClientId"],
            FacebookClientSecret = secretProvider.GetSecret("FACEBOOK_CLIENT_SECRET")
                ?? configuration["Authentication:Facebook:ClientSecret"]
        };
    }

    /// <summary>
    /// Obtiene configuración de RabbitMQ desde secretos.
    /// </summary>
    public static RabbitMqCredentials GetRabbitMqCredentials(IConfiguration configuration)
    {
        var secretProvider = CompositeSecretProvider.CreateDefault();

        return new RabbitMqCredentials
        {
            Host = secretProvider.GetSecret(SecretKeys.RabbitMqHost)
                ?? configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(secretProvider.GetSecret(SecretKeys.RabbitMqPort)
                ?? configuration["RabbitMQ:Port"] ?? "5672"),
            Username = secretProvider.GetSecret(SecretKeys.RabbitMqUser)
                ?? configuration["RabbitMQ:UserName"] ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured"),
            Password = secretProvider.GetSecret(SecretKeys.RabbitMqPassword)
                ?? configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
            VirtualHost = secretProvider.GetSecret(SecretKeys.RabbitMqVirtualHost)
                ?? configuration["RabbitMQ:VirtualHost"] ?? "/"
        };
    }

    /// <summary>
    /// Valida que los secretos requeridos estén configurados.
    /// </summary>
    public static void ValidateAuthSecrets(ILogger logger)
    {
        var secretProvider = CompositeSecretProvider.CreateDefault();

        // Database es requerido
        if (!secretProvider.HasSecret(SecretKeys.DatabaseConnectionString) &&
            !secretProvider.HasSecret(SecretKeys.DatabasePassword))
        {
            logger.LogWarning(
                "Database credentials not found in secrets. Using appsettings.json fallback. " +
                "In production, set {Key} environment variable.",
                SecretKeys.DatabaseConnectionString);
        }

        // JWT Key es crítico
        if (!secretProvider.HasSecret(SecretKeys.JwtSecretKey))
        {
            logger.LogWarning(
                "JWT secret key not found in secrets. Using appsettings.json fallback. " +
                "⚠️ CRITICAL: In production, ALWAYS set {Key} environment variable!",
                SecretKeys.JwtSecretKey);
        }
    }
}

/// <summary>
/// Credenciales OAuth para autenticación externa.
/// </summary>
public class OAuthCredentials
{
    // Google OAuth
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }

    // Microsoft OAuth (Azure AD)
    public string? MicrosoftClientId { get; set; }
    public string? MicrosoftClientSecret { get; set; }
    public string? MicrosoftTenantId { get; set; }

    // Facebook OAuth
    public string? FacebookClientId { get; set; }
    public string? FacebookClientSecret { get; set; }

    // Apple Sign In
    public string? AppleClientId { get; set; }       // Also known as Service ID
    public string? AppleTeamId { get; set; }         // 10-character Team ID
    public string? AppleKeyId { get; set; }          // Key ID from Apple Developer
    public string? ApplePrivateKey { get; set; }     // P8 private key content

    // Validation helpers
    public bool HasGoogle => !string.IsNullOrEmpty(GoogleClientId) && !string.IsNullOrEmpty(GoogleClientSecret);
    public bool HasMicrosoft => !string.IsNullOrEmpty(MicrosoftClientId) && !string.IsNullOrEmpty(MicrosoftClientSecret);
    public bool HasFacebook => !string.IsNullOrEmpty(FacebookClientId) && !string.IsNullOrEmpty(FacebookClientSecret);
    public bool HasApple => !string.IsNullOrEmpty(AppleClientId) && !string.IsNullOrEmpty(AppleTeamId)
                          && !string.IsNullOrEmpty(AppleKeyId) && !string.IsNullOrEmpty(ApplePrivateKey);
}

/// <summary>
/// Credenciales RabbitMQ.
/// </summary>
public class RabbitMqCredentials
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    public string ToConnectionString() => $"amqp://{Username}:{Password}@{Host}:{Port}/{VirtualHost}";
}
