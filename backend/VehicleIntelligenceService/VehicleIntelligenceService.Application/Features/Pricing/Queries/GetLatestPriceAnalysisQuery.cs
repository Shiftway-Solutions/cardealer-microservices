using MediatR;
using VehicleIntelligenceService.Application.DTOs;
using VehicleIntelligenceService.Domain.Interfaces;

namespace VehicleIntelligenceService.Application.Features.Pricing.Queries;

public record GetLatestPriceAnalysisQuery(Guid VehicleId) : IRequest<PriceAnalysisDto?>;

public class GetLatestPriceAnalysisHandler : IRequestHandler<GetLatestPriceAnalysisQuery, PriceAnalysisDto?>
{
    private readonly IPriceAnalysisRepository _repository;

    public GetLatestPriceAnalysisHandler(IPriceAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<PriceAnalysisDto?> Handle(GetLatestPriceAnalysisQuery request, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetLatestByVehicleIdAsync(request.VehicleId, cancellationToken);

        if (analysis == null)
            return null;

        return new PriceAnalysisDto
        {
            Id = analysis.Id,
            VehicleId = analysis.VehicleId,
            CurrentPrice = analysis.CurrentPrice,
            SuggestedPrice = analysis.SuggestedPrice,
            SuggestedPriceMin = analysis.SuggestedPriceMin,
            SuggestedPriceMax = analysis.SuggestedPriceMax,
            MarketAvgPrice = analysis.MarketAvgPrice,
            PriceVsMarket = analysis.PriceVsMarket,
            PricePosition = analysis.PricePosition,
            PredictedDaysToSaleAtCurrentPrice = analysis.PredictedDaysToSaleAtCurrentPrice,
            PredictedDaysToSaleAtSuggestedPrice = analysis.PredictedDaysToSaleAtSuggestedPrice,
            ConfidenceScore = analysis.ConfidenceScore,
            AnalysisDate = analysis.AnalysisDate
        };
    }
}
