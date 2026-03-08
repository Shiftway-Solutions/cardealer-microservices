namespace NotificationService.Application.DTOs;

// ========== Price Alert DTOs ==========

public record CreatePriceAlertRequest(
    Guid VehicleId,
    string? VehicleTitle,
    decimal? CurrentPrice,
    decimal TargetPrice,
    decimal? PriceDropPercentage = null,
    bool? NotifyByEmail = true,
    bool? NotifyByPush = true,
    bool? NotifyBySms = false
);

public record UpdatePriceAlertRequest(
    decimal? TargetPrice = null,
    decimal? PriceDropPercentage = null,
    bool? IsActive = null,
    bool? NotifyByEmail = null,
    bool? NotifyByPush = null,
    bool? NotifyBySms = null
);

public record PriceAlertResponse(
    Guid Id,
    Guid UserId,
    Guid VehicleId,
    string VehicleTitle,
    string? VehicleImageUrl,
    decimal CurrentPrice,
    decimal TargetPrice,
    decimal? PriceDropPercentage,
    bool IsActive,
    bool NotifyByEmail,
    bool NotifyByPush,
    bool NotifyBySms,
    int TriggeredCount,
    DateTime? LastNotifiedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ========== Saved Search DTOs ==========

public record SavedSearchCriteria(
    string? Make = null,
    string? Model = null,
    string? BodyType = null,
    int? MinYear = null,
    int? MaxYear = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? FuelType = null,
    string? Transmission = null,
    string? Location = null,
    int? MaxMileage = null
);

public record CreateSavedSearchRequest(
    string Name,
    SavedSearchCriteria Criteria,
    bool? NotifyOnNewResults = true,
    bool? NotifyByEmail = true,
    bool? NotifyByPush = true,
    string? NotificationFrequency = "daily"
);

public record UpdateSavedSearchRequest(
    string? Name = null,
    SavedSearchCriteria? Criteria = null,
    bool? NotifyOnNewResults = null,
    bool? NotifyByEmail = null,
    bool? NotifyByPush = null,
    string? NotificationFrequency = null
);

public record SavedSearchResponse(
    Guid Id,
    Guid UserId,
    string Name,
    SavedSearchCriteria? Criteria,
    bool NotifyOnNewResults,
    bool NotifyByEmail,
    bool NotifyByPush,
    string NotificationFrequency,
    int MatchCount,
    DateTime? LastMatchAt,
    DateTime? LastNotifiedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
