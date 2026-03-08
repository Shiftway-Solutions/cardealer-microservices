using System.Text.RegularExpressions;

namespace RecoAgent.Application.Services;

/// <summary>
/// Post-LLM output validation for RecoAgent responses.
/// RED TEAM v2: Prevents PII leakage, offensive content, and system prompt leakage in recommendations.
/// Modeled after ChatbotService's multi-layer output validation (PiiDetector + ContentModeration + GroundingValidator).
/// </summary>
public static class OutputContentValidator
{
    // ── PII Patterns (Dominican Republic specific) ───────────────────
    private static readonly Regex CedulaPattern = new(
        @"\b\d{3}[-\s]?\d{7}[-\s]?\d{1}\b", RegexOptions.Compiled);

    private static readonly Regex RncPattern = new(
        @"\b\d{1}[-\s]?\d{2}[-\s]?\d{5}[-\s]?\d{1,2}\b", RegexOptions.Compiled);

    private static readonly Regex CreditCardPattern = new(
        @"\b(?:\d{4}[-\s]?){3,4}\d{1,4}\b", RegexOptions.Compiled);

    private static readonly Regex PhonePattern = new(
        @"(?:\+?1[-\s]?)?(?:809|829|849)[-\s]?\d{3}[-\s]?\d{4}\b", RegexOptions.Compiled);

    private static readonly Regex EmailPattern = new(
        @"\b[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}\b", RegexOptions.Compiled);

    // ── System Prompt Leakage Keywords ───────────────────────────────
    private static readonly string[] SystemPromptKeywords =
    {
        "REGLA ABSOLUTA", "TU FUNCIÓN PRINCIPAL", "system prompt",
        "instrucciones del sistema", "RecoAgent", "Claude",
        "PATROCINADOS CON AFINIDAD", "score_afinidad_perfil",
        "es_patrocinado", "ANTI-ALUCINACIÓN", "DIVERSIFICACIÓN"
    };

    // ── Offensive Content Patterns ───────────────────────────────────
    private static readonly Regex[] OffensivePatterns =
    {
        // Hate speech / discrimination
        new(@"\b(?:negro|negra)\s+(?:de\s+mierda|sucio|sucia|asqueroso|asquerosa)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:haitiano|haitiana)\s+(?:de\s+mierda|sucio|sucia|maldito|maldita)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Sexual content
        new(@"\b(?:puta|putas|puto|putos|maricón|maricona)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Violence / threats
        new(@"\b(?:voy\s+a\s+matar|te\s+voy\s+a|te\s+mato|amenaza)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Scam / fraud patterns in recommendations
        new(@"\b(?:envía\s+dinero|western\s+union|giro\s+bancario|pago\s+por\s+adelantado)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(?:send\s+money|wire\s+transfer|advance\s+payment|nigerian\s+prince)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// Validates the full RecoAgent response for PII, offensive content, and system prompt leakage.
    /// Returns a validation result with details of any issues found and sanitized content.
    /// </summary>
    public static OutputValidationResult Validate(DTOs.RecoAgentResponse response)
    {
        var issues = new List<string>();

        // 1. Scan all recommendation explanations
        if (response.Recomendaciones != null)
        {
            foreach (var reco in response.Recomendaciones)
            {
                if (string.IsNullOrEmpty(reco.RazonRecomendacion))
                    continue;

                // PII check
                var piiResult = SanitizePii(reco.RazonRecomendacion);
                if (piiResult.WasSanitized)
                {
                    reco.RazonRecomendacion = piiResult.SanitizedText;
                    issues.AddRange(piiResult.DetectedTypes.Select(t =>
                        $"pii_{t}:vehiculo_{reco.VehiculoId}"));
                }

                // Offensive content check
                if (ContainsOffensiveContent(reco.RazonRecomendacion))
                {
                    reco.RazonRecomendacion = "Vehículo recomendado basado en tu perfil y preferencias.";
                    issues.Add($"offensive_content:vehiculo_{reco.VehiculoId}");
                }

                // System prompt leakage check
                if (ContainsSystemPromptLeakage(reco.RazonRecomendacion))
                {
                    reco.RazonRecomendacion = "Vehículo recomendado basado en tu perfil y preferencias.";
                    issues.Add($"prompt_leakage:vehiculo_{reco.VehiculoId}");
                }
            }
        }

        // 2. Scan etapa_compra_detectada (could leak system prompt terminology)
        if (!string.IsNullOrEmpty(response.EtapaCompraDetectada))
        {
            if (ContainsSystemPromptLeakage(response.EtapaCompraDetectada))
            {
                response.EtapaCompraDetectada = "exploracion";
                issues.Add("prompt_leakage:etapa_compra");
            }
        }

        return new OutputValidationResult
        {
            HasIssues = issues.Count > 0,
            Issues = issues,
            IssueCount = issues.Count,
        };
    }

    /// <summary>
    /// Sanitizes PII from a text string. Returns the sanitized text and detected PII types.
    /// </summary>
    private static PiiSanitizationResult SanitizePii(string text)
    {
        var result = text;
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
                    result = result.Replace(match.Value, "[DATO_PROTEGIDO]");
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
                    result = result.Replace(match.Value, "[DATO_PROTEGIDO]");
                    wasSanitized = true;
                }
            }
        }

        // RNC
        if (RncPattern.IsMatch(result))
        {
            detectedTypes.Add("rnc");
            result = RncPattern.Replace(result, "[DATO_PROTEGIDO]");
            wasSanitized = true;
        }

        // Phone
        if (PhonePattern.IsMatch(result))
        {
            detectedTypes.Add("phone");
            result = PhonePattern.Replace(result, "[DATO_PROTEGIDO]");
            wasSanitized = true;
        }

        // Email
        if (EmailPattern.IsMatch(result))
        {
            detectedTypes.Add("email");
            result = EmailPattern.Replace(result, "[DATO_PROTEGIDO]");
            wasSanitized = true;
        }

        return new PiiSanitizationResult(result, detectedTypes, wasSanitized);
    }

    /// <summary>
    /// Checks if text contains offensive content.
    /// </summary>
    private static bool ContainsOffensiveContent(string text)
    {
        return OffensivePatterns.Any(p => p.IsMatch(text));
    }

    /// <summary>
    /// Checks if text contains system prompt keywords that indicate leakage.
    /// </summary>
    private static bool ContainsSystemPromptLeakage(string text)
    {
        return SystemPromptKeywords.Any(kw =>
            text.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    private record PiiSanitizationResult(string SanitizedText, List<string> DetectedTypes, bool WasSanitized);
}

/// <summary>
/// Result of output content validation for RecoAgent responses.
/// </summary>
public class OutputValidationResult
{
    public bool HasIssues { get; set; }
    public List<string> Issues { get; set; } = new();
    public int IssueCount { get; set; }
}
