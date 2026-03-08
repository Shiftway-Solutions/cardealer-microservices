using System.Text.RegularExpressions;

namespace SearchAgent.Application.Services;

/// <summary>
/// PII (Personally Identifiable Information) sanitizer for SearchAgent.
/// RED TEAM v2: Prevents user PII from reaching the LLM via search queries.
/// Lighter version than ChatbotService — SearchAgent deals with search terms, not conversations.
/// Dominican Republic specific patterns: cédula, RNC, phone numbers (809/829/849).
/// </summary>
public static class PiiSanitizer
{
    // ── Dominican Republic Cédula (ID) ──────────────────────────────
    private static readonly Regex CedulaPattern = new(
        @"\b\d{3}[-\s]?\d{7}[-\s]?\d{1}\b", RegexOptions.Compiled);

    // ── RNC (Registro Nacional del Contribuyente) ───────────────────
    private static readonly Regex RncPattern = new(
        @"\b\d{1}[-\s]?\d{2}[-\s]?\d{5}[-\s]?\d{1,2}\b", RegexOptions.Compiled);

    // ── Credit/Debit Card Numbers ───────────────────────────────────
    private static readonly Regex CreditCardPattern = new(
        @"\b(?:\d{4}[-\s]?){3,4}\d{1,4}\b", RegexOptions.Compiled);

    // ── Dominican Phone Numbers ─────────────────────────────────────
    private static readonly Regex PhonePattern = new(
        @"(?:\+?1[-\s]?)?(?:809|829|849)[-\s]?\d{3}[-\s]?\d{4}\b", RegexOptions.Compiled);

    // ── Email Addresses ─────────────────────────────────────────────
    private static readonly Regex EmailPattern = new(
        @"\b[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}\b", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes PII from a search query before sending to the LLM.
    /// Returns the sanitized query and detection details.
    /// </summary>
    public static PiiSanitizationResult Sanitize(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new PiiSanitizationResult(query, false, new List<string>());

        var result = query;
        var detectedTypes = new List<string>();
        var wasSanitized = false;

        // Credit card (highest priority)
        if (CreditCardPattern.IsMatch(result))
        {
            var matches = CreditCardPattern.Matches(result);
            foreach (Match match in matches)
            {
                var digits = new string(match.Value.Where(char.IsDigit).ToArray());
                if (digits.Length >= 13 && digits.Length <= 19)
                {
                    detectedTypes.Add("credit_card");
                    result = result.Replace(match.Value, "");
                    wasSanitized = true;
                }
            }
        }

        // Dominican Cédula
        if (CedulaPattern.IsMatch(result))
        {
            var matches = CedulaPattern.Matches(result);
            foreach (Match match in matches)
            {
                var digits = new string(match.Value.Where(char.IsDigit).ToArray());
                if (digits.Length == 11)
                {
                    detectedTypes.Add("cedula");
                    result = result.Replace(match.Value, "");
                    wasSanitized = true;
                }
            }
        }

        // RNC
        if (RncPattern.IsMatch(result))
        {
            detectedTypes.Add("rnc");
            result = RncPattern.Replace(result, "");
            wasSanitized = true;
        }

        // Phone — strip from search queries (no legitimate vehicle search includes a phone number)
        if (PhonePattern.IsMatch(result))
        {
            detectedTypes.Add("phone");
            result = PhonePattern.Replace(result, "");
            wasSanitized = true;
        }

        // Email — strip from search queries
        if (EmailPattern.IsMatch(result))
        {
            detectedTypes.Add("email");
            result = EmailPattern.Replace(result, "");
            wasSanitized = true;
        }

        // Clean up any double/trailing spaces left after removal
        result = Regex.Replace(result, @"\s{2,}", " ").Trim();

        return new PiiSanitizationResult(result, wasSanitized, detectedTypes);
    }

    /// <summary>
    /// Sanitizes PII from the SearchAgent response fields (mensaje_usuario, advertencias).
    /// Ensures the LLM doesn't echo back any PII from the conversation.
    /// </summary>
    public static void SanitizeResponse(Domain.Models.SearchAgentResponse response)
    {
        if (!string.IsNullOrEmpty(response.MensajeUsuario))
        {
            response.MensajeUsuario = StripPii(response.MensajeUsuario);
        }

        if (response.Advertencias != null)
        {
            response.Advertencias = response.Advertencias
                .Select(StripPii)
                .ToList();
        }
    }

    private static string StripPii(string text)
    {
        var result = text;
        result = CreditCardPattern.Replace(result, "[DATO_PROTEGIDO]");
        result = PhonePattern.Replace(result, "[DATO_PROTEGIDO]");
        result = EmailPattern.Replace(result, "[DATO_PROTEGIDO]");

        // Cédula — only 11-digit matches
        var cedulaMatches = CedulaPattern.Matches(result);
        foreach (Match match in cedulaMatches)
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            if (digits.Length == 11)
                result = result.Replace(match.Value, "[DATO_PROTEGIDO]");
        }

        return result;
    }
}

/// <summary>
/// Result of PII sanitization for SearchAgent.
/// </summary>
public record PiiSanitizationResult(string SanitizedQuery, bool WasSanitized, List<string> DetectedTypes);
