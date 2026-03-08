using FluentValidation;
using System.Text.RegularExpressions;

namespace SearchAgent.Application.Validators;

/// <summary>
/// Custom validator to detect SQL Injection patterns in string inputs.
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

    public static IRuleBuilderOptions<T, string> NoSqlInjection<T>(this IRuleBuilder<T, string> ruleBuilder)
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

    public static IRuleBuilderOptions<T, string> NoXss<T>(this IRuleBuilder<T, string> ruleBuilder)
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

    public static IRuleBuilderOptions<T, string> NoXssAdvanced<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        var xssRegexPatterns = new[]
        {
            @"<script[\s\S]*?>[\s\S]*?</script>",
            @"javascript\s*:",
            @"on\w+\s*=",
            @"<iframe[\s\S]*?>",
            @"<object[\s\S]*?>",
            @"<embed[\s\S]*?>",
            @"<img[\s\S]*?onerror\s*=",
            @"data:text/html",
            @"eval\s*\(",
            @"expression\s*\("
        };

        return ruleBuilder.Must(input =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            return !xssRegexPatterns.Any(pattern =>
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
        })
        .WithMessage("Input contains potential XSS attack patterns and is not allowed.");
    }
}

/// <summary>
/// Combined security validator for both SQL Injection and XSS.
/// </summary>
public static class SecurityValidator
{
    public static IRuleBuilderOptions<T, string> NoSecurityThreats<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NoSqlInjection()
            .NoXss();
    }
}
