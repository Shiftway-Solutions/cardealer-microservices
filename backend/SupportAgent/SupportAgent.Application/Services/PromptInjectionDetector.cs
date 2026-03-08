using System.Text;
using System.Text.RegularExpressions;

namespace SupportAgent.Application.Services;

/// <summary>
/// Detects prompt injection attacks in user messages before sending to the LLM.
/// Ported from ChatbotService with SupportAgent-specific patterns added.
/// Blocks attempts to override the system prompt, impersonate system roles,
/// or extract model internals.
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

    // ── SupportAgent-Specific: Action Impersonation ──────────────────
    private static readonly Regex[] ActionImpersonationPatterns =
    {
        new(@"(?:activa|activar|desactiva|desactivar|elimina|eliminar|borrar|borra)\s+(?:mi|la|el)\s+(?:cuenta|perfil|suscripci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:dame|otorga|concede)\s+(?:acceso|permisos?|rol)\s+(?:de\s+)?(?:admin|administrador|dealer)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:haz|realiza|ejecuta)\s+(?:una?\s+)?(?:reembolso|pago|transferencia)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:aprueba|aprobar|rechaza|rechazar)\s+(?:(?:mi|la|el)\s+)?(?:kyc|verificaci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
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
    /// Detects prompt injection patterns in the user message.
    /// Returns a result indicating threat level and whether to block.
    /// Constitutional AI: Applies Unicode normalization to prevent homoglyph bypass.
    /// Prompt extraction attempts upgraded to MEDIUM severity (block system prompt leakage).
    /// </summary>
    public static PromptInjectionResult Detect(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return PromptInjectionResult.Safe();

        // Normalize to prevent Unicode/homoglyph bypass attacks
        var normalized = NormalizeForDetection(message);

        var detectedPatterns = new List<string>();

        foreach (var pattern in SystemRolePatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"system_role:{pattern}");

        foreach (var pattern in OverridePatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"override:{pattern}");

        foreach (var pattern in IdentityPatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"identity:{pattern}");

        foreach (var pattern in ExtractionPatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"extraction:{pattern}");

        foreach (var pattern in ActionImpersonationPatterns)
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"action_impersonation:{pattern}");

        if (detectedPatterns.Count == 0)
            return PromptInjectionResult.Safe();

        var hasSystemRole = detectedPatterns.Any(p => p.StartsWith("system_role:"));
        var hasOverride = detectedPatterns.Any(p => p.StartsWith("override:"));
        var hasIdentity = detectedPatterns.Any(p => p.StartsWith("identity:"));
        var hasExtraction = detectedPatterns.Any(p => p.StartsWith("extraction:"));
        var hasActionImpersonation = detectedPatterns.Any(p => p.StartsWith("action_impersonation:"));

        // RED TEAM v2: Action impersonation upgraded from Low → Medium (sanitize + allow)
        // Extraction upgraded from Low → Medium to prevent system prompt leakage
        var threatLevel = (hasSystemRole || hasOverride) ? ThreatLevel.High
            : (hasIdentity || hasExtraction || hasActionImpersonation) ? ThreatLevel.Medium
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
    /// </summary>
    public static string Sanitize(string message)
    {
        var result = message;
        result = Regex.Replace(result, @"<\|[^|]+\|>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[SYSTEM\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[INST\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<<SYS>>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"###\s*(?:System|Instruction):", "", RegexOptions.IgnoreCase);
        return result.Trim();
    }
}

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

public enum ThreatLevel
{
    None = 0,
    Low = 1,      // Log but allow
    Medium = 2,   // Identity override, extraction, or action impersonation — sanitize then allow
    High = 3,     // System role/instruction override — block
}
