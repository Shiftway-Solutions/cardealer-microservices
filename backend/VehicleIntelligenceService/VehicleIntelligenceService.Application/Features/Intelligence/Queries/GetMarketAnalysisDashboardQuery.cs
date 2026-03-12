using MediatR;
using VehicleIntelligenceService.Application.DTOs;

namespace VehicleIntelligenceService.Application.Features.Intelligence.Queries;

public record GetMarketAnalysisDashboardQuery(
    string? Make = null,
    string? Model = null,
    int? MinYear = null,
    int? MaxYear = null,
    string? FuelType = null,
    string? BodyType = null)
    : IRequest<List<MarketAnalysisDto>>;

public class GetMarketAnalysisDashboardHandler
    : IRequestHandler<GetMarketAnalysisDashboardQuery, List<MarketAnalysisDto>>
{
    public Task<List<MarketAnalysisDto>> Handle(GetMarketAnalysisDashboardQuery request, CancellationToken cancellationToken)
    {
        // En producción, esto filturaría datos reales basados en los parámetros
        // Por ahora, retornamos análisis mock para categorías populares
        var popularModels = new[]
        {
            ("Toyota", "Corolla"),
            ("Honda", "Civic"),
            ("Hyundai", "Elantra"),
            ("Mazda", "3"),
            ("Kia", "Forte"),
            ("Toyota", "Yaris"),
            ("Honda", "Fit"),
            ("Nissan", "Sentra")
        };

        var result = new List<MarketAnalysisDto>();

        foreach (var (make, model) in popularModels)
        {
            // Aplicar filtros si existen
            if (!string.IsNullOrEmpty(request.Make) && !request.Make.Equals(make, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(request.Model) && !request.Model.Equals(model, StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(new MarketAnalysisDto(
                Make: make,
                Model: model,
                Year: 2023,
                TotalListings: Random.Shared.Next(50, 300),
                AvgPrice: 1_100_000m + (Random.Shared.Next(-400000, 400000)),
                MinPrice: 800_000m,
                MaxPrice: 1_800_000m,
                AvgDaysToSale: Random.Shared.Next(12, 45),
                MedianDaysToSale: Random.Shared.Next(12, 45),
                PriceTrend: Random.Shared.Next(0, 3) switch
                {
                    0 => "Rising",
                    1 => "Stable",
                    _ => "Falling"
                },
                DemandTrend: Random.Shared.Next(0, 3) switch
                {
                    0 => "Rising",
                    1 => "Stable",
                    _ => "Falling"
                },
                CompetitorCount: Random.Shared.Next(15, 150),
                MarketShare: Random.Shared.Next(2, 25) / 100m,
                Recommendations: new List<string>
                {
                    $"{make} {model} sigue siendo uno de los más buscados",
                    "Mantén el precio competitivo para atraer más compradores",
                    "Los vehículos con más fotos de calidad se venden más rápido",
                    "Considera incluir información sobre mantenimiento en la descripción"
                }
            ));
        }

        return Task.FromResult(result);
    }
}
