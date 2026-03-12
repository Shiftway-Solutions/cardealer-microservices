using System.Text.Json.Serialization;

namespace AuthService.Application.DTOs.Auth;

public record ResetPasswordRequest(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("newPassword")] string NewPassword,
    [property: JsonPropertyName("confirmPassword")] string ConfirmPassword);
