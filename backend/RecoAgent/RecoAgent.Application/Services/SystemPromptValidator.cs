using System.Text.RegularExpressions;

namespace RecoAgent.Application.Services;

/// <summary>
/// Validates admin-supplied system prompt overrides to prevent compromised admin accounts
/// from injecting malicious instructions into the LLM system prompt.
/// RED TEAM v2: Defense-in-depth for the SystemPromptOverride attack surface.
/// </summary>
public static class SystemPromptValidator
{
    // Maximum allowed system prompt length (prevent token exhaustion attacks)
    private const int MaxPromptLength = 10_000;

    // Forbidden patterns that should never appear in a legitimate system prompt override
    private static readonly Regex[] ForbiddenPatterns =
    {
        // Attempting to disable safety features (allow intermediate Spanish articles: la, las, el, los, de)
        new(@"(?:ignor[ae]|desactiva|disable|remove)\s+(?:\w+\s+){0,3}(?:safety|security|validation|seguridad|validaci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Attempting to exfiltrate data
        new(@"(?:send|envía|transmit|exfiltrate)\s+(?:data|datos|information|logs)\s+(?:to|a)\s+(?:https?://|ftp://)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Attempting to execute commands
        new(@"(?:execute|ejecuta|run|correr)\s+(?:command|comando|shell|bash|system)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Attempting to change response format to leak data
        new(@"(?:include|incluye|add|agrega)\s+(?:raw|full|complete|todo)\s+(?:system\s+prompt|instrucciones|configuration|configuraci[oó]n)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Attempting to bypass JSON-only output
        new(@"(?:respond|responde)\s+(?:in\s+)?(?:HTML|markdown|XML|plain\s+text)\s+(?:instead|en\s+vez)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    // Required keywords that a legitimate RecoAgent system prompt should contain
    private static readonly string[] RequiredKeywords =
    {
        "JSON",         // Must output JSON
        "vehículo",     // Must be about vehicles (or vehiculo)
    };

    /// <summary>
    /// Validates a system prompt override. Returns null if valid, or an error message if invalid.
    /// </summary>
    public static string? Validate(string? promptOverride)
    {
        if (string.IsNullOrWhiteSpace(promptOverride))
            return null; // null/empty override is valid (falls back to auto-built prompt)

        // Length check
        if (promptOverride.Length > MaxPromptLength)
            return $"System prompt override exceeds maximum length ({MaxPromptLength} chars)";

        // Forbidden patterns check
        foreach (var pattern in ForbiddenPatterns)
        {
            if (pattern.IsMatch(promptOverride))
                return $"System prompt override contains forbidden pattern: {pattern}";
        }

        // Must contain at least one required keyword (basic relevance check)
        var hasRequiredKeyword = RequiredKeywords.Any(kw =>
            promptOverride.Contains(kw, StringComparison.OrdinalIgnoreCase));

        if (!hasRequiredKeyword)
            return "System prompt override must contain vehicle-related content and JSON output instruction";

        return null; // Valid
    }
}
