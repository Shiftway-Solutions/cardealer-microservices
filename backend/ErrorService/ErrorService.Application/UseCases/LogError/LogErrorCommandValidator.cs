using FluentValidation;
using ErrorService.Application.DTOs;
using ErrorService.Application.Validators;
using System.Text.RegularExpressions;

namespace ErrorService.Application.UseCases.LogError
{
    /// <summary>
    /// Validador robusto para LogErrorRequest con reglas de seguridad y sanitización.
    /// Uses shared SecurityValidators (NoSqlInjection/NoXss) instead of inline patterns.
    /// </summary>
    public class LogErrorCommandValidator : AbstractValidator<LogErrorRequest>
    {
        private static readonly Regex ServiceNameRegex = new(@"^[a-zA-Z0-9\-_.]+$", RegexOptions.Compiled);
        private static readonly Regex HttpMethodRegex = new(@"^(GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS|TRACE)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex EndpointRegex = new(@"^[a-zA-Z0-9\-_/:.?&=]+$", RegexOptions.Compiled);

        public LogErrorCommandValidator()
        {
            // ServiceName - REQUERIDO y con formato estricto
            RuleFor(x => x.ServiceName)
                .NotEmpty().WithMessage("ServiceName es requerido")
                .MaximumLength(100).WithMessage("ServiceName no puede exceder 100 caracteres")
                .Matches(ServiceNameRegex).WithMessage("ServiceName solo puede contener letras, números, guiones, puntos y guiones bajos")
                .NoSqlInjection()
                .NoXss();

            // ExceptionType - REQUERIDO + security validation
            RuleFor(x => x.ExceptionType)
                .NotEmpty().WithMessage("ExceptionType es requerido")
                .MaximumLength(200).WithMessage("ExceptionType no puede exceder 200 caracteres")
                .NoSqlInjection()
                .NoXss();

            // Message - REQUERIDO con límite de tamaño + security validation
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message es requerido")
                .MaximumLength(5000).WithMessage("Message no puede exceder 5000 caracteres (demasiado largo)")
                .NoSqlInjection()
                .NoXss();

            // StackTrace - OPCIONAL pero con límite + security validation
            RuleFor(x => x.StackTrace)
                .MaximumLength(50000).WithMessage("StackTrace no puede exceder 50000 caracteres")
                .NoXss()
                .NoSqlInjection()
                .When(x => !string.IsNullOrWhiteSpace(x.StackTrace));

            // Endpoint - OPCIONAL con formato validado + security validation
            RuleFor(x => x.Endpoint)
                .MaximumLength(500).WithMessage("Endpoint no puede exceder 500 caracteres")
                .Matches(EndpointRegex).WithMessage("Endpoint contiene caracteres inválidos")
                .NoSqlInjection()
                .NoXss()
                .When(x => !string.IsNullOrWhiteSpace(x.Endpoint));

            // HttpMethod - OPCIONAL pero con valores permitidos
            RuleFor(x => x.HttpMethod)
                .MaximumLength(10).WithMessage("HttpMethod no puede exceder 10 caracteres")
                .Matches(HttpMethodRegex).WithMessage("HttpMethod debe ser un método HTTP válido (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS, TRACE)")
                .When(x => !string.IsNullOrWhiteSpace(x.HttpMethod));

            // UserId - OPCIONAL con límite
            RuleFor(x => x.UserId)
                .MaximumLength(100).WithMessage("UserId no puede exceder 100 caracteres")
                .NoSqlInjection()
                .NoXss()
                .When(x => !string.IsNullOrWhiteSpace(x.UserId));

            // StatusCode - OPCIONAL pero con rango válido
            RuleFor(x => x.StatusCode)
                .InclusiveBetween(100, 599).WithMessage("StatusCode debe estar entre 100 y 599")
                .When(x => x.StatusCode.HasValue);

            // Metadata - OPCIONAL pero con límite de tamaño
            RuleFor(x => x.Metadata)
                .Must(metadata => metadata == null || metadata.Count <= 50)
                .WithMessage("Metadata no puede contener más de 50 entradas")
                .When(x => x.Metadata != null);

            // Validación de tamaño total de Metadata
            RuleFor(x => x.Metadata)
                .Must(BeValidMetadataSize)
                .WithMessage("El tamaño total de Metadata no puede exceder 10KB")
                .When(x => x.Metadata != null && x.Metadata.Any());

            // Metadata keys/values — security validation against XSS/SQLi
            RuleFor(x => x.Metadata)
                .Must(BeSecureMetadata)
                .WithMessage("Metadata keys or values contain potential security threats (XSS/SQLi patterns)")
                .When(x => x.Metadata != null && x.Metadata.Any());
        }

        /// <summary>
        /// XSS/SQLi patterns to check in Metadata keys and values.
        /// </summary>
        private static readonly string[] s_dangerousPatterns = new[]
        {
            "<script", "</script>", "javascript:", "onerror=", "onload=", "onclick=",
            "<iframe", "<object", "<embed", "eval(", "expression(", "vbscript:",
            "SELECT ", "INSERT ", "UPDATE ", "DELETE ", "DROP ", "UNION ", "--", "/*", "*/",
            "xp_", "sp_", "EXEC ", "EXECUTE "
        };

        /// <summary>
        /// Validates that Metadata keys and values do not contain XSS/SQLi patterns.
        /// </summary>
        private bool BeSecureMetadata(Dictionary<string, object>? metadata)
        {
            if (metadata == null || !metadata.Any())
                return true;

            foreach (var kvp in metadata)
            {
                var key = kvp.Key?.ToUpperInvariant() ?? string.Empty;
                var value = kvp.Value?.ToString()?.ToUpperInvariant() ?? string.Empty;

                if (s_dangerousPatterns.Any(p => key.Contains(p, StringComparison.OrdinalIgnoreCase)
                    || value.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Valida que el tamaño total de Metadata no exceda 10KB
        /// </summary>
        private bool BeValidMetadataSize(Dictionary<string, object>? metadata)
        {
            if (metadata == null || !metadata.Any())
                return true;

            var totalSize = metadata.Sum(kvp =>
            {
                var keySize = kvp.Key?.Length ?? 0;
                var valueSize = kvp.Value?.ToString()?.Length ?? 0;
                return keySize + valueSize;
            });

            return totalSize <= 10240; // 10KB
        }
    }
}
