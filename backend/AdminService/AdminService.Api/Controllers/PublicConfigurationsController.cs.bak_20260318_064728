using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Public-facing endpoint for platform configurations.
/// Serves the route /api/configurations/category/{category} that the frontend
/// SiteConfigProvider calls on every page load (unauthenticated).
/// 
/// The admin-only CRUD operations remain on ConfigurationsController
/// at /api/admin/configurations.
/// </summary>
[ApiController]
[Route("api/configurations")]
[Produces("application/json")]
public class PublicConfigurationsController : ControllerBase
{
    private static readonly Dictionary<string, List<ConfigKeyValue>> _configsByCategory = new()
    {
        ["general"] = new()
        {
            new("platform.name", "OKLA"),
            new("platform.url", "https://okla.com.do"),
            new("platform.currency", "DOP"),
            new("platform.country", "DO"),
            new("platform.locale", "es-DO"),
            new("platform.contact_email", "info@okla.com.do"),
            new("platform.whatsapp_number", "+18092001234"),
            new("platform.timezone", "America/Santo_Domingo"),
        },
        ["billing"] = new()
        {
            new("listing.price", "29"),
            new("listing.currency", "DOP"),
        },
        ["media"] = new()
        {
            new("media.max_upload_size_mb", "100"),
            new("media.max_images_per_vehicle", "12"),
        },
    };

    /// <summary>
    /// Get configurations by category (public, no auth required).
    /// Used by SiteConfigProvider on every page load.
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetByCategory(string category, [FromQuery] string? environment = null)
    {
        if (_configsByCategory.TryGetValue(category.ToLowerInvariant(), out var configs))
            return Ok(configs);

        // Return empty array for unknown categories (frontend handles gracefully)
        return Ok(Array.Empty<ConfigKeyValue>());
    }
}

/// <summary>DTO matching frontend expectation: { key, value }</summary>
public record ConfigKeyValue(string Key, string Value);
