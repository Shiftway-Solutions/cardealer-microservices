namespace ComparisonService.Domain.Entities;

public class VehicleComparison
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> VehicleIds { get; set; } = new();
    public string? ShareToken { get; set; }
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    protected VehicleComparison() { } // For EF Core

    public VehicleComparison(Guid userId, string name, List<Guid>? vehicleIds = null, bool isPublic = false)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        VehicleIds = vehicleIds ?? new List<Guid>();
        IsPublic = isPublic;
        CreatedAt = DateTime.UtcNow;

        if (VehicleIds.Count > 3)
            throw new ArgumentException("Cannot compare more than 3 vehicles");
    }

    public void AddVehicle(Guid vehicleId)
    {
        if (VehicleIds.Count >= 3)
            throw new InvalidOperationException("Cannot add more than 3 vehicles to a comparison");

        if (VehicleIds.Contains(vehicleId))
            throw new InvalidOperationException("Vehicle already exists in this comparison");

        VehicleIds.Add(vehicleId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveVehicle(Guid vehicleId)
    {
        VehicleIds.Remove(vehicleId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateVehicles(List<Guid> vehicleIds)
    {
        if (vehicleIds.Count > 3)
            throw new ArgumentException("Cannot compare more than 3 vehicles");

        VehicleIds = vehicleIds;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MakePublic()
    {
        IsPublic = true;
        ShareToken = Guid.NewGuid().ToString("N")[..16]; // 16 character token
        UpdatedAt = DateTime.UtcNow;
    }

    public void MakePrivate()
    {
        IsPublic = false;
        ShareToken = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void GenerateShareToken()
    {
        ShareToken = Guid.NewGuid().ToString("N")[..16]; // 16 character token
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
