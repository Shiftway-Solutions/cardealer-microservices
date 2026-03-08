using FluentValidation;
using System.Text.RegularExpressions;

namespace ErrorService.Application.Validators;

/// <summary>
/// Custom validator to detect SQL Injection patterns in string inputs.
/// Aligned with AuthService reference implementation (string? nullable support).
/// </summary>
public static class SqlInjectionValidator
{
    private static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
        "EXEC", "EXECUTE", "UNION", "DECLARE", "CAST", "CONVERT",
        "--", "/*", "*/", "xp_", "sp_", "INFORMATION_SCHEMA", "SYSOBJECTS",
        "SYSCOLUMNS", "TABLE_NAME", "COLUMN_NAME", "OR 1=1", "OR '1'='1'",
        "WAITFOR DELAY", "BENCHMARK", "SLEEP("
    };

    /// <summary>
    /// Validates that the input does not contain SQL injection patterns.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NoSqlInjection<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(input =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            var upperInput = input.ToUpperInvariant();
            return !SqlKeywords.Any(keyword => upperInput.Contains(keyword));
        })
        .WithMessage("Input contains potential SQL injection patterns and is not allowed.");
    }
}

/// <summary>
/// Custom validator to detect XSS (Cross-Site Scripting) patterns in string inputs.
/// Aligned with AuthService reference implementation (string? nullable, pre-compiled regex).
/// </summary>
public static class XssValidator
{
    private static readonly string[] XssPatterns = new[]
    {
        "<script", "</script>", "javascript:", "onerror=", "onload=", "onclick=",
        "onmouseover=", "onfocus=", "onblur=", "<iframe", "</iframe>", "<object",
        "</object>", "<embed", "</embed>", "eval(", "expression(", "vbscript:",
        "data:text/html", "<svg", "onanimationstart=", "onanimationend=",
        "ontransitionend=", "<img", "src=", "alert(", "confirm(", "prompt("
    };

    /// <summary>
    /// Validates that the input does not contain XSS attack patterns.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NoXss<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(input =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            var lowerInput = input.ToLowerInvariant();
            return !XssPatterns.Any(pattern => lowerInput.Contains(pattern));
        })
        .WithMessage("Input contains potential XSS attack patterns and is not allowed.");
    }

    // Performance: Pre-compile regex patterns once at class load, not per-invocation
    private static readonly Regex[] s_xssRegexPatterns = new[]
    {
        new Regex(@"<script[\s\S]*?>[\s\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<iframe[\s\S]*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<object[\s\S]*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<embed[\s\S]*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<img[\s\S]*?onerror\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"data:text/html", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"eval\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"expression\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    /// <summary>
    /// Advanced XSS validation using pre-compiled regex patterns.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NoXssAdvanced<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(input =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            return !s_xssRegexPatterns.Any(regex => regex.IsMatch(input));
        })
        .WithMessage("Input contains potential XSS attack patterns and is not allowed.");
    }
}

/// <summary>
/// Combined security validator for both SQL Injection and XSS.
/// </summary>
public static class SecurityValidator
{
    /// <summary>
    /// Validates input against both SQL Injection and XSS patterns.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NoSecurityThreats<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NoSqlInjection()
            .NoXss();
    }
}
