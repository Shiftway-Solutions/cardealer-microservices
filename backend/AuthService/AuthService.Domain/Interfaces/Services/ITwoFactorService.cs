// AuthService.Domain/Interfaces/Services/ITwoFactorService.cs
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Interfaces.Services;

public interface ITwoFactorService
{
    // Authenticator App
    Task<(string secret, string qrCodeUri)> GenerateAuthenticatorKeyAsync(string userId, string email);
    bool VerifyAuthenticatorCode(string secret, string code);

    // SMS/Email
    Task<string> GenerateSmsCodeAsync(string userId);
    Task<string> GenerateEmailCodeAsync(string userId);
    Task<bool> VerifyCodeAsync(string userId, string code, TwoFactorAuthType type);

    // Recovery Codes
    Task<List<string>> GenerateRecoveryCodesAsync(string userId);
    Task<bool> VerifyRecoveryCodeAsync(string userId, string code);

    /// <summary>
    /// Gets the count of remaining unused recovery codes for a user.
    /// Used to warn users when they're running low.
    /// </summary>
    Task<int> GetRemainingRecoveryCodesCountAsync(string userId);

    // General
    Task<bool> IsTwoFactorEnabledAsync(string userId);

    Task<bool> SendTwoFactorCodeAsync(string userId, TwoFactorAuthType type);
    Task<bool> SendTwoFactorCodeToDestinationAsync(string userId, TwoFactorAuthType type, string destination);
}
