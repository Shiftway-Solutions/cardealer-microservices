using FluentValidation;

namespace ServiceDiscovery.Application.Validators;

// ============================================
// Security Validators (SQL Injection, XSS)
// ============================================
public static class SecurityValidators
{
    private static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
        "EXEC", "EXECUTE", "XP_", "SP_", "UNION", "DECLARE", "CAST", "CONVERT",
        "--", "/*", "*/", "INFORMATION_SCHEMA", "SYSOBJECTS", "SYSCOLUMNS",
        "WAITFOR DELAY", "BENCHMARK", "SLEEP(", "OR 1=1", "OR '1'='1'"
    };

    private static readonly string[] XssPatterns = new[]
    {
        "<script", "</script>", "javascript:", "vbscript:", "onerror=", "onload=",
        "onclick=", "onmouseover=", "onfocus=", "onblur=", "<iframe", "</iframe>",
        "<object", "<embed", "<svg", "eval(", "expression(", "alert(", "confirm(",
        "prompt(", "data:text/html", "onanimationstart=", "onanimationend=", "ontransitionend="
    };

    public static IRuleBuilderOptions<T, string> NoSqlInjection<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(input =>
        {
            if (string.IsNullOrWhiteSpace(input)) return true;
            var upperInput = input.ToUpperInvariant();
            return !SqlKeywords.Any(keyword => upperInput.Contains(keyword));
        })
        .WithMessage("Input contains potential SQL injection patterns.");
    }

    public static IRuleBuilderOptions<T, string> NoXss<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(input =>
        {
            if (string.IsNullOrWhiteSpace(input)) return true;
            var lowerInput = input.ToLowerInvariant();
            return !XssPatterns.Any(pattern => lowerInput.Contains(pattern));
        })
        .WithMessage("Input contains potential XSS attack patterns.");
    }
}
