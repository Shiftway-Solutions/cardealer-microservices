using System.Security.Cryptography;
using System.Text;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OtpNet;
using System.Text.Json;

namespace AuthService.Infrastructure.Services.Identity;

public class TwoFactorService : ITwoFactorService
{
    private readonly IQRCodeService _qrCodeService;
    private readonly IDistributedCache _cache;
    private readonly IAuthNotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        IQRCodeService qrCodeService,
        IDistributedCache cache,
        IAuthNotificationService notificationService,
        IUserRepository userRepository,
        ILogger<TwoFactorService> logger)
    {
        _qrCodeService = qrCodeService;
        _cache = cache;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<(string secret, string qrCodeUri)> GenerateAuthenticatorKeyAsync(string userId, string email)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);

        var issuer = "OKLA";
        var qrCodeUri = $"otpauth://totp/{issuer}:{email}?secret={base32Secret}&issuer={issuer}&digits=6";
        var qrCodeImage = _qrCodeService.GenerateQRCode(qrCodeUri);

        await _cache.SetStringAsync($"2fa_setup_{userId}", base32Secret, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return (base32Secret, qrCodeImage);
    }

    public bool VerifyAuthenticatorCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _);
    }

    public async Task<string> GenerateSmsCodeAsync(string userId)
    {
        var code = GenerateRandomCode(6);
        await _cache.SetStringAsync($"2fa_sms_{userId}", code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
        return code;
    }

    public async Task<string> GenerateEmailCodeAsync(string userId)
    {
        var code = GenerateRandomCode(6);
        await _cache.SetStringAsync($"2fa_email_{userId}", code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
        return code;
    }

    public async Task<bool> VerifyCodeAsync(string userId, string code, TwoFactorAuthType type)
    {
        var cacheKey = type switch
        {
            TwoFactorAuthType.Authenticator => $"2fa_setup_{userId}",
            TwoFactorAuthType.SMS => $"2fa_sms_{userId}",
            TwoFactorAuthType.Email => $"2fa_email_{userId}",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        var storedCode = await _cache.GetStringAsync(cacheKey);
        if (storedCode == code)
        {
            await _cache.RemoveAsync(cacheKey);
            return true;
        }

        return false;
    }

    /// <summary>
    /// US-18.1: Generates recovery codes with DUAL PERSISTENCE (Redis + PostgreSQL)
    /// - Redis: Fast cache with 365 days TTL
    /// - PostgreSQL: Persistent storage via TwoFactorAuth entity
    /// </summary>
    public async Task<List<string>> GenerateRecoveryCodesAsync(string userId)
    {
        var recoveryCodes = new List<string>();
        for (int i = 0; i < 10; i++)
            recoveryCodes.Add(GenerateRandomCode(8));

        // 1. Save to Redis (fast cache) with extended TTL of 365 days
        await _cache.SetStringAsync($"recovery_codes_{userId}",
            JsonSerializer.Serialize(recoveryCodes),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) });

        // 2. Save to PostgreSQL (persistent) via TwoFactorAuth entity
        try
        {
            var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId);
            if (twoFactorAuth != null)
            {
                twoFactorAuth.UpdateRecoveryCodes(recoveryCodes);
                await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);
                _logger.LogInformation("Recovery codes saved to PostgreSQL for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save recovery codes to PostgreSQL for user {UserId}, Redis cache will be used", userId);
        }

        return recoveryCodes;
    }

    /// <summary>
    /// US-18.1: Verifies recovery code with FALLBACK from Redis to PostgreSQL
    /// </summary>
    public async Task<bool> VerifyRecoveryCodeAsync(string userId, string code)
    {
        var cacheKey = $"recovery_codes_{userId}";

        // Try Redis first
        var storedCodesJson = await _cache.GetStringAsync(cacheKey);
        List<string>? storedCodes = null;

        if (storedCodesJson != null)
        {
            storedCodes = JsonSerializer.Deserialize<List<string>>(storedCodesJson);
        }
        else
        {
            // FALLBACK to PostgreSQL if Redis fails or is empty (US-18.1)
            _logger.LogInformation("Redis cache miss for recovery codes, falling back to PostgreSQL for user {UserId}", userId);
            try
            {
                var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId);
                if (twoFactorAuth != null && twoFactorAuth.RecoveryCodes.Any())
                {
                    storedCodes = twoFactorAuth.RecoveryCodes;
                    // Repopulate Redis cache from PostgreSQL
                    await _cache.SetStringAsync(cacheKey,
                        JsonSerializer.Serialize(storedCodes),
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) });
                    _logger.LogInformation("Recovery codes restored from PostgreSQL to Redis for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recovery codes from PostgreSQL for user {UserId}", userId);
                return false;
            }
        }

        if (storedCodes == null || !storedCodes.Any())
            return false;

        // Normalize the code (uppercase, no spaces, no dashes)
        var normalizedCode = code.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        var normalizedCodeBytes = Encoding.UTF8.GetBytes(normalizedCode.PadRight(8, '0'));

        // Use constant-time comparison to prevent timing attacks
        string? matchedCode = null;
        bool found = false;

        foreach (var storedCode in storedCodes)
        {
            var storedCodeBytes = Encoding.UTF8.GetBytes(storedCode.PadRight(8, '0'));
            if (CryptographicOperations.FixedTimeEquals(normalizedCodeBytes, storedCodeBytes))
            {
                matchedCode = storedCode;
                found = true;
            }
        }

        if (found && matchedCode != null)
        {
            storedCodes.Remove(matchedCode);

            // Update Redis
            await _cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(storedCodes),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) });

            // Update PostgreSQL (US-18.1: dual persistence)
            try
            {
                var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId);
                if (twoFactorAuth != null)
                {
                    twoFactorAuth.UpdateRecoveryCodes(storedCodes);
                    await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update recovery codes in PostgreSQL after code use for user {UserId}", userId);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// US-18.1: Gets remaining recovery codes count with PostgreSQL fallback
    /// </summary>
    public async Task<int> GetRemainingRecoveryCodesCountAsync(string userId)
    {
        // Try Redis first
        var storedCodesJson = await _cache.GetStringAsync($"recovery_codes_{userId}");
        if (storedCodesJson != null)
        {
            var storedCodes = JsonSerializer.Deserialize<List<string>>(storedCodesJson);
            return storedCodes?.Count ?? 0;
        }

        // Fallback to PostgreSQL
        try
        {
            var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId);
            return twoFactorAuth?.RecoveryCodes.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get recovery codes count from PostgreSQL for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> IsTwoFactorEnabledAsync(string userId)
    {
        var enabled = await _cache.GetStringAsync($"2fa_enabled_{userId}");
        return enabled == "true";
    }

    // CORRECCIÓN: Este método ahora SI envía códigos realmente
    public async Task<bool> SendTwoFactorCodeAsync(string userId, TwoFactorAuthType type)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            string code;
            string destination;

            switch (type)
            {
                case TwoFactorAuthType.SMS:
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                        return false;

                    code = await GenerateSmsCodeAsync(userId);
                    destination = user.PhoneNumber;
                    await _notificationService.SendTwoFactorCodeAsync(destination, code, TwoFactorAuthType.SMS);
                    break;

                case TwoFactorAuthType.Email:
                    code = await GenerateEmailCodeAsync(userId);
                    destination = user.Email!;
                    await _notificationService.SendTwoFactorCodeAsync(destination, code, TwoFactorAuthType.Email);
                    break;

                default:
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA code via {Type} for user {UserId}", type, userId);
            return false;
        }
    }

    // NUEVO MÉTODO: Enviar código a un destino específico
    public async Task<bool> SendTwoFactorCodeToDestinationAsync(string userId, TwoFactorAuthType type, string destination)
    {
        try
        {
            string code;

            switch (type)
            {
                case TwoFactorAuthType.SMS:
                    code = await GenerateSmsCodeAsync(userId);
                    await _notificationService.SendTwoFactorCodeAsync(destination, code, TwoFactorAuthType.SMS);
                    break;

                case TwoFactorAuthType.Email:
                    code = await GenerateEmailCodeAsync(userId);
                    await _notificationService.SendTwoFactorCodeAsync(destination, code, TwoFactorAuthType.Email);
                    break;

                default:
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA code via {Type} to {Destination}", type, destination);
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random code.
    /// Uses RandomNumberGenerator instead of System.Random for security.
    /// </summary>
    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }
}
