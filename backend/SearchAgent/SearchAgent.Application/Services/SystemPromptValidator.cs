using System.Text.RegularExpressions;

namespace SearchAgent.Application.Services;

/// <summary>
/// Validates admin-supplied system prompt overrides to prevent compromised admin accounts
/// from injecting malicious instructions into the LLM system prompt.
/// RED TEAM v2: Defense-in-depth for the SystemPromptOverride attack surface.
/// </summary>
public static class SystemPromptValidator
{
    private const int MaxPromptLength = 15_000; // SearchAgent prompt is longer (includes slang glossary)

    private static readonly Regex[] ForbiddenPatterns =
    {
        new(@"(?:ignor[ae]|desactiva|disable|remove)\s+(?:safety|security|validation|seguridad|validaci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:send|envía|transmit|exfiltrate)\s+(?:data|datos|information|logs)\s+(?:to|a)\s+(?:https?://|ftp://)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:execute|ejecuta|run|correr)\s+(?:command|comando|shell|bash|system)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:include|incluye|add|agrega)\s+(?:raw|full|complete|todo)\s+(?:system\s+prompt|instrucciones|configuration|configuraci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?:respond|responde)\s+(?:in\s+)?(?:HTML|markdown|XML|plain\s+text)\s+(?:instead|en\s+vez)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    private static readonly string[] RequiredKeywords =
    {
        "JSON",         // Must output JSON
        "filtro",       // Must be about search filters (or vehículo)
    };

    /// <summary>
    /// Validates a system prompt override. Returns null if valid, or an error message if invalid.
    /// </summary>
    public static string? Validate(string? promptOverride)
    {
        if (string.IsNullOrWhiteSpace(promptOverride))
            return null;

        if (promptOverride.Length > MaxPromptLength)
            return $"System prompt override exceeds maximum length ({MaxPromptLength} chars)";

        foreach (var pattern in ForbiddenPatterns)
        {
            if (pattern.IsMatch(promptOverride))
                return $"System prompt override contains forbidden pattern: {pattern}";
        }

        var hasRequiredKeyword = RequiredKeywords.Any(kw =>
            promptOverride.Contains(kw, StringComparison.OrdinalIgnoreCase));

        if (!hasRequiredKeyword)
            return "System prompt override must contain search filter-related content and JSON output instruction";

        return null;
    }
}
