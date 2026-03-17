namespace AuthService.Application.DTOs.Auth;

// RefreshToken is optional so the controller can fall back to the
// okla_refresh_token HttpOnly cookie when the body is empty ({}).
// The frontend never sends the refresh token in the body because it
// is stored exclusively in an HttpOnly cookie (XSS-safe).
public record RefreshTokenRequest(string? RefreshToken = null);
