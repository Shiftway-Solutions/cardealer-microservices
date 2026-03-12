using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Enums;
using AuthService.Shared.Exceptions;

namespace AuthService.Application.Features.TwoFactor.Commands.SendSms2FACode;

/// <summary>
/// Handler for sending SMS 2FA verification code.
/// 
/// Flow:
/// 1. Validate temp token → get user ID
/// 2. Verify user has SMS 2FA enabled
/// 3. Get phone number from TwoFactorAuth or User
/// 4. Generate 6-digit code
/// 5. Store code in Redis with 5-minute expiration
/// 6. Send SMS via NotificationService
/// 7. Return masked phone number
/// </summary>
public class SendSms2FACodeCommandHandler : IRequestHandler<SendSms2FACodeCommand, SendSms2FACodeResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly INotificationService _notificationService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly ILogger<SendSms2FACodeCommandHandler> _logger;

    private const int CODE_LENGTH = 6;
    private const int CODE_EXPIRATION_MINUTES = 5;
    private const int MAX_SMS_PER_HOUR = 3;
    private const int SMS_RATE_LIMIT_MINUTES = 60;

    public SendSms2FACodeCommandHandler(
        IUserRepository userRepository,
        IDistributedCache cache,
        INotificationService notificationService,
        IJwtGenerator jwtGenerator,
        ILogger<SendSms2FACodeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _notificationService = notificationService;
        _jwtGenerator = jwtGenerator;
        _logger = logger;
    }

    public async Task<SendSms2FACodeResponse> Handle(SendSms2FACodeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate temp token and get user ID
        var tempTokenData = _jwtGenerator.ValidateTempToken(request.TempToken);
        if (tempTokenData == null)
        {
            _logger.LogWarning("Invalid or expired temp token for SMS 2FA");
            throw new UnauthorizedException("Invalid or expired temp token.");
        }

        if (!Guid.TryParse(tempTokenData.Value.userId, out var userId))
        {
            _logger.LogError("Invalid user ID format in temp token");
            throw new UnauthorizedException("Invalid temp token.");
        }

        // 2. Check rate limit - prevent SMS flooding (max 3 per hour)
        var rateLimitKey = $"sms_rate_limit:{userId}";
        var rateLimitData = await _cache.GetStringAsync(rateLimitKey, cancellationToken);
        int smsCount = 0;
        if (!string.IsNullOrEmpty(rateLimitData) && int.TryParse(rateLimitData, out var existingCount))
        {
            smsCount = existingCount;
        }

        if (smsCount >= MAX_SMS_PER_HOUR)
        {
            _logger.LogWarning("Rate limit exceeded for SMS 2FA. User {UserId} attempted {Count} SMS in 1 hour",
                userId, smsCount);
            throw new ValidationException($"Too many SMS requests. Please wait before requesting another code. Maximum {MAX_SMS_PER_HOUR} codes per hour.");
        }

        // 3. Get user
        var user = await _userRepository.GetByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        // 3. Get 2FA settings
        var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId.ToString());
        if (twoFactorAuth == null || twoFactorAuth.Status != TwoFactorAuthStatus.Enabled)
        {
            _logger.LogWarning("2FA not enabled for user {UserId}", userId);
            throw new ValidationException("Two-factor authentication is not enabled.");
        }

        // 4. Check if SMS method is enabled
        if (twoFactorAuth.PrimaryMethod != TwoFactorAuthType.SMS &&
            !twoFactorAuth.EnabledMethods.Contains(TwoFactorAuthType.SMS))
        {
            _logger.LogWarning("SMS 2FA not enabled for user {UserId}", userId);
            throw new ValidationException("SMS two-factor authentication is not enabled for this account.");
        }

        // 5. Get phone number
        var phoneNumber = twoFactorAuth.PhoneNumber ?? user.PhoneNumber;
        if (string.IsNullOrEmpty(phoneNumber))
        {
            _logger.LogWarning("No phone number configured for user {UserId}", userId);
            throw new ValidationException("No phone number configured for SMS verification.");
        }

        // 6. Generate 6-digit code
        var code = GenerateNumericCode(CODE_LENGTH);

        // 7. Store in Redis with expiration
        var cacheKey = $"sms_2fa_code:{userId}";
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CODE_EXPIRATION_MINUTES)
        };
        await _cache.SetStringAsync(cacheKey, code, cacheOptions, cancellationToken);

        // 8. Send SMS via NotificationService
        try
        {
            await _notificationService.SendSmsAsync(
                phoneNumber,
                $"Tu código de verificación OKLA es: {code}. Válido por {CODE_EXPIRATION_MINUTES} minutos.");

            // 9. Increment rate limit counter after successful send
            await _cache.SetStringAsync(rateLimitKey, (smsCount + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SMS_RATE_LIMIT_MINUTES)
                }, cancellationToken);

            _logger.LogInformation("SMS 2FA code sent to user {UserId}, phone ending in {PhoneSuffix}. SMS count: {Count}/{Max}",
                userId, phoneNumber.Length >= 2 ? phoneNumber[^2..] : "**", smsCount + 1, MAX_SMS_PER_HOUR);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS 2FA code to user {UserId}", userId);
            throw new InvalidOperationException("Failed to send SMS. Please try again.");
        }

        // 10. Return masked phone number
        var maskedPhone = MaskPhoneNumber(phoneNumber);

        return new SendSms2FACodeResponse(
            UserId: userId,
            MaskedPhoneNumber: maskedPhone,
            CodeExpirationMinutes: CODE_EXPIRATION_MINUTES,
            Message: $"Verification code sent to {maskedPhone}"
        );
    }

    private static string GenerateNumericCode(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = (char)('0' + (bytes[i] % 10));
        }
        return new string(code);
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        // Show last 2 digits: +1******89
        var masked = new string('*', phoneNumber.Length - 2) + phoneNumber[^2..];
        return masked;
    }
}
