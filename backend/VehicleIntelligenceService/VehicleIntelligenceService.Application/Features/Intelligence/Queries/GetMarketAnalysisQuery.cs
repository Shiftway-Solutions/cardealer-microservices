using MediatR;
using VehicleIntelligenceService.Application.DTOs;

namespace VehicleIntelligenceService.Application.Features.Intelligence.Queries;

public record GetMarketAnalysisQuery(string Make, string Model, int Year)
    : IRequest<MarketAnalysisDto?>;

public class GetMarketAnalysisHandler : IRequestHandler<GetMarketAnalysisQuery, MarketAnalysisDto?>
{
    public Task<MarketAnalysisDto?> Handle(GetMarketAnalysisQuery request, CancellationToken cancellationToken)
    {
        // En producción, esto vendría de análisis de datos reales del mercado
        // Por ahora, datos mock basados en el tipo de vehículo
        var result = new MarketAnalysisDto(
            Make: request.Make,
            Model: request.Model,
            Year: request.Year,
            TotalListings: Random.Shared.Next(50, 500),
            AvgPrice: 1_200_000m + (Random.Shared.Next(-500000, 500000)),
            MinPrice: 900_000m,
            MaxPrice: 2_000_000m,
            AvgDaysToSale: Random.Shared.Next(15, 60),
            MedianDaysToSale: Random.Shared.Next(15, 60),
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
            CompetitorCount: Random.Shared.Next(20, 200),
            MarketShare: Random.Shared.Next(2, 20) / 100m,
            Recommendations: new List<string>
            {
                $"Este {request.Make} {request.Model} tiene una demanda moderada en el mercado",
                "Considera ajustar el precio si está por encima del promedio de mercado",
                "Las búsquedas han mostrado una tendencia creciente este mes",
                "Asegúrate de incluir fotos de calidad para aumentar las visualizaciones"
            }
        );

        return Task.FromResult<MarketAnalysisDto?>(result);
    }
}
