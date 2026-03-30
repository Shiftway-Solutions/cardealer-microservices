using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for platform configuration and feature flags (consolidated from ConfigurationService).
/// TODO: Implement full CQRS handlers with MediatR when ConfigurationService logic is migrated.
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ConfigurationsController : ControllerBase
{
    private readonly ILogger<ConfigurationsController> _logger;

    // TODO: Replace with persistent storage when fully implemented
    private static readonly Dictionary<string, ConfigurationEntry> _configurations = new()
    {
        ["platform.name"] = new("platform.name", "OKLA", "Platform display name", "general", DateTime.UtcNow),
        ["platform.currency"] = new("platform.currency", "DOP", "Default currency code", "general", DateTime.UtcNow),
        ["listing.price"] = new("listing.price", "29", "Price per listing in USD", "billing", DateTime.UtcNow),
        ["media.max_upload_size_mb"] = new("media.max_upload_size_mb", "100", "Maximum upload size in MB", "media", DateTime.UtcNow),
    };

    private static readonly Dictionary<string, FeatureFlagEntry> _featureFlags = new()
    {
        ["feature.chatbot"] = new("feature.chatbot", true, "AI Chatbot feature", DateTime.UtcNow),
        ["feature.video360"] = new("feature.video360", false, "360° video uploads", DateTime.UtcNow),
        ["feature.whatsapp"] = new("feature.whatsapp", true, "WhatsApp notifications", DateTime.UtcNow),
    };

    public ConfigurationsController(ILogger<ConfigurationsController> logger)
    {
        _logger = logger;
    }

    // ========================================
    // CONFIGURATION ENDPOINTS
    // ========================================

    /// <summary>
    /// Get all platform configurations
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <returns>List of configuration entries</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] string? category = null)
    {
        _logger.LogInformation("Getting configurations with category filter: {Category}", category);

        // TODO: Replace with MediatR query when ConfigurationService logic is migrated
        var configs = _configurations.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
            configs = configs.Where(c => c.Category == category);

        return Ok(configs.ToList());
    }

    /// <summary>
    /// Get a specific configuration value by key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration entry</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByKey(string key)
    {
        _logger.LogInformation("Getting configuration for key: {Key}", key);

        if (!_configurations.TryGetValue(key, out var config))
            return NotFound(new { error = $"Configuration key '{key}' not found." });

        return Ok(config);
    }

    /// <summary>
    /// Update a configuration value
    /// </summary>
    /// <param name="request">Configuration update request</param>
    /// <returns>Updated configuration entry</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Update([FromBody] UpdateConfigurationRequest request)
    {
        _logger.LogInformation("Updating configuration: {Key}={Value}", request.Key, request.Value);

        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new { error = "Configuration key is required." });

        // TODO: Replace with MediatR command when ConfigurationService logic is migrated
        var entry = new ConfigurationEntry(
            request.Key,
            request.Value,
            request.Description,
            request.Category ?? "general",
            DateTime.UtcNow
        );

        _configurations[request.Key] = entry;

        return Ok(entry);
    }

    /// <summary>
    /// Create a new configuration entry
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] UpdateConfigurationRequest request)
    {
        _logger.LogInformation("Creating configuration: {Key}={Value}", request.Key, request.Value);

        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new { error = "Configuration key is required." });

        var entry = new ConfigurationEntry(
            request.Key,
            request.Value,
            request.Description,
            request.Category ?? "general",
            DateTime.UtcNow
        );

        _configurations[request.Key] = entry;
        return Created($"/api/admin/configurations/{request.Key}", entry);
    }

    /// <summary>
    /// Delete a configuration entry by key
    /// </summary>
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string key)
    {
        _logger.LogInformation("Deleting configuration: {Key}", key);

        if (!_configurations.Remove(key))
            return NotFound(new { error = $"Configuration key '{key}' not found." });

        return NoContent();
    }

    /// <summary>
    /// Bulk upsert configurations
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult BulkUpsert([FromBody] BulkUpsertConfigRequest request)
    {
        _logger.LogInformation("Bulk upserting {Count} configurations", request.Items?.Count ?? 0);

        var results = new List<ConfigurationEntry>();
        if (request.Items != null)
        {
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;
                var entry = new ConfigurationEntry(
                    item.Key,
                    item.Value,
                    item.Description,
                    item.Category ?? "general",
                    DateTime.UtcNow
                );
                _configurations[item.Key] = entry;
                results.Add(entry);
            }
        }

        return Ok(results);
    }

    /// <summary>
    /// Get configuration change history (stub — returns empty for now)
    /// </summary>
    [HttpGet("{configId}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHistory(string configId)
    {
        _logger.LogInformation("Getting history for configuration: {ConfigId}", configId);
        return Ok(Array.Empty<object>());
    }

    // ========================================
    // FEATURE FLAGS ENDPOINTS
    // ========================================

    /// <summary>
    /// Get all feature flags
    /// </summary>
    /// <returns>List of feature flags</returns>
    [HttpGet("/api/featureflags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllFeatureFlags()
    {
        _logger.LogInformation("Getting all feature flags");

        // TODO: Replace with MediatR query when ConfigurationService logic is migrated
        return Ok(_featureFlags.Values.ToList());
    }

    /// <summary>
    /// Create a new feature flag
    /// </summary>
    /// <param name="request">Feature flag creation data</param>
    /// <returns>Created feature flag</returns>
    [HttpPost("/api/featureflags")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateFeatureFlag([FromBody] CreateFeatureFlagRequest request)
    {
        _logger.LogInformation("Creating feature flag: {Key}", request.Key);

        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new { error = "Feature flag key is required." });

        if (_featureFlags.ContainsKey(request.Key))
            return BadRequest(new { error = $"Feature flag '{request.Key}' already exists." });

        // TODO: Replace with MediatR command when ConfigurationService logic is migrated
        var entry = new FeatureFlagEntry(request.Key, request.IsEnabled, request.Description, DateTime.UtcNow);
        _featureFlags[request.Key] = entry;

        return CreatedAtAction(nameof(GetAllFeatureFlags), entry);
    }

    /// <summary>
    /// Update an existing feature flag
    /// </summary>
    /// <param name="request">Feature flag update data</param>
    /// <returns>Updated feature flag</returns>
    [HttpPut("/api/featureflags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateFeatureFlag([FromBody] UpdateFeatureFlagRequest request)
    {
        _logger.LogInformation("Updating feature flag: {Key}", request.Key);

        if (!_featureFlags.ContainsKey(request.Key))
            return NotFound(new { error = $"Feature flag '{request.Key}' not found." });

        // TODO: Replace with MediatR command when ConfigurationService logic is migrated
        var entry = new FeatureFlagEntry(request.Key, request.IsEnabled, request.Description, DateTime.UtcNow);
        _featureFlags[request.Key] = entry;

        return Ok(entry);
    }

    /// <summary>
    /// Delete a feature flag
    /// </summary>
    /// <param name="key">Feature flag key</param>
    /// <returns>No content on success</returns>
    [HttpDelete("/api/featureflags/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteFeatureFlag(string key)
    {
        _logger.LogInformation("Deleting feature flag: {Key}", key);

        if (!_featureFlags.Remove(key))
            return NotFound(new { error = $"Feature flag '{key}' not found." });

        return NoContent();
    }
}

/// <summary>
/// Configuration entry record
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record ConfigurationEntry(
    string Key,
    string Value,
    string? Description,
    string Category,
    DateTime UpdatedAt
);

/// <summary>
/// Request to update a configuration entry
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record UpdateConfigurationRequest(
    string Key,
    string Value,
    string? Description = null,
    string? Category = null
);

/// <summary>
/// Feature flag entry record
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record FeatureFlagEntry(
    string Key,
    bool IsEnabled,
    string? Description,
    DateTime UpdatedAt
);

/// <summary>
/// Request to create a feature flag
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record CreateFeatureFlagRequest(
    string Key,
    bool IsEnabled = false,
    string? Description = null
);

/// <summary>
/// Request to update a feature flag
/// TODO: Move to Application/DTOs when fully implemented
/// </summary>
public record UpdateFeatureFlagRequest(
    string Key,
    bool IsEnabled,
    string? Description = null
);

public record BulkUpsertConfigRequest
{
    public List<UpdateConfigurationRequest>? Items { get; set; }
}
