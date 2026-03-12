using VehicleIntelligenceService.Domain.Entities;

namespace VehicleIntelligenceService.Domain.Interfaces;

/// <summary>
/// Motor de pricing con Machine Learning
/// </summary>
public interface IPricingEngine
{
    /// <summary>
    /// Calcula el precio sugerido para un vehículo
    /// </summary>
    Task<PriceAnalysis> AnalyzePriceAsync(VehiclePricingInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Predice el nivel de demanda para un vehículo
    /// </summary>
    Task<DemandPrediction> PredictDemandAsync(DemandPredictionInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene comparables del mercado
    /// </summary>
    Task<List<MarketComparable>> GetMarketComparablesAsync(Guid vehicleId, int limit = 10, CancellationToken cancellationToken = default);
}

public record VehiclePricingInput(
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    int Mileage,
    string Condition,
    string FuelType,
    string Transmission,
    decimal CurrentPrice,
    int PhotoCount,
    int ViewCount,
    int DaysListed
);

public record DemandPredictionInput(
    string Make,
    string Model,
    int Year,
    string? FuelType = null,
    string? Transmission = null
);
