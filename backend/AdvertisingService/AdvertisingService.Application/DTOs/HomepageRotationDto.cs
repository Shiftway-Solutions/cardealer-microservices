using System.Text.Json.Serialization;
using AdvertisingService.Domain.Enums;

namespace AdvertisingService.Application.DTOs;

/// <summary>
/// Top-level response for GET /api/advertising/rotation/{section}.
/// JsonPropertyName attributes ensure camelCase output that matches the frontend TypeScript types.
/// </summary>
public record HomepageRotationDto(
    [property: JsonPropertyName("section")]      AdPlacementType Section,
    [property: JsonPropertyName("items")]        List<RotatedVehicleDto> Vehicles,
    [property: JsonPropertyName("algorithmUsed")] RotationAlgorithmType AlgorithmUsed,
    [property: JsonPropertyName("generatedAt")]  DateTime GeneratedAt
);

/// <summary>
/// A single vehicle slot in the homepage rotation.
/// Includes raw rotation metadata plus enriched vehicle details fetched from VehiclesSaleService.
/// </summary>
public record RotatedVehicleDto(
    [property: JsonPropertyName("campaignId")]   Guid? CampaignId,
    [property: JsonPropertyName("vehicleId")]    Guid VehicleId,
    [property: JsonPropertyName("ownerId")]      Guid OwnerId,
    [property: JsonPropertyName("ownerType")]    string OwnerType,
    [property: JsonPropertyName("position")]     int Position,
    [property: JsonPropertyName("qualityScore")] decimal Score,
    // ── Enrichment fields from VehiclesSaleService ────────────────────────────
    [property: JsonPropertyName("title")]        string? Title,
    [property: JsonPropertyName("slug")]         string? Slug,
    [property: JsonPropertyName("imageUrl")]     string? ImageUrl,
    [property: JsonPropertyName("price")]        decimal Price,
    [property: JsonPropertyName("currency")]     string Currency,
    [property: JsonPropertyName("location")]     string? Location,
    [property: JsonPropertyName("isFeatured")]   bool IsFeatured,
    [property: JsonPropertyName("isPremium")]    bool IsPremium
);
