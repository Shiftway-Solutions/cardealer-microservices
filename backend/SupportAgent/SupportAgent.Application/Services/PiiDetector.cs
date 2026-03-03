using System.Text.RegularExpressions;

namespace SupportAgent.Application.Services;

/// <summary>
/// PII (Personally Identifiable Information) detector and sanitizer.
/// Ported from ChatbotService with identical DR-specific patterns.
/// Uses regex-based detection (NOT prompt-based) to ensure PII never reaches the LLM.
/// Supports Dominican Republic specific formats: cédula, RNC, phone numbers.
/// </summary>
public static class PiiDetector
{
    // ── Dominican Republic Specific ──────────────────────────────────
    private static readonly Regex CedulaPattern = new(
        @"\b\d{3}[-\s]?\d{7}[-\s]?\d{1}\b", RegexOptions.Compiled);

    private static readonly Regex RncPattern = new(
        @"\b\d{1}[-\s]?\d{2}[-\s]?\d{5}[-\s]?\d{1,2}\b", RegexOptions.Compiled);

    // ── Financial ────────────────────────────────────────────────────
    private static readonly Regex CreditCardPattern = new(
        @"\b(?:\d{4}[-\s]?){3,4}\d{1,4}\b", RegexOptions.Compiled);

    private static readonly Regex CvvPattern = new(
        @"\bcvv\s*[:=]?\s*\d{3,4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BankAccountPattern = new(
        @"\b(?:DO\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}|\d{10,20})\b", RegexOptions.Compiled);

    // ── Contact Info ─────────────────────────────────────────────────
    private static readonly Regex PhonePattern = new(
        @"(?:\+?1[-\s]?)?(?:809|829|849)[-\s]?\d{3}[-\s]?\d{4}\b", RegexOptions.Compiled);

    private static readonly Regex EmailPattern = new(
        @"\b[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}\b", RegexOptions.Compiled);

    // ── Identity Documents ───────────────────────────────────────────
    private static readonly Regex PassportPattern = new(
        @"\b[A-Z]{1,2}\d{6,9}\b", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes PII from user messages BEFORE sending to the LLM.
    /// Returns the sanitized message and detection info.
    /// </summary>
    public static PiiSanitizationResult Sanitize(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return new PiiSanitizationResult(message, new PiiDetectionInfo());

        var sanitized = message;
        var info = new PiiDetectionInfo();

        // Credit cards (check with Luhn before redacting)
        sanitized = CreditCardPattern.Replace(sanitized, match =>
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            if (digits.Length >= 13 && digits.Length <= 19 && PassesLuhnCheck(digits))
            {
                info.HasCreditCard = true;
                info.DetectedTypes.Add("credit_card");
                return "[TARJETA-REDACTADA]";
            }
            return match.Value;
        });

        // CVV
        sanitized = CvvPattern.Replace(sanitized, match =>
        {
            info.DetectedTypes.Add("cvv");
            return "[CVV-REDACTADO]";
        });

        // Cédula dominicana
        sanitized = CedulaPattern.Replace(sanitized, match =>
        {
            info.HasCedula = true;
            info.DetectedTypes.Add("cedula");
            return "[CÉDULA-REDACTADA]";
        });

        // RNC
        sanitized = RncPattern.Replace(sanitized, match =>
        {
            info.DetectedTypes.Add("rnc");
            return "[RNC-REDACTADO]";
        });

        // Phone numbers (DR format)
        sanitized = PhonePattern.Replace(sanitized, match =>
        {
            info.HasPhone = true;
            info.DetectedTypes.Add("phone");
            return "[TELÉFONO-REDACTADO]";
        });

        // Email
        sanitized = EmailPattern.Replace(sanitized, match =>
        {
            info.HasEmail = true;
            info.DetectedTypes.Add("email");
            return "[EMAIL-REDACTADO]";
        });

        // Bank accounts (IBAN or long number)
        sanitized = BankAccountPattern.Replace(sanitized, match =>
        {
            info.DetectedTypes.Add("bank_account");
            return "[CUENTA-REDACTADA]";
        });

        // Passport
        sanitized = PassportPattern.Replace(sanitized, match =>
        {
            info.DetectedTypes.Add("passport");
            return "[PASAPORTE-REDACTADO]";
        });

        info.WasSanitized = sanitized != message;

        return new PiiSanitizationResult(sanitized, info);
    }

    /// <summary>
    /// Sanitizes PII from bot responses AFTER receiving from the LLM (echo-back prevention).
    /// </summary>
    public static string SanitizeResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        var sanitized = response;

        // Redact any PII the bot might echo back
        sanitized = CreditCardPattern.Replace(sanitized, match =>
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            return digits.Length >= 13 && PassesLuhnCheck(digits) ? "[TARJETA-REDACTADA]" : match.Value;
        });

        sanitized = CedulaPattern.Replace(sanitized, "[CÉDULA-REDACTADA]");
        sanitized = CvvPattern.Replace(sanitized, "[CVV-REDACTADO]");

        // Redact DR phone numbers in response
        sanitized = PhonePattern.Replace(sanitized, match =>
        {
            // Don't redact OKLA's official phone (ProConsumidor etc. are in the KB)
            var cleaned = new string(match.Value.Where(char.IsDigit).ToArray());
            if (cleaned.Contains("8095672233")) // ProConsumidor official number
                return match.Value;
            return "[TELÉFONO-REDACTADO]";
        });

        return sanitized;
    }

    /// <summary>
    /// Luhn algorithm for credit card validation.
    /// </summary>
    private static bool PassesLuhnCheck(string digits)
    {
        var sum = 0;
        var alternate = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}

/// <summary>
/// Result of PII sanitization containing the cleaned message and detection metadata.
/// </summary>
public record PiiSanitizationResult(string SanitizedMessage, PiiDetectionInfo DetectionInfo);

/// <summary>
/// Metadata about what PII was detected in a message.
/// </summary>
public class PiiDetectionInfo
{
    public bool HasCreditCard { get; set; }
    public bool HasCedula { get; set; }
    public bool HasEmail { get; set; }
    public bool HasPhone { get; set; }
    public bool WasSanitized { get; set; }
    public List<string> DetectedTypes { get; set; } = new();

    /// <summary>
    /// If a credit card was detected, the user should be redirected to a human agent.
    /// </summary>
    public bool RequiresAgentTransfer => HasCreditCard;
}
