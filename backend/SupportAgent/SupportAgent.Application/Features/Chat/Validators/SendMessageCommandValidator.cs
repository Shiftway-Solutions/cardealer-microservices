using System.Text.RegularExpressions;
using FluentValidation;
using SupportAgent.Application.Features.Chat.Commands;

namespace SupportAgent.Application.Features.Chat.Validators;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MinimumLength(1).WithMessage("El mensaje debe tener al menos 1 carácter.")
            .MaximumLength(2000).WithMessage("El mensaje no puede exceder 2000 caracteres.")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.SessionId)
            .MaximumLength(64).When(x => x.SessionId != null)
            .WithMessage("El ID de sesión no puede exceder 64 caracteres.");
    }
}

/// <summary>
/// Security validators for FluentValidation — prevents SQL injection and XSS attacks.
/// Ported from AuthService per project coding standards.
/// </summary>
public static class SecurityValidators
{
    // SQL injection patterns
    private static readonly Regex SqlInjectionPattern = new(
        @"('|--|;|\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|ALTER|CREATE|EXEC|EXECUTE|xp_|sp_)\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // XSS patterns
    private static readonly Regex XssPattern = new(
        @"(<\s*script|javascript\s*:|on\w+\s*=|<\s*iframe|<\s*object|<\s*embed|<\s*form|<\s*input|<\s*img\s+[^>]*onerror)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IRuleBuilderOptions<T, string> NoSqlInjection<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || !SqlInjectionPattern.IsMatch(value))
            .WithMessage("El mensaje contiene patrones no permitidos.");
    }

    public static IRuleBuilderOptions<T, string> NoXss<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || !XssPattern.IsMatch(value))
            .WithMessage("El mensaje contiene contenido no permitido.");
    }
}
