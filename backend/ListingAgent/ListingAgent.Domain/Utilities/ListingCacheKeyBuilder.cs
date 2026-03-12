namespace ListingAgent.Domain.Utilities;

/// <summary>
/// Pure utility for building semantic Redis cache keys for the ListingAgent.
/// Keys are based on Make/Model/Year/Trim (not VehicleId), so ALL vehicles of the
/// same type share the same cache entry — enabling ≥50% cache hit rate after warm-up.
/// </summary>
public static class ListingCacheKeyBuilder
{
    /// <summary>
    /// Builds a normalized cache key: "listing:v2:{make}:{model}:{year}:{trim}".
    /// Normalization: lowercase, trim spaces, replace spaces/hyphens/slashes with underscores.
    /// </summary>
    public static string Build(string make, string model, int year, string? trim)
    {
        var normalizedMake = Normalize(make);
        var normalizedModel = Normalize(model);
        var normalizedTrim = Normalize(trim);

        return $"listing:v2:{normalizedMake}:{normalizedModel}:{year}:{normalizedTrim}";
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("-", "_");
}
