namespace AuthService.Application.DTOs.Auth;

// RefreshToken is optional — logout falls back to the okla_refresh_token HttpOnly cookie.
public record LogoutRequest(string? RefreshToken = null);
