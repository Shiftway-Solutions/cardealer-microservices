namespace ComparisonService.Application.DTOs;

/// <summary>
/// DTO for comparison list responses.
/// </summary>
public record ComparisonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<Guid> VehicleIds { get; init; } = new();
    public int VehicleCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsPublic { get; init; }
    public bool HasShareLink { get; init; }
}

/// <summary>
/// DTO for comparison detail responses with full vehicle data.
/// </summary>
public record ComparisonDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<Guid> VehicleIds { get; init; } = new();
    public List<VehicleComparisonDto> Vehicles { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsPublic { get; init; }
    public string? ShareToken { get; init; }
    public string? ShareUrl { get; init; }
}

/// <summary>
/// DTO representing a vehicle in a comparison context.
/// </summary>
public record VehicleComparisonDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public int? Mileage { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? BodyStyle { get; init; }
    public string? Condition { get; init; }
    public string? PrimaryImageUrl { get; init; }
}

/// <summary>
/// DTO for share link responses.
/// </summary>
public record ShareResponseDto
{
    public string ShareToken { get; init; } = string.Empty;
    public string ShareUrl { get; init; } = string.Empty;
}
