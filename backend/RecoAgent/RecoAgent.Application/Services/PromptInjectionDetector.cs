using System.Text;
using System.Text.RegularExpressions;

namespace RecoAgent.Application.Services;

/// <summary>
/// Detects prompt injection attacks in recommendation requests before sending to the LLM.
/// Primary attack vector: the InstruccionesAdicionales field, which is user-supplied free text
/// that gets injected into the Claude prompt.
/// Ported from ChatbotService/SearchAgent with RecoAgent-specific patterns.
/// Constitutional AI: Applies Unicode normalization to prevent homoglyph bypass.
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

    // ── System Role Impersonation ────────────────────────────────────
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

    // ── Instruction Override Attempts ────────────────────────────────
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

    // ── Identity Override Attempts ───────────────────────────────────
    private static readonly Regex[] IdentityPatterns =
    {
        new(@"(?:ahora\s+)?(?:eres|serás|actúa\s+como|pretende\s+ser)\s+(?:un|una|el|la)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:you\s+are\s+now|act\s+as|pretend\s+to\s+be|behave\s+as)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:from\s+now\s+on|starting\s+now)\s+you", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:a\s+partir\s+de\s+ahora|desde\s+ahora)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:DAN|jailbreak|do\s+anything\s+now)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── Prompt Extraction Attempts ───────────────────────────────────
    private static readonly Regex[] ExtractionPatterns =
    {
        new(@"(?:muéstrame|dime|repite|muestra)\s+(?:tu\s+)?(?:system\s+)?prompt", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:show|tell|repeat|reveal|print)\s+(?:me\s+)?(?:your\s+)?(?:system\s+)?(?:prompt|instructions)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:cuáles?\s+son\s+tus\s+instrucciones|qué\s+instrucciones\s+tienes)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:what\s+are\s+your\s+instructions|what\s+is\s+your\s+system\s+prompt)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"output\s+(?:your\s+)?(?:initial|system|original)\s+(?:prompt|instructions|message)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // ── RecoAgent-Specific: Ranking/Score Manipulation ───────────────
    private static readonly Regex[] RankingManipulationPatterns =
    {
        new(@"(?:pon|coloca|mueve|posiciona)\s+(?:este|el|mi)\s+(?:veh[ií]culo|carro|listado)\s+(?:primero|de\s+primero|arriba)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:put|place|move|position)\s+(?:this|my)\s+(?:vehicle|car|listing)\s+(?:first|on\s+top)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:sube|aumenta|incrementa)\s+(?:(?:el|la)\s+)?(?:score|puntuación|ranking|posición)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:boost|increase|raise)\s+(?:the\s+)?(?:score|ranking|position|affinity)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"score_afinidad_perfil\s*[:=]\s*[01]\.?\d*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"es_patrocinado\s*[:=]\s*true", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:marca|elimina|quita|remove)\s+(?:la\s+)?(?:competencia|otros?\s+(?:carros?|veh[ií]culos?))", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    /// <summary>
    /// Normalizes Unicode text to prevent homoglyph bypass attacks.
    /// Converts confusable characters (e.g., Cyrillic 'а' → Latin 'a'),
    /// removes zero-width characters, and applies NFKC normalization.
    /// </summary>
    private static string NormalizeForDetection(string input)
    {
        // 1. Unicode NFKC normalization — collapses compatibility equivalents
        var normalized = input.Normalize(NormalizationForm.FormKC);

        // 2. Remove zero-width characters used to bypass detection
        normalized = Regex.Replace(normalized, @"[\u200B-\u200F\u202A-\u202E\u2060\uFEFF\u00AD]", "");

        // 3. Map common Cyrillic homoglyphs to Latin equivalents
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            sb.Append(HomoglyphMap.TryGetValue(c, out var mapped) ? mapped : c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Detects prompt injection patterns in the recommendation request.
    /// Primary check target: InstruccionesAdicionales (user-supplied free text).
    /// Also checks candidate data fields for indirect injection via RAG-like vectors.
    /// Constitutional AI: Applies Unicode normalization + blocks extraction + ranking manipulation.
    /// </summary>
    public static PromptInjectionResult Detect(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return PromptInjectionResult.Safe();

        // Normalize to prevent Unicode/homoglyph bypass attacks
        var normalized = NormalizeForDetection(message);

        var detectedPatterns = new List<string>();

        // Check system role impersonation (HIGH severity — block)
        foreach (var pattern in SystemRolePatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"system_role:{pattern}");

        // Check instruction override (HIGH severity — block)
        foreach (var pattern in OverridePatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"override:{pattern}");

        // Check identity override (MEDIUM severity — sanitize)
        foreach (var pattern in IdentityPatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"identity:{pattern}");

        // Check prompt extraction (MEDIUM severity — block system prompt leakage)
        foreach (var pattern in ExtractionPatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"extraction:{pattern}");

        // Check ranking/score manipulation (MEDIUM severity — RecoAgent-specific)
        foreach (var pattern in RankingManipulationPatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"ranking_manipulation:{pattern}");

        if (detectedPatterns.Count == 0)
            return PromptInjectionResult.Safe();

        // Determine threat level
        var hasSystemRole = detectedPatterns.Any(p => p.StartsWith("system_role:"));
        var hasOverride = detectedPatterns.Any(p => p.StartsWith("override:"));
        var hasIdentity = detectedPatterns.Any(p => p.StartsWith("identity:"));
        var hasExtraction = detectedPatterns.Any(p => p.StartsWith("extraction:"));
        var hasRankingManipulation = detectedPatterns.Any(p => p.StartsWith("ranking_manipulation:"));

        var threatLevel = (hasSystemRole || hasOverride) ? ThreatLevel.High
            : (hasIdentity || hasExtraction || hasRankingManipulation) ? ThreatLevel.Medium
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
    /// Sanitizes known injection tokens from the message (used for Medium threats).
    /// Strips system role tokens and instruction override markers.
    /// </summary>
    public static string Sanitize(string message)
    {
        var result = message;

        // Strip special tokens that the model interprets as control
        result = Regex.Replace(result, @"<\|[^|]+\|>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[SYSTEM\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[INST\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<<SYS>>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"###\s*(?:System|Instruction):", "", RegexOptions.IgnoreCase);

        // Strip ranking manipulation JSON-like patterns
        result = Regex.Replace(result, @"score_afinidad_perfil\s*[:=]\s*[01]\.?\d*", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"es_patrocinado\s*[:=]\s*true", "", RegexOptions.IgnoreCase);

        return result.Trim();
    }
}

/// <summary>
/// Result of prompt injection detection for RecoAgent.
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
    Medium = 2,   // Identity override, extraction, or ranking manipulation — sanitize then allow
    High = 3,     // System role/instruction override — block
}
