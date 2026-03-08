using VehicleIntelligenceService.Domain.Entities;
using VehicleIntelligenceService.Domain.Interfaces;
using System.Text.Json;

namespace VehicleIntelligenceService.Infrastructure.Services;

/// <summary>
/// Motor de pricing heurístico para el mercado dominicano.
/// Clasifica en 5 niveles: Great Deal, Good Deal, Fair, High Price, Overpriced.
/// Detecta sobrevaluaciones >20% con flag IsOvervalued.
/// En producción, se complementaría con modelos ML reales (XGBoost, TensorFlow, etc.).
/// </summary>
public class PricingEngine : IPricingEngine
{
    // ── Constantes de negocio ────────────────────────────────────────────
    
    /// <summary>Km promedio por año en RD (el mercado dominicano usa kilómetros)</summary>
    internal const int ExpectedKmPerYear = 20_000;
    
    /// <summary>Costo por km de diferencia sobre el promedio esperado</summary>
    internal const decimal CostPerKmDifference = 0.06m;
    
    /// <summary>Umbral de sobrevaluación grave (20% por encima del mercado)</summary>
    internal const decimal OvervaluationThreshold = 0.20m;
    
    // ── Precios base por marca (USD, vehículo nuevo) ─────────────────────
    // Incluye las marcas más vendidas en República Dominicana
    private static readonly Dictionary<string, decimal> BrandBasePrices = new(StringComparer.OrdinalIgnoreCase)
    {
        // Japonesas (dominan el mercado DR)
        ["Toyota"]     = 30_000m,
        ["Honda"]      = 28_000m,
        ["Nissan"]     = 25_000m,
        ["Mazda"]      = 27_000m,
        ["Mitsubishi"] = 26_000m,
        ["Suzuki"]     = 20_000m,
        ["Subaru"]     = 29_000m,
        ["Lexus"]      = 45_000m,
        ["Acura"]      = 38_000m,
        ["Infiniti"]   = 40_000m,
        
        // Coreanas (muy populares en DR)
        ["Hyundai"]    = 23_000m,
        ["Kia"]        = 22_000m,
        ["Genesis"]    = 42_000m,
        
        // Americanas
        ["Ford"]       = 26_000m,
        ["Chevrolet"]  = 25_000m,
        ["Jeep"]       = 32_000m,
        ["Dodge"]      = 28_000m,
        ["Ram"]        = 35_000m,
        ["GMC"]        = 34_000m,
        ["Chrysler"]   = 27_000m,
        ["Buick"]      = 30_000m,
        ["Cadillac"]   = 45_000m,
        ["Lincoln"]    = 42_000m,
        
        // Europeas
        ["Mercedes-Benz"] = 48_000m,
        ["BMW"]           = 46_000m,
        ["Audi"]          = 44_000m,
        ["Volkswagen"]    = 28_000m,
        ["Volvo"]         = 40_000m,
        ["Porsche"]       = 65_000m,
        ["Land Rover"]    = 50_000m,
        ["Mini"]          = 30_000m,
        ["Fiat"]          = 20_000m,
        ["Peugeot"]       = 22_000m,
        ["Renault"]       = 21_000m,
        
        // Chinas (creciente presencia en DR)
        ["Changan"]  = 18_000m,
        ["Chery"]    = 17_000m,
        ["Haval"]    = 22_000m,
        ["MG"]       = 20_000m,
        ["BYD"]      = 25_000m,
        ["JAC"]      = 16_000m,
        ["Great Wall"] = 19_000m,
    };
    
    // Seed determinista para el factor de mercado (evita Random no reproducible)
    private static int GetDeterministicMarketFactor(string make, string model, int year)
    {
        var hash = HashCode.Combine(
            make?.ToUpperInvariant() ?? "",
            model?.ToUpperInvariant() ?? "",
            year);
        // Mapear hash a rango 20-30 (mismo rango que el Random anterior)
        return 20 + (Math.Abs(hash) % 11);
    }
    
    // ── AnalyzePriceAsync ────────────────────────────────────────────────
    
    public async Task<PriceAnalysis> AnalyzePriceAsync(VehiclePricingInput input, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        var basePrice = CalculateBasePrice(input.Make, input.Model, input.Year);
        var mileageAdjustment = CalculateMileageAdjustment(input.Mileage, input.Year);
        
        var conditionMultiplier = input.Condition switch
        {
            "Excellent" => 1.10m,
            "Good"      => 1.00m,
            "Fair"      => 0.90m,
            "Poor"      => 0.75m,
            _           => 1.00m
        };
        
        var qualityBonus = CalculateQualityBonus(input.PhotoCount, input.ViewCount);
        
        // Precio sugerido = (base + ajuste km) × condición + bonus calidad
        var suggestedPrice = (basePrice + mileageAdjustment) * conditionMultiplier + qualityBonus;
        
        // Protección: precio sugerido nunca menor a $500
        suggestedPrice = Math.Max(suggestedPrice, 500m);
        
        var priceRange = suggestedPrice * 0.05m; // ±5%
        
        // Ratio precio actual vs sugerido
        var priceVsMarket = input.CurrentPrice / suggestedPrice;
        
        // Porcentaje de sobrevaluación (positivo = arriba, negativo = debajo)
        var overvaluationPct = (priceVsMarket - 1.0m) * 100m;
        
        // ── Clasificación en 5 niveles ──
        var pricePosition = ClassifyPricePosition(priceVsMarket);
        
        // ── Flag de sobrevaluación >20% ──
        var isOvervalued = priceVsMarket > (1.0m + OvervaluationThreshold);
        
        var predictedDaysAtCurrent = CalculatePredictedDays(priceVsMarket, input.DaysListed);
        var predictedDaysAtSuggested = CalculatePredictedDays(1.0m, input.DaysListed);
        var confidenceScore = CalculateConfidence(input.ViewCount, input.DaysListed);
        
        // Factores que influyen — incluye info de sobrevaluación
        var influencingFactors = new Dictionary<string, object>
        {
            ["basePrice"] = basePrice,
            ["mileageAdjustment"] = mileageAdjustment,
            ["conditionMultiplier"] = conditionMultiplier,
            ["qualityBonus"] = qualityBonus,
            ["pricePosition"] = pricePosition,
            ["overvaluationPercentage"] = Math.Round(overvaluationPct, 2)
        };
        
        if (isOvervalued)
        {
            influencingFactors["overvaluationAlert"] = 
                $"ALERTA: Vehículo sobrepreciado en {Math.Round(overvaluationPct, 1)}%. " +
                $"Precio actual RD$ {input.CurrentPrice:N0} vs sugerido RD$ {Math.Round(suggestedPrice, 0):N0}. " +
                "Recomendamos reducir el precio para agilizar la venta.";
        }
        
        return new PriceAnalysis
        {
            Id = Guid.NewGuid(),
            VehicleId = input.VehicleId,
            CurrentPrice = input.CurrentPrice,
            SuggestedPrice = Math.Round(suggestedPrice, 2),
            SuggestedPriceMin = Math.Round(suggestedPrice - priceRange, 2),
            SuggestedPriceMax = Math.Round(suggestedPrice + priceRange, 2),
            MarketAvgPrice = Math.Round(basePrice, 2),
            PriceVsMarket = Math.Round(priceVsMarket, 4),
            PricePosition = pricePosition,
            IsOvervalued = isOvervalued,
            OvervaluationPercentage = Math.Round(overvaluationPct, 2),
            PredictedDaysToSaleAtCurrentPrice = predictedDaysAtCurrent,
            PredictedDaysToSaleAtSuggestedPrice = predictedDaysAtSuggested,
            ConfidenceScore = confidenceScore,
            AnalysisDate = DateTime.UtcNow,
            InfluencingFactors = JsonSerializer.Serialize(influencingFactors)
        };
    }
    
    // ── Clasificación de precio en 5 niveles ─────────────────────────────
    
    /// <summary>
    /// 5-tier price classification aligned with OKLA spec:
    /// "Great Deal"  → ≤ 0.85 (15%+ debajo del mercado)
    /// "Good Deal"   → 0.85–0.95 (5–15% debajo)
    /// "Fair"        → 0.95–1.05 (±5%)
    /// "High Price"  → 1.05–1.15 (5–15% arriba)
    /// "Overpriced"  → > 1.15 (15%+ arriba)
    /// </summary>
    internal static string ClassifyPricePosition(decimal priceVsMarket) => priceVsMarket switch
    {
        <= 0.85m => "Great Deal",
        <= 0.95m => "Good Deal",
        <= 1.05m => "Fair",
        <= 1.15m => "High Price",
        _        => "Overpriced"
    };

    // ── PredictDemandAsync ───────────────────────────────────────────────

    public async Task<DemandPrediction> PredictDemandAsync(DemandPredictionInput input, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        
        var demandScore = CalculateDemandScore(input.Make, input.Model, input.Year);
        var currentDemand = demandScore switch
        {
            >= 80 => DemandLevel.VeryHigh,
            >= 60 => DemandLevel.High,
            >= 40 => DemandLevel.Medium,
            >= 20 => DemandLevel.Low,
            _     => DemandLevel.VeryLow
        };
        
        var trend = demandScore >= 60 ? TrendDirection.Rising : 
                    demandScore <= 30 ? TrendDirection.Falling : 
                    TrendDirection.Stable;
        
        var trendStrength = trend == TrendDirection.Stable ? 0.1m : 0.7m;
        
        var predicted30 = PredictFutureDemand(currentDemand, trend, 30);
        var predicted90 = PredictFutureDemand(currentDemand, trend, 90);
        
        var searchesPerDay = (int)(demandScore * 0.5m);
        var availableInventory = 100 - (int)(demandScore * 0.5m);
        var avgDaysToSale = demandScore >= 60 ? 15 : demandScore >= 40 ? 35 : 60;
        
        var buyRec = demandScore >= 70 ? BuyRecommendation.StrongBuy :
                     demandScore >= 50 ? BuyRecommendation.Buy :
                     demandScore >= 30 ? BuyRecommendation.Hold :
                     BuyRecommendation.Avoid;
        
        var insights = GenerateInsights(demandScore, currentDemand, trend, buyRec);
        
        return new DemandPrediction
        {
            Id = Guid.NewGuid(),
            Make = input.Make,
            Model = input.Model,
            Year = input.Year,
            CurrentDemand = currentDemand,
            DemandScore = demandScore,
            Trend = trend,
            TrendStrength = trendStrength,
            PredictedDemand30Days = predicted30,
            PredictedDemand90Days = predicted90,
            SearchesPerDay = searchesPerDay,
            AvailableInventory = availableInventory,
            AvgDaysToSale = avgDaysToSale,
            BuyRecommendation = buyRec,
            BuyRecommendationReason = GetBuyRecommendationReason(buyRec, demandScore, avgDaysToSale),
            Insights = JsonSerializer.Serialize(insights),
            PredictionDate = DateTime.UtcNow
        };
    }

    // ── GetMarketComparablesAsync ────────────────────────────────────────

    public Task<List<MarketComparable>> GetMarketComparablesAsync(Guid vehicleId, int limit = 10, CancellationToken cancellationToken = default)
    {
        // En producción, esto consultaría APIs externas o DB de comparables
        var comparables = new List<MarketComparable>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Source = "OKLA",
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Mileage = 45000,
                Price = 28500,
                SimilarityScore = 95,
                Status = "Active",
                DaysOnMarket = 12
            }
        };
        
        return Task.FromResult(comparables.Take(limit).ToList());
    }

    // ── Métodos privados de cálculo ──────────────────────────────────────
    
    private decimal CalculateBasePrice(string make, string model, int year)
    {
        var basePrice = BrandBasePrices.GetValueOrDefault(make, 25_000m);
        
        // Depreciación ~10% anual (exponencial)
        var currentYear = DateTime.Now.Year;
        var age = Math.Max(0, currentYear - year);
        var depreciationFactor = Math.Pow(0.90, age);
        
        return basePrice * (decimal)depreciationFactor;
    }
    
    private decimal CalculateMileageAdjustment(int mileage, int year)
    {
        var currentYear = DateTime.Now.Year;
        var age = Math.Max(1, currentYear - year);
        var expectedMileage = age * ExpectedKmPerYear; // km en RD
        
        var mileageDiff = mileage - expectedMileage;
        
        // Costo por km de diferencia (penaliza exceso, bonifica bajo km)
        return mileageDiff * -CostPerKmDifference;
    }
    
    private decimal CalculateQualityBonus(int photoCount, int viewCount)
    {
        var photoBonus = Math.Min(photoCount * 50, 500);  // Max $500 por fotos
        var viewBonus = Math.Min(viewCount * 2, 300);     // Max $300 por views
        return photoBonus + viewBonus;
    }
    
    private int CalculatePredictedDays(decimal priceVsMarket, int daysListed)
    {
        var baseDays = 30;
        
        if (priceVsMarket > 1.20m) return baseDays + 45; // 75 días — sobrepreciado grave
        if (priceVsMarket > 1.15m) return baseDays + 30; // 60 días — overpriced
        if (priceVsMarket > 1.05m) return baseDays + 15; // 45 días — alto
        if (priceVsMarket < 0.85m) return baseDays - 20; // 10 días — gran deal
        if (priceVsMarket < 0.95m) return baseDays - 10; // 20 días — buen deal
        
        return baseDays; // 30 días — fair
    }
    
    private decimal CalculateConfidence(int viewCount, int daysListed)
    {
        var viewScore = Math.Min(viewCount / 10.0m, 50);
        var timeScore = Math.Min(daysListed / 3.0m, 50);
        
        return Math.Round(viewScore + timeScore, 2);
    }
    
    /// <summary>
    /// Calcula demand score de forma DETERMINISTA (ya no usa Random).
    /// Usa un hash del make+model+year como factor de mercado reproducible.
    /// </summary>
    private decimal CalculateDemandScore(string make, string model, int year)
    {
        // Marcas populares en RD
        var popularBrands = new[] { "Toyota", "Honda", "Nissan", "Hyundai", "Kia" };
        var brandScore = popularBrands.Contains(make, StringComparer.OrdinalIgnoreCase) ? 30 : 20;
        
        var currentYear = DateTime.Now.Year;
        var age = currentYear - year;
        var yearScore = age <= 3 ? 40 : age <= 5 ? 30 : age <= 10 ? 20 : 10;
        
        // Factor de mercado determinista (reemplaza new Random().Next(20, 31))
        var marketFactor = GetDeterministicMarketFactor(make, model, year);
        
        return brandScore + yearScore + marketFactor;
    }
    
    private DemandLevel PredictFutureDemand(DemandLevel current, TrendDirection trend, int days)
    {
        if (trend == TrendDirection.Stable) return current;
        
        var change = trend == TrendDirection.Rising ? 1 : -1;
        var newLevel = (int)current + change;
        
        return newLevel switch
        {
            <= 0 => DemandLevel.VeryHigh,
            >= 4 => DemandLevel.VeryLow,
            _    => (DemandLevel)newLevel
        };
    }
    
    private List<string> GenerateInsights(decimal demandScore, DemandLevel level, TrendDirection trend, BuyRecommendation buyRec)
    {
        var insights = new List<string>();
        
        if (demandScore >= 70)
            insights.Add($"Alta demanda: {demandScore}/100 score");
        
        if (trend == TrendDirection.Rising)
            insights.Add("Tendencia al alza, buen momento para inventario");
        
        if (level == DemandLevel.VeryHigh)
            insights.Add("Se vende rápido, menos de 15 días promedio");
        
        if (buyRec == BuyRecommendation.StrongBuy)
            insights.Add("Excelente oportunidad de compra para dealers");
        
        return insights;
    }
    
    private string GetBuyRecommendationReason(BuyRecommendation rec, decimal score, decimal avgDays)
    {
        return rec switch
        {
            BuyRecommendation.StrongBuy => $"Alta demanda ({score}/100), se vende en {avgDays} días promedio",
            BuyRecommendation.Buy       => $"Buena demanda ({score}/100), rotación aceptable",
            BuyRecommendation.Hold      => $"Demanda media ({score}/100), esperar mejores condiciones",
            BuyRecommendation.Avoid     => $"Baja demanda ({score}/100), difícil de vender",
            _                           => "Sin razón disponible"
        };
    }
}
