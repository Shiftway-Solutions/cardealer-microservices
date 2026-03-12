using MediatR;
using VehicleIntelligenceService.Application.DTOs;
using VehicleIntelligenceService.Domain.Interfaces;

namespace VehicleIntelligenceService.Application.Features.Pricing.Queries;

public record GetPriceAnalysisByIdQuery(Guid Id) : IRequest<PriceAnalysisDto?>;

public class GetPriceAnalysisByIdHandler : IRequestHandler<GetPriceAnalysisByIdQuery, PriceAnalysisDto?>
{
    private readonly IPriceAnalysisRepository _repository;

    public GetPriceAnalysisByIdHandler(IPriceAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<PriceAnalysisDto?> Handle(GetPriceAnalysisByIdQuery request, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdAsync(request.Id, cancellationToken);

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
