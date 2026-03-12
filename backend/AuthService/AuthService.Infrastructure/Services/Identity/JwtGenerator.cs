using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Services.Identity;

public class JwtGenerator : IJwtGenerator
{
    // Performance: Reuse handler and credentials — both are thread-safe
    private static readonly JwtSecurityTokenHandler s_tokenHandler = new();
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
        };
    }

    public string GenerateToken(ApplicationUser user)
    {
        return GenerateToken(user, null, null);
    }

    /// <inheritdoc />
    public string GenerateToken(ApplicationUser user, int? expiresMinutes)
    {
        return GenerateToken(user, expiresMinutes, null);
    }

    /// <inheritdoc />
    public string GenerateToken(ApplicationUser user, int? expiresMinutes, string? sessionId)
    {
        var effectiveExpiration = expiresMinutes ?? _jwtSettings.ExpiresMinutes;

        // Security (CWE-359): Only include minimal required claims in JWT.
        // PII (firstName, lastName, picture) and internal values (security_stamp) 
        // must NOT be in JWT — they are exposed via /api/auth/me instead.
        var claims = new List<Claim>
        {
            // Standard JWT subject claim (RFC 7519 §4.1.2)
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            // .NET identity claims — kept for middleware compatibility (ClaimTypes.NameIdentifier)
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim("email_verified", user.EmailConfirmed.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Multi-tenant: dealerId
            new Claim("dealerId", user.DealerId ?? string.Empty),
            // Account type as string for frontend compatibility (also keep int for backend services)
            new Claim("account_type", ((int)user.AccountType).ToString()),
            new Claim("accountType", user.AccountType.ToString().ToLowerInvariant()),
            // User intent - what the user wants to do
            new Claim("userIntent", user.UserIntent.ToClaimValue()),
        };

        // Embed session ID so /api/auth/security/sessions can mark isCurrent correctly
        if (!string.IsNullOrEmpty(sessionId))
        {
            claims.Add(new Claim("SessionId", sessionId));
        }

        // Add role claims based on AccountType for [Authorize(Roles = "...")] compatibility
        // AccountType: Guest=0, Buyer=1, Dealer=2, DealerEmployee=3, Admin=4, PlatformEmployee=5, Seller=6
        switch (user.AccountType)
        {
            case Domain.Enums.AccountType.Admin:
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new Claim(ClaimTypes.Role, "Compliance"));
                break;
            case Domain.Enums.AccountType.PlatformEmployee:
                claims.Add(new Claim(ClaimTypes.Role, "Compliance"));
                claims.Add(new Claim(ClaimTypes.Role, "PlatformEmployee"));
                break;
            case Domain.Enums.AccountType.Dealer:
                claims.Add(new Claim(ClaimTypes.Role, "Dealer"));
                break;
            case Domain.Enums.AccountType.DealerEmployee:
                claims.Add(new Claim(ClaimTypes.Role, "DealerEmployee"));
                break;
            case Domain.Enums.AccountType.Buyer:
                claims.Add(new Claim(ClaimTypes.Role, "User"));
                break;
            case Domain.Enums.AccountType.Seller:
                claims.Add(new Claim(ClaimTypes.Role, "User"));
                claims.Add(new Claim(ClaimTypes.Role, "Seller"));
                break;
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(effectiveExpiration),
            signingCredentials: _signingCredentials
        );

        return s_tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Performance & Security: Use cryptographic random bytes instead of Guid
        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public (string userId, string email)? ValidateToken(string token)
    {
        try
        {
            var principal = s_tokenHandler.ValidateToken(token, _validationParameters, out _);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                return null;

            return (userId, email);
        }
        catch
        {
            return null;
        }
    }

    // NUEVO: Método para generar token temporal para 2FA
    public string GenerateTempToken(string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("temp_token", "true"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5), // Corta expiración para 2FA
            signingCredentials: _signingCredentials
        );

        return s_tokenHandler.WriteToken(token);
    }

    // NUEVO: Método para validar token temporal para 2FA
    public (string userId, string email)? ValidateTempToken(string token)
    {
        try
        {
            var principal = s_tokenHandler.ValidateToken(token, _validationParameters, out _);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isTemp = principal.FindFirst("temp_token")?.Value;

            if (string.IsNullOrEmpty(userId) || isTemp != "true")
                return null;

            return (userId, ""); // Email no es necesario para token temporal
        }
        catch
        {
            return null;
        }
    }
}
