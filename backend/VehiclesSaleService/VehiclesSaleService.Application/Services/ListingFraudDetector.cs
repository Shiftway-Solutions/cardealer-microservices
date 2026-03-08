using VehiclesSaleService.Domain.Entities;

namespace VehiclesSaleService.Application.Services;

/// <summary>
/// Fraud detection service for vehicle listings.
/// Evaluates risk signals at publish time and produces a fraud score + flags.
/// Score range: 0 (clean) to 100 (highly suspicious).
/// Listings scoring ≥50 are flagged for manual review with details.
/// </summary>
public static class ListingFraudDetector
{
    /// <summary>
    /// Evaluates a vehicle listing for fraud signals before publication.
    /// </summary>
    public static FraudEvaluation Evaluate(Vehicle vehicle, decimal? marketPriceEstimate = null)
    {
        var signals = new List<FraudSignal>();
        int score = 0;

        // 1. Missing VIN — vehicles without VIN are harder to verify
        if (string.IsNullOrWhiteSpace(vehicle.VIN))
        {
            score += 15;
            signals.Add(new FraudSignal("NO_VIN",
                "Vehículo publicado sin número VIN. No se puede verificar historial.",
                FraudSeverity.Medium, 15));
        }

        // 2. VIN checksum failure (if VIN provided but invalid)
        if (!string.IsNullOrWhiteSpace(vehicle.VIN))
        {
            var vinResult = VinValidationService.Validate(vehicle.VIN);
            if (!vinResult.IsValid && vinResult.IsChecksumFailure)
            {
                score += 25;
                signals.Add(new FraudSignal("VIN_CHECKSUM_FAIL",
                    "El dígito verificador del VIN no es válido. Posible VIN fabricado.",
                    FraudSeverity.High, 25));
            }
        }

        // 3. Price anomaly — too low or too high relative to market
        if (marketPriceEstimate.HasValue && marketPriceEstimate.Value > 0)
        {
            var ratio = vehicle.Price / marketPriceEstimate.Value;

            if (ratio < 0.4m)
            {
                score += 30;
                signals.Add(new FraudSignal("PRICE_TOO_LOW",
                    $"Precio ({vehicle.Price:N0} DOP) es {(1 - ratio) * 100:F0}% inferior al precio de mercado ({marketPriceEstimate.Value:N0} DOP). Posible fraude.",
                    FraudSeverity.High, 30));
            }
            else if (ratio < 0.6m)
            {
                score += 15;
                signals.Add(new FraudSignal("PRICE_BELOW_MARKET",
                    $"Precio significativamente por debajo del mercado ({ratio * 100:F0}% del valor estimado).",
                    FraudSeverity.Medium, 15));
            }
            else if (ratio > 2.0m)
            {
                score += 10;
                signals.Add(new FraudSignal("PRICE_ABOVE_MARKET",
                    $"Precio muy por encima del mercado ({ratio * 100:F0}% del valor estimado).",
                    FraudSeverity.Low, 10));
            }
        }
        else
        {
            // No market data available — basic price sanity checks
            if (vehicle.Price < 50_000 && vehicle.Year >= 2010)
            {
                score += 20;
                signals.Add(new FraudSignal("PRICE_SUSPICIOUSLY_LOW",
                    $"Precio ({vehicle.Price:N0} DOP) sospechosamente bajo para un vehículo año {vehicle.Year}.",
                    FraudSeverity.Medium, 20));
            }
        }

        // 4. Unrealistically low mileage for year
        if (vehicle.Mileage > 0 && vehicle.Year > 0)
        {
            var vehicleAge = DateTime.UtcNow.Year - vehicle.Year;
            if (vehicleAge > 0)
            {
                // Average ~15,000 km/year. Flag if <2,000 km/year for vehicles older than 5 years
                var avgPerYear = vehicle.Mileage / (decimal)vehicleAge;
                if (vehicleAge >= 5 && avgPerYear < 2_000)
                {
                    score += 15;
                    signals.Add(new FraudSignal("LOW_MILEAGE_FOR_AGE",
                        $"Kilometraje ({vehicle.Mileage:N0} km) inusualmente bajo para un vehículo de {vehicleAge} años ({avgPerYear:N0} km/año promedio).",
                        FraudSeverity.Medium, 15));
                }
            }
        }

        // 5. Self-reported clean title on high-mileage vehicle
        if (vehicle.HasCleanTitle && vehicle.Mileage > 250_000)
        {
            score += 5;
            signals.Add(new FraudSignal("HIGH_MILEAGE_CLEAN_TITLE",
                "Título limpio autodeclarado en vehículo con alto kilometraje. Verificar con CARFAX.",
                FraudSeverity.Low, 5));
        }

        // 6. Missing critical contact info
        if (string.IsNullOrWhiteSpace(vehicle.SellerPhone) && string.IsNullOrWhiteSpace(vehicle.SellerEmail))
        {
            score += 10;
            signals.Add(new FraudSignal("NO_CONTACT",
                "El vendedor no proporcionó información de contacto.",
                FraudSeverity.Medium, 10));
        }

        // 7. Very short description
        if (string.IsNullOrWhiteSpace(vehicle.Description) || vehicle.Description.Length < 20)
        {
            score += 5;
            signals.Add(new FraudSignal("MINIMAL_DESCRIPTION",
                "Descripción muy corta o ausente. Listados legítimos suelen tener descripciones detalladas.",
                FraudSeverity.Low, 5));
        }

        // 8. No images or very few images
        if (vehicle.Images.Count <= 1)
        {
            score += 10;
            signals.Add(new FraudSignal("FEW_IMAGES",
                $"Solo {vehicle.Images.Count} imagen(es). Listados legítimos suelen tener 5+ fotos.",
                FraudSeverity.Low, 10));
        }

        // Cap score at 100
        score = Math.Min(score, 100);

        return new FraudEvaluation
        {
            FraudScore = score,
            RiskLevel = score switch
            {
                >= 50 => FraudRiskLevel.High,
                >= 25 => FraudRiskLevel.Medium,
                _ => FraudRiskLevel.Low
            },
            Signals = signals,
            RequiresManualReview = score >= 50,
            EvaluatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Result of fraud evaluation for a vehicle listing
/// </summary>
public record FraudEvaluation
{
    public int FraudScore { get; init; }
    public FraudRiskLevel RiskLevel { get; init; }
    public List<FraudSignal> Signals { get; init; } = new();
    public bool RequiresManualReview { get; init; }
    public DateTime EvaluatedAt { get; init; }
}

public record FraudSignal(
    string Code,
    string Description,
    FraudSeverity Severity,
    int Points);

public enum FraudRiskLevel
{
    Low,
    Medium,
    High
}

public enum FraudSeverity
{
    Low,
    Medium,
    High
}
