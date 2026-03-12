using AuthService.Application.DTOs.Auth;
using MediatR;

namespace AuthService.Application.Features.Auth.Commands.Login;

/// <summary>
/// US-18.3: Login command with optional CAPTCHA token.
/// CaptchaToken is required after 2+ failed login attempts.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string? CaptchaToken = null
) : IRequest<LoginResponse>;
