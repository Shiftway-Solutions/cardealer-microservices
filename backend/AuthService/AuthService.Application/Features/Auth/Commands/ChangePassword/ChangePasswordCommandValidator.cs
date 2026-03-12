using FluentValidation;
using AuthService.Application.Validators;

namespace AuthService.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Validator para ChangePasswordCommand.
/// Implementa validaciones de seguridad OWASP para contraseñas.
/// </summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    // Lista de contraseñas comunes prohibidas (OWASP)
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password123", "123456", "12345678", "qwerty", "abc123",
        "monkey", "1234567", "letmein", "trustno1", "dragon", "baseball",
        "iloveyou", "master", "sunshine", "ashley", "michael", "shadow",
        "123123", "654321", "superman", "qazwsx", "welcome", "admin",
        "Password1!", "P@ssw0rd", "P@ssword123", "Qwerty123!", "Admin123!"
    };

    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .MaximumLength(50).WithMessage("Invalid user ID format.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.")
            .MaximumLength(128).WithMessage("Password exceeds maximum length.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\\/~`]")
                .WithMessage("Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>_-+=[]\\~/`).")
            .Must(NotBeCommonPassword).WithMessage("This password is too common. Please choose a more secure password.")
            .Must((cmd, newPwd) => newPwd != cmd.CurrentPassword)
                .WithMessage("New password must be different from current password.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");

        // Validaciones de seguridad adicionales
        RuleFor(x => x.NewPassword)
            .NoSqlInjection()
            .NoXss();

        // Validar longitud de secuencias repetitivas (e.g., "aaaa", "1111")
        RuleFor(x => x.NewPassword)
            .Must(NoRepeatingCharacters)
            .WithMessage("Password must not contain more than 3 consecutive identical characters.");

        // Validar que no contenga el userId
        RuleFor(x => x)
            .Must(x => !x.NewPassword.Contains(x.UserId, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Password must not contain your user ID.");
    }

    private static bool NotBeCommonPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return true;

        return !CommonPasswords.Contains(password);
    }

    private static bool NoRepeatingCharacters(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 4)
            return true;

        for (int i = 0; i <= password.Length - 4; i++)
        {
            if (password[i] == password[i + 1] &&
                password[i] == password[i + 2] &&
                password[i] == password[i + 3])
            {
                return false;
            }
        }
        return true;
    }
}
