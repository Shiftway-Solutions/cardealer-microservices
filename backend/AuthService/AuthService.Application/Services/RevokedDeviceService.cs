using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AuthService.Application.Services;

/// <summary>
/// Servicio para gestionar dispositivos revocados.
/// Almacena y verifica fingerprints de dispositivos que fueron desconectados por seguridad.
/// Los dispositivos revocados requieren verificación adicional para volver a iniciar sesión.
/// </summary>
public interface IRevokedDeviceService
{
    /// <summary>
    /// Marca un dispositivo como revocado para un usuario específico.
    /// </summary>
    Task MarkDeviceAsRevokedAsync(
        string userId,
        string ipAddress,
        string userAgent,
        string browser,
        string operatingSystem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un dispositivo está en la lista de revocados.
    /// </summary>
    Task<RevokedDeviceCheckResult> CheckIfDeviceIsRevokedAsync(
        string userId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un dispositivo de la lista de revocados (después de verificación exitosa).
    /// </summary>
    Task ClearRevokedDeviceAsync(
        string userId,
        string deviceFingerprint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera un fingerprint único para un dispositivo basado en IP y User-Agent.
    /// </summary>
    string GenerateDeviceFingerprint(string ipAddress, string userAgent);
}

public class RevokedDeviceCheckResult
{
    public bool IsRevoked { get; set; }
    public string? DeviceFingerprint { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
}

public class RevokedDeviceService : IRevokedDeviceService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RevokedDeviceService> _logger;

    // Los dispositivos revocados se mantienen en cache por 30 días
    private const int REVOKED_DEVICE_EXPIRATION_DAYS = 30;
    private const string REVOKED_DEVICE_PREFIX = "revoked_device:";
    private const string REVOKED_DEVICES_LIST_PREFIX = "revoked_devices_list:";

    public RevokedDeviceService(
        IDistributedCache cache,
        ILogger<RevokedDeviceService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task MarkDeviceAsRevokedAsync(
        string userId,
        string ipAddress,
        string userAgent,
        string browser,
        string operatingSystem,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fingerprint = GenerateDeviceFingerprint(ipAddress, userAgent);
            var cacheKey = $"{REVOKED_DEVICE_PREFIX}{userId}:{fingerprint}";

            var revokedDevice = new RevokedDeviceData
            {
                UserId = userId,
                DeviceFingerprint = fingerprint,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Browser = browser,
                OperatingSystem = operatingSystem,
                RevokedAt = DateTime.UtcNow
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(revokedDevice),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(REVOKED_DEVICE_EXPIRATION_DAYS)
                },
                cancellationToken);

            // También guardar en una lista de dispositivos revocados del usuario (para posible auditoría)
            var listKey = $"{REVOKED_DEVICES_LIST_PREFIX}{userId}";
            var existingList = await _cache.GetStringAsync(listKey, cancellationToken);
            var deviceList = string.IsNullOrEmpty(existingList)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(existingList) ?? new List<string>();

            if (!deviceList.Contains(fingerprint))
            {
                deviceList.Add(fingerprint);
                await _cache.SetStringAsync(
                    listKey,
                    JsonSerializer.Serialize(deviceList),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(REVOKED_DEVICE_EXPIRATION_DAYS)
                    },
                    cancellationToken);
            }

            _logger.LogInformation(
                "Device marked as revoked. User: {UserId}, Fingerprint: {Fingerprint}, Browser: {Browser}, OS: {OS}",
                userId, fingerprint[..Math.Min(8, fingerprint.Length)] + "...", browser, operatingSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking device as revoked for user {UserId}", userId);
            throw;
        }
    }

    public async Task<RevokedDeviceCheckResult> CheckIfDeviceIsRevokedAsync(
        string userId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fingerprint = GenerateDeviceFingerprint(ipAddress, userAgent);
            var cacheKey = $"{REVOKED_DEVICE_PREFIX}{userId}:{fingerprint}";

            var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedValue))
            {
                return new RevokedDeviceCheckResult { IsRevoked = false };
            }

            var revokedDevice = JsonSerializer.Deserialize<RevokedDeviceData>(cachedValue);
            if (revokedDevice == null)
            {
                return new RevokedDeviceCheckResult { IsRevoked = false };
            }

            _logger.LogWarning(
                "Revoked device detected. User: {UserId}, Fingerprint: {Fingerprint}, RevokedAt: {RevokedAt}",
                userId, fingerprint[..Math.Min(8, fingerprint.Length)] + "...", revokedDevice.RevokedAt);

            return new RevokedDeviceCheckResult
            {
                IsRevoked = true,
                DeviceFingerprint = fingerprint,
                RevokedAt = revokedDevice.RevokedAt,
                Browser = revokedDevice.Browser,
                OperatingSystem = revokedDevice.OperatingSystem
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if device is revoked for user {UserId}", userId);
            // En caso de error, permitir el login normal para no bloquear usuarios legítimos
            return new RevokedDeviceCheckResult { IsRevoked = false };
        }
    }

    public async Task ClearRevokedDeviceAsync(
        string userId,
        string deviceFingerprint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{REVOKED_DEVICE_PREFIX}{userId}:{deviceFingerprint}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            // Remover de la lista también
            var listKey = $"{REVOKED_DEVICES_LIST_PREFIX}{userId}";
            var existingList = await _cache.GetStringAsync(listKey, cancellationToken);
            if (!string.IsNullOrEmpty(existingList))
            {
                var deviceList = JsonSerializer.Deserialize<List<string>>(existingList) ?? new List<string>();
                if (deviceList.Remove(deviceFingerprint))
                {
                    await _cache.SetStringAsync(
                        listKey,
                        JsonSerializer.Serialize(deviceList),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(REVOKED_DEVICE_EXPIRATION_DAYS)
                        },
                        cancellationToken);
                }
            }

            _logger.LogInformation(
                "Revoked device cleared. User: {UserId}, Fingerprint: {Fingerprint}",
                userId, deviceFingerprint[..Math.Min(8, deviceFingerprint.Length)] + "...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing revoked device for user {UserId}", userId);
            throw;
        }
    }

    public string GenerateDeviceFingerprint(string ipAddress, string userAgent)
    {
        // Combinar IP + User-Agent para crear un fingerprint único
        // Nota: El User-Agent puede cambiar con actualizaciones del navegador,
        // por lo que usamos solo partes estables
        var stableUserAgent = ExtractStableUserAgentParts(userAgent);
        var combined = $"{ipAddress}|{stableUserAgent}";

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Extrae partes estables del User-Agent (navegador principal y SO).
    /// Esto permite que actualizaciones menores del navegador no invaliden el fingerprint.
    /// </summary>
    private static string ExtractStableUserAgentParts(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "unknown";

        var normalized = userAgent.ToLowerInvariant();
        var parts = new List<string>();

        // Sistema operativo
        if (normalized.Contains("windows"))
            parts.Add("windows");
        else if (normalized.Contains("mac os") || normalized.Contains("macos"))
            parts.Add("macos");
        else if (normalized.Contains("linux"))
            parts.Add("linux");
        else if (normalized.Contains("android"))
            parts.Add("android");
        else if (normalized.Contains("iphone") || normalized.Contains("ipad") || normalized.Contains("ios"))
            parts.Add("ios");
        else
            parts.Add("other-os");

        // Navegador principal
        if (normalized.Contains("edg/"))
            parts.Add("edge");
        else if (normalized.Contains("chrome/") && !normalized.Contains("chromium"))
            parts.Add("chrome");
        else if (normalized.Contains("firefox/"))
            parts.Add("firefox");
        else if (normalized.Contains("safari/") && !normalized.Contains("chrome"))
            parts.Add("safari");
        else if (normalized.Contains("opera") || normalized.Contains("opr/"))
            parts.Add("opera");
        else
            parts.Add("other-browser");

        return string.Join("|", parts);
    }

    private class RevokedDeviceData
    {
        public string UserId { get; set; } = string.Empty;
        public string DeviceFingerprint { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public DateTime RevokedAt { get; set; }
    }
}
