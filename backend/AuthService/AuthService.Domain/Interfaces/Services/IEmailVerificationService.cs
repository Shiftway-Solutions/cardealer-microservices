using AuthService.Domain.Entities;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces.Services;

/// <summary>
/// Resultado de la verificación de email
/// </summary>
public record EmailVerificationResult(
    bool Success,
    string? UserId = null,
    string? Email = null,
    string? UserName = null
);

public interface IEmailVerificationService
{
    Task SendVerificationEmailAsync(ApplicationUser user);

    /// <summary>
    /// Verifica el token de email. Retorna información del usuario si es exitoso.
    /// </summary>
    Task<EmailVerificationResult> VerifyAsync(string token);
}
