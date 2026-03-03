using System.Text.RegularExpressions;

namespace SearchAgent.Application.Services;

/// <summary>
/// Lightweight prompt injection detector for SearchAgent.
/// Since SearchAgent outputs structured JSON (not prose), the risk is lower,
/// but system prompt content could leak via advertencias/mensaje_usuario fields.
/// </summary>
public static class PromptInjectionDetector
{
    // ── System Role Impersonation ────────────────────────────────────
    private static readonly Regex[] SystemRolePatterns =
    {
        new(@"\[SYSTEM\]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|system\|>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|im_start\|>system", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\[INST\]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<<SYS>>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Instruction Override Attempts ────────────────────────────────
    private static readonly Regex[] OverridePatterns =
    {
        new(@"ignor[ae]\s+(?:todas?\s+)?(?:las?\s+)?instrucciones?\s+anteriores?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"ignore\s+(?:all\s+)?(?:previous|prior|above)\s+instructions?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"olvida\s+(?:todo|las\s+reglas|tus\s+instrucciones)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"forget\s+(?:everything|all|your\s+instructions)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"override\s+(?:your\s+)?prompt", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Prompt Extraction Attempts ───────────────────────────────────
    private static readonly Regex[] ExtractionPatterns =
    {
        new(@"(?:muéstrame|dime|repite|muestra)\s+(?:tu\s+)?(?:system\s+)?prompt", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:show|tell|repeat|reveal|print)\s+(?:me\s+)?(?:your\s+)?(?:system\s+)?(?:prompt|instructions)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"output\s+(?:your\s+)?(?:initial|system|original)\s+(?:prompt|instructions)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// Detects prompt injection patterns in the search query.
    /// Returns true if injection is detected and the query should be rejected.
    /// </summary>
    public static PromptInjectionResult Detect(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return PromptInjectionResult.Safe();

        var detectedPatterns = new List<string>();

        foreach (var pattern in SystemRolePatterns)
            if (pattern.IsMatch(query))
                detectedPatterns.Add($"system_role:{pattern}");

        foreach (var pattern in OverridePatterns)
            if (pattern.IsMatch(query))
                detectedPatterns.Add($"override:{pattern}");

        foreach (var pattern in ExtractionPatterns)
            if (pattern.IsMatch(query))
                detectedPatterns.Add($"extraction:{pattern}");

        if (detectedPatterns.Count == 0)
            return PromptInjectionResult.Safe();

        var hasSystemRole = detectedPatterns.Any(p => p.StartsWith("system_role:"));
        var hasOverride = detectedPatterns.Any(p => p.StartsWith("override:"));

        var shouldBlock = hasSystemRole || hasOverride;

        return new PromptInjectionResult
        {
            IsInjectionDetected = true,
            DetectedPatterns = detectedPatterns,
            ShouldBlock = shouldBlock,
        };
    }

    /// <summary>
    /// Sanitizes the SearchAgent response to prevent system prompt leakage
    /// via advertencias or mensaje_usuario fields.
    /// </summary>
    public static void SanitizeResponse(Domain.Models.SearchAgentResponse response)
    {
        // Check advertencias for potential system prompt content
        if (response.Advertencias != null)
        {
            var systemPromptKeywords = new[] {
                "REGLA ABSOLUTA", "TU FUNCIÓN PRINCIPAL", "RESPONDE ÚNICAMENTE",
                "CORRECCIONES ORTOGRÁFICAS", "system prompt", "instrucciones del sistema",
                "MARCAS POR SEGMENTO", "PATROCINADOS CON AFINIDAD"
            };

            response.Advertencias = response.Advertencias
                .Where(a => !systemPromptKeywords.Any(kw =>
                    a.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Check mensaje_usuario for system prompt content
        if (!string.IsNullOrEmpty(response.MensajeUsuario))
        {
            var leakagePatterns = new[] {
                "REGLA ABSOLUTA", "TU FUNCIÓN", "system prompt",
                "instrucciones", "SearchAgent", "Claude"
            };

            if (leakagePatterns.Any(p =>
                response.MensajeUsuario.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                response.MensajeUsuario = "¿Buscas un vehículo? Prueba con algo como 'Toyota Corolla 2020 automático' o 'SUV económica para familia'. 🚗";
            }
        }
    }
}

public class PromptInjectionResult
{
    public bool IsInjectionDetected { get; set; }
    public List<string> DetectedPatterns { get; set; } = new();
    public bool ShouldBlock { get; set; }

    public static PromptInjectionResult Safe() => new()
    {
        IsInjectionDetected = false,
        ShouldBlock = false,
    };
}
