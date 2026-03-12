using System.Text.Json;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// US-18.3: Google reCAPTCHA v3 verification service.
/// Validates CAPTCHA tokens after 2+ failed login attempts.
/// 
/// Configuration required in appsettings.json:
/// {
///   "ReCaptcha": {
///     "SecretKey": "your-secret-key-here",
///     "MinScore": 0.5,
///     "Enabled": true
///   }
/// }
/// </summary>
public class CaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CaptchaService> _logger;
    private readonly string _secretKey;
    private readonly decimal _minScore;
    private readonly bool _enabled;

    private const string RECAPTCHA_VERIFY_URL = "https://www.google.com/recaptcha/api/siteverify";
    private const int CAPTCHA_REQUIRED_AFTER_ATTEMPTS = 2;

    public decimal LastScore { get; private set; }

    public CaptchaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CaptchaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _secretKey = configuration["ReCaptcha:SecretKey"] ?? "";
        _minScore = decimal.TryParse(configuration["ReCaptcha:MinScore"], out var score) ? score : 0.5m;

        // Read configured enabled flag from ReCaptcha section
        var recaptchaEnabled = bool.TryParse(configuration["ReCaptcha:Enabled"], out var enabled) && enabled;

        // Allow override via environment variable `CAPTCHA__ENABLED` for testing/dev convenience.
        // This env var maps to configuration key exactly as written when set in the environment.
        var envOverride = configuration["CAPTCHA__ENABLED"] ?? configuration["Captcha__Enabled"];
        if (!string.IsNullOrEmpty(envOverride) && bool.TryParse(envOverride, out var envEnabled))
        {
            _enabled = envEnabled;
            _logger.LogInformation("reCAPTCHA enabled overridden by env CAPTCHA__ENABLED => {Enabled}", _enabled);
        }
        else
        {
            _enabled = recaptchaEnabled;
        }

        if (_enabled && string.IsNullOrEmpty(_secretKey))
        {
            _logger.LogWarning("reCAPTCHA is enabled but SecretKey is not configured. CAPTCHA validation will be skipped.");
        }
    }

    /// <inheritdoc />
    public bool IsCaptchaRequired(int failedAttemptCount)
    {
        return _enabled && failedAttemptCount >= CAPTCHA_REQUIRED_AFTER_ATTEMPTS;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(string token, string expectedAction, string? ipAddress = null)
    {
        // If not enabled or no secret key, skip verification (allow login)
        if (!_enabled || string.IsNullOrEmpty(_secretKey))
        {
            _logger.LogDebug("reCAPTCHA verification skipped (disabled or not configured)");
            LastScore = 1.0m;
            return true;
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Empty CAPTCHA token provided");
            LastScore = 0.0m;
            return false;
        }

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "secret", _secretKey },
                { "response", token },
                { "remoteip", ipAddress ?? "" }
            });

            var response = await _httpClient.PostAsync(RECAPTCHA_VERIFY_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("reCAPTCHA API returned status {StatusCode}", response.StatusCode);
                LastScore = 0.0m;
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ReCaptchaResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                _logger.LogError("Failed to parse reCAPTCHA response");
                LastScore = 0.0m;
                return false;
            }

            LastScore = result.Score;

            if (!result.Success)
            {
                _logger.LogWarning("reCAPTCHA verification failed. Errors: {Errors}",
                    string.Join(", ", result.ErrorCodes ?? Array.Empty<string>()));
                return false;
            }

            // Verify action matches (protection against replay attacks)
            if (!string.IsNullOrEmpty(expectedAction) && result.Action != expectedAction)
            {
                _logger.LogWarning("reCAPTCHA action mismatch. Expected: {Expected}, Got: {Actual}",
                    expectedAction, result.Action);
                return false;
            }

            // Verify score meets minimum threshold
            if (result.Score < _minScore)
            {
                _logger.LogWarning("reCAPTCHA score {Score} is below minimum threshold {MinScore}",
                    result.Score, _minScore);
                return false;
            }

            _logger.LogInformation("reCAPTCHA verification successful. Score: {Score}, Action: {Action}",
                result.Score, result.Action);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying reCAPTCHA token");
            // In case of error, allow login (fail open) to avoid blocking legitimate users
            // You may want to change this behavior in high-security scenarios
            LastScore = 0.5m;
            return true;
        }
    }

    /// <summary>
    /// Response model from Google reCAPTCHA v3 API
    /// </summary>
    private class ReCaptchaResponse
    {
        public bool Success { get; set; }
        public decimal Score { get; set; }
        public string? Action { get; set; }
        public DateTime? ChallengeTs { get; set; }
        public string? Hostname { get; set; }

        // Error codes if verification failed
        public string[]? ErrorCodes { get; set; }
    }
}
