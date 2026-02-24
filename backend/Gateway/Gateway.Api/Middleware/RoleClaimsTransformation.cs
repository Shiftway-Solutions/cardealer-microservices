using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Gateway.Api.Middleware;

/// <summary>
/// Maps long Microsoft URI role claims to the short "role" claim type
/// so that Ocelot's RouteClaimsRequirement { "role": "Compliance" } works.
///
/// Background: AuthService generates JWTs using .NET Identity which stores
/// roles under the full Microsoft URI claim type:
///   http://schemas.microsoft.com/ws/2008/06/identity/claims/role
///
/// Ocelot's ClaimsAuthorizer does an exact type match on the claim key,
/// so it looks for claims with type "role" (short name) and finds nothing.
///
/// This transformer runs after JWT validation and adds short-name "role"
/// claims that mirror the long-URI ones — allowing Ocelot authorization to work.
/// </summary>
public sealed class RoleClaimsTransformation : IClaimsTransformation
{
    private const string LongRoleClaimType =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    private const string ShortRoleClaimType = "role";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // If short-name "role" claims already exist, nothing to do
        if (principal.HasClaim(c => c.Type == ShortRoleClaimType))
            return Task.FromResult(principal);

        var roleValues = principal.Claims
            .Where(c => c.Type == LongRoleClaimType)
            .Select(c => c.Value)
            .ToList();

        if (roleValues.Count == 0)
            return Task.FromResult(principal);

        // Add a new identity with short-name "role" claims
        var roleIdentity = new ClaimsIdentity();
        foreach (var role in roleValues)
        {
            roleIdentity.AddClaim(new Claim(ShortRoleClaimType, role));
        }

        principal.AddIdentity(roleIdentity);
        return Task.FromResult(principal);
    }
}
