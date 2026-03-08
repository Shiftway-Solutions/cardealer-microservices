using System.Text.RegularExpressions;

namespace ChatbotService.Application.Services;

/// <summary>
/// Detects prompt injection attacks in user messages before sending to the LLM.
/// Blocks attempts to override the system prompt, impersonate system roles,
/// or extract model internals.
/// </summary>
public static class PromptInjectionDetector
{
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

    /// <summary>
    /// Normalizes Unicode text to prevent homoglyph bypass attacks.
    /// Converts confusable characters (e.g., Cyrillic 'а' → Latin 'a').
    /// </summary>
    private static string NormalizeForDetection(string input)
    {
        // 1. Unicode NFKC normalization — collapses compatibility equivalents
        var normalized = input.Normalize(System.Text.NormalizationForm.FormKC);

        // 2. Remove zero-width characters used to bypass detection
        normalized = Regex.Replace(normalized, @"[\u200B-\u200F\u202A-\u202E\u2060\uFEFF]", "");

        // 3. Map common Cyrillic homoglyphs to Latin equivalents
        var homoglyphMap = new Dictionary<char, char>
        {
            {'а', 'a'}, {'е', 'e'}, {'о', 'o'}, {'р', 'p'}, {'с', 'c'},
            {'у', 'y'}, {'х', 'x'}, {'А', 'A'}, {'В', 'B'}, {'Е', 'E'},
            {'К', 'K'}, {'М', 'M'}, {'Н', 'H'}, {'О', 'O'}, {'Р', 'P'},
            {'С', 'C'}, {'Т', 'T'}, {'Х', 'X'},
        };

        var chars = normalized.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (homoglyphMap.TryGetValue(chars[i], out var replacement))
                chars[i] = replacement;
        }

        return new string(chars);
    }

    /// <summary>
    /// Analyzes a user message for prompt injection attempts.
    /// Returns detection result with threat level and matched patterns.
    /// Constitutional AI: Applies Unicode normalization to prevent homoglyph bypass.
    /// Prompt extraction attempts are now blocked (upgraded to MEDIUM severity).
    /// </summary>
    public static PromptInjectionResult Detect(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return PromptInjectionResult.Safe();

        // Normalize to prevent Unicode/homoglyph bypass attacks
        var normalized = NormalizeForDetection(message);

        var detectedPatterns = new List<string>();

        // Check system role impersonation (HIGH severity)
        foreach (var pattern in SystemRolePatterns)
        {
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"system_role:{pattern}");
        }

        // Check instruction override (HIGH severity)
        foreach (var pattern in OverridePatterns)
        {
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"override:{pattern}");
        }

        // Check identity override (MEDIUM severity)
        foreach (var pattern in IdentityPatterns)
        {
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"identity:{pattern}");
        }

        // Check prompt extraction (MEDIUM severity — block to prevent system prompt leakage)
        foreach (var pattern in ExtractionPatterns)
        {
            if (pattern.IsMatch(normalized))
                detectedPatterns.Add($"extraction:{pattern}");
        }

        if (detectedPatterns.Count == 0)
            return PromptInjectionResult.Safe();

        // Determine threat level
        var hasSystemRole = detectedPatterns.Any(p => p.StartsWith("system_role:"));
        var hasOverride = detectedPatterns.Any(p => p.StartsWith("override:"));
        var hasIdentity = detectedPatterns.Any(p => p.StartsWith("identity:"));
        var hasExtraction = detectedPatterns.Any(p => p.StartsWith("extraction:"));

        // Extraction upgraded from Low → Medium to prevent system prompt leakage
        var threatLevel = (hasSystemRole || hasOverride) ? ThreatLevel.High
            : (hasIdentity || hasExtraction) ? ThreatLevel.Medium
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
    /// Scans RAG context data (vehicle descriptions, dealer info) for indirect injection.
    /// Constitutional AI: Prevents malicious content in inventory data from manipulating the LLM.
    /// Returns sanitized context with injection tokens stripped.
    /// </summary>
    public static string SanitizeRagContext(string ragContext)
    {
        if (string.IsNullOrWhiteSpace(ragContext))
            return ragContext;

        var result = ragContext;

        // Strip all system role tokens that could be embedded in vehicle descriptions
        result = Regex.Replace(result, @"<\|[^|]+\|>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[SYSTEM\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\[INST\]", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<<SYS>>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"###\s*(?:System|Instruction):", "", RegexOptions.IgnoreCase);

        // Strip instruction override patterns from vehicle descriptions
        result = Regex.Replace(result, @"ignor[ae]\s+(?:todas?\s+)?(?:las?\s+)?instrucciones?\s+anteriores?", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"ignore\s+(?:all\s+)?(?:previous|prior|above)\s+instructions?", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"nuevas?\s+instrucciones?", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"olvida\s+(?:todo|las\s+reglas|tus\s+instrucciones)", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"override\s+(?:your\s+)?prompt", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"reset\s+(?:your\s+)?(?:instructions|context|memory)", "", RegexOptions.IgnoreCase);

        return result.Trim();
    }

    /// <summary>
    /// Sanitizes a message by stripping known injection tokens.
    /// Used when threat level is Medium (allow but sanitize).
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

        return result.Trim();
    }
}

/// <summary>
/// Result of prompt injection detection.
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
    Low = 1,      // Extraction attempt — log but allow
    Medium = 2,   // Identity override — sanitize then allow
    High = 3,     // System role/instruction override — block
}
