namespace VehicleIntelligenceService.Domain.Entities;

public class MarketComparable
{
    public Guid Id { get; set; }
    public Guid PriceAnalysisId { get; set; }
    public PriceAnalysis? PriceAnalysis { get; set; }

    // Info del vehículo comparable
    public Guid? VehicleId { get; set; }              // Si es de nuestra plataforma
    public string Source { get; set; } = string.Empty; // "OKLA", "Facebook", "AutoTrader"
    public string ExternalUrl { get; set; } = string.Empty;

    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Mileage { get; set; }
    public decimal Price { get; set; }

    // Similitud con vehículo objetivo
    public decimal SimilarityScore { get; set; }      // 0-100

    // Estado
    public string Status { get; set; } = "Active";    // Active, Sold, Expired
    public DateTime? ListedDate { get; set; }
    public DateTime? SoldDate { get; set; }
    public int? DaysOnMarket { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
