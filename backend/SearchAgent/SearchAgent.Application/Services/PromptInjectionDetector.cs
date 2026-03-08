using System.Text;
using System.Text.RegularExpressions;

namespace SearchAgent.Application.Services;

/// <summary>
/// Lightweight prompt injection detector for SearchAgent.
/// Since SearchAgent outputs structured JSON (not prose), the risk is lower,
/// but system prompt content could leak via advertencias/mensaje_usuario fields.
/// RED TEAM v2: Full pattern parity with RecoAgent/SupportAgent + SearchAgent-specific patterns.
/// </summary>
public static class PromptInjectionDetector
{
    // ── Unicode Homoglyph Mapping (Cyrillic → Latin) ─────────────────
    private static readonly Dictionary<char, char> HomoglyphMap = new()
    {
        {'а', 'a'}, {'е', 'e'}, {'о', 'o'}, {'р', 'p'}, {'с', 'c'},
        {'у', 'y'}, {'х', 'x'}, {'А', 'A'}, {'В', 'B'}, {'Е', 'E'},
        {'К', 'K'}, {'М', 'M'}, {'Н', 'H'}, {'О', 'O'}, {'Р', 'P'},
        {'С', 'C'}, {'Т', 'T'}, {'Х', 'X'},
    };

    // ── System Role Impersonation (9 patterns — parity with RecoAgent) ──
    private static readonly Regex[] SystemRolePatterns =
    {
        new(@"\[SYSTEM\]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|system\|>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|im_start\|>system", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"###\s*(?:System|Instruction|Instructions)\s*:", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|begin_of_text\|>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|start_header_id\|>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<\|eot_id\|>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\[INST\]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"<<SYS>>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Instruction Override Attempts (9 patterns — parity with RecoAgent) ──
    private static readonly Regex[] OverridePatterns =
    {
        new(@"ignor[ae]\s+(?:todas?\s+)?(?:las?\s+)?instrucciones?\s+anteriores?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"ignore\s+(?:all\s+)?(?:previous|prior|above)\s+instructions?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"nuevas?\s+instrucciones?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"new\s+instructions?\s*(?:override|replace)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"olvida\s+(?:todo|las\s+reglas|tus\s+instrucciones)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"forget\s+(?:everything|all|your\s+instructions)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"override\s+(?:your\s+)?prompt", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"sobreescrib[ei]\s+(?:el\s+)?prompt", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"reset\s+(?:your\s+)?(?:instructions|context|memory)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Identity Override Attempts (5 patterns — NEW for SearchAgent) ──
    private static readonly Regex[] IdentityPatterns =
    {
        new(@"(?:ahora\s+)?(?:eres|serás|actúa\s+como|pretende\s+ser)\s+(?:un|una|el|la)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:you\s+are\s+now|act\s+as|pretend\s+to\s+be|behave\s+as)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:from\s+now\s+on|starting\s+now)\s+you", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:a\s+partir\s+de\s+ahora|desde\s+ahora)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:DAN|jailbreak|do\s+anything\s+now)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Prompt Extraction Attempts (5 patterns — parity with RecoAgent) ──
    private static readonly Regex[] ExtractionPatterns =
    {
        new(@"(?:muéstrame|dime|repite|muestra)\s+(?:tu\s+)?(?:system\s+)?prompt", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:show|tell|repeat|reveal|print)\s+(?:me\s+)?(?:your\s+)?(?:system\s+)?(?:prompt|instructions)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:cuáles?\s+son\s+tus\s+instrucciones|qué\s+instrucciones\s+tienes)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:what\s+are\s+your\s+instructions|what\s+is\s+your\s+system\s+prompt)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"output\s+(?:your\s+)?(?:initial|system|original)\s+(?:prompt|instructions|message)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── SearchAgent-Specific: Filter Manipulation Attempts ───────────
    private static readonly Regex[] FilterManipulationPatterns =
    {
        new(@"(?:devuelve|retorna|genera)\s+(?:todos?\s+)?(?:los\s+)?(?:filtros?|resultados?)\s+(?:sin\s+filtrar|sin\s+restricci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:return|output)\s+(?:all\s+)?(?:results?|data)\s+(?:without\s+filters?|unfiltered)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"precio_m[ií]nimo\s*[:=]+\s*0", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"confianza\s*[:=]+\s*1\.?0*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:elimina|ignora|quita)\s+(?:el\s+)?(?:filtro|límite|restricción)\s+de\s+(?:precio|año|marca)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// Detects prompt injection patterns in the search query.
    /// Returns structured result with threat level and pattern details.
    /// RED TEAM v2: Now includes identity override detection and threat classification.
    /// </summary>
    public static PromptInjectionResult Detect(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return PromptInjectionResult.Safe();

        // Normalize Unicode to defeat homoglyph and zero-width bypasses
        var normalizedQuery = NormalizeForDetection(query);
        var detectedPatterns = new List<string>();

        foreach (var pattern in SystemRolePatterns)
            if (pattern.IsMatch(normalizedQuery))
                detectedPatterns.Add($"system_role:{pattern}");

        foreach (var pattern in OverridePatterns)
            if (pattern.IsMatch(normalizedQuery))
                detectedPatterns.Add($"override:{pattern}");

        foreach (var pattern in IdentityPatterns)
            if (pattern.IsMatch(normalizedQuery))
                detectedPatterns.Add($"identity:{pattern}");

        foreach (var pattern in ExtractionPatterns)
            if (pattern.IsMatch(normalizedQuery))
                detectedPatterns.Add($"extraction:{pattern}");

        foreach (var pattern in FilterManipulationPatterns)
            if (pattern.IsMatch(normalizedQuery))
                detectedPatterns.Add($"filter_manipulation:{pattern}");

        if (detectedPatterns.Count == 0)
            return PromptInjectionResult.Safe();

        // Determine threat level (parity with RecoAgent/SupportAgent)
        var hasSystemRole = detectedPatterns.Any(p => p.StartsWith("system_role:"));
        var hasOverride = detectedPatterns.Any(p => p.StartsWith("override:"));
        var hasIdentity = detectedPatterns.Any(p => p.StartsWith("identity:"));
        var hasExtraction = detectedPatterns.Any(p => p.StartsWith("extraction:"));
        var hasFilterManipulation = detectedPatterns.Any(p => p.StartsWith("filter_manipulation:"));

        var threatLevel = (hasSystemRole || hasOverride) ? ThreatLevel.High
            : (hasIdentity || hasExtraction || hasFilterManipulation) ? ThreatLevel.Medium
            : ThreatLevel.Low;

        return new PromptInjectionResult
        {
            IsInjectionDetected = threatLevel >= ThreatLevel.Medium,
            ThreatLevel = threatLevel,
            DetectedPatterns = detectedPatterns,
            ShouldBlock = threatLevel >= ThreatLevel.High,
        };
    }

    /// <summary>
    /// Normalizes text for injection detection: NFKC normalization,
    /// zero-width character removal, and Cyrillic→Latin homoglyph mapping.
    /// </summary>
    private static string NormalizeForDetection(string input)
    {
        // Step 1: NFKC normalization (handles fullwidth chars, etc.)
        var normalized = input.Normalize(NormalizationForm.FormKC);

        // Step 2: Remove zero-width characters used to evade pattern matching
        normalized = Regex.Replace(normalized, @"[\u200B-\u200F\u202A-\u202E\u2060\uFEFF\u00AD]", "");

        // Step 3: Map Cyrillic homoglyphs to Latin equivalents
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            sb.Append(HomoglyphMap.TryGetValue(c, out var mapped) ? mapped : c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Sanitizes known injection tokens from the query (used for Medium threats).
    /// Strips system role tokens and instruction override markers.
    /// </summary>
    public static string Sanitize(string query)
    {
        var result = query;

        // Strip special tokens that the model interprets as control
        result = Regex.Replace(result, @"<\|[^|]+\|>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[SYSTEM\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[INST\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<<SYS>>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"###\s*(?:System|Instruction):", "", RegexOptions.IgnoreCase);

        // Strip filter manipulation JSON-like patterns
        result = Regex.Replace(result, @"precio_m[ií]nimo\s*[:=]+\s*\d+", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"confianza\s*[:=]+\s*[01]\.?\d*", "", RegexOptions.IgnoreCase);

        return result.Trim();
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

/// <summary>
/// Result of prompt injection detection for SearchAgent.
/// RED TEAM v2: Now includes ThreatLevel (parity with RecoAgent/SupportAgent).
/// </summary>
public class PromptInjectionResult
{
    public bool IsInjectionDetected { get; set; }
    public ThreatLevel ThreatLevel { get; set; }
    public List<string> DetectedPatterns { get; set; } = new();
    public bool ShouldBlock { get; set; }

    public static PromptInjectionResult Safe() => new()
    {
        IsInjectionDetected = false,
        ThreatLevel = ThreatLevel.None,
        ShouldBlock = false,
    };
}

/// <summary>
/// Threat level for prompt injection detection.
/// </summary>
public enum ThreatLevel
{
    None = 0,
    Low = 1,      // Log but allow
    Medium = 2,   // Identity override, extraction, or filter manipulation — sanitize then allow
    High = 3,     // System role/instruction override — block
}
