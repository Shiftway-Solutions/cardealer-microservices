using MediatR;
using VehicleIntelligenceService.Application.DTOs;
using VehicleIntelligenceService.Domain.Interfaces;
using System.Text.Json;

namespace VehicleIntelligenceService.Application.Features.Demand.Queries;

public record GetDemandPredictionQuery(string Make, string Model, int Year) : IRequest<DemandPredictionDto?>;

public class GetDemandPredictionHandler : IRequestHandler<GetDemandPredictionQuery, DemandPredictionDto?>
{
    private readonly IDemandPredictionRepository _repository;

    public GetDemandPredictionHandler(IDemandPredictionRepository repository)
    {
        _repository = repository;
    }

    public async Task<DemandPredictionDto?> Handle(GetDemandPredictionQuery request, CancellationToken cancellationToken)
    {
        var prediction = await _repository.GetLatestByMakeModelYearAsync(
            request.Make,
            request.Model,
            request.Year,
            cancellationToken
        );

        if (prediction == null)
            return null;

        var insights = JsonSerializer.Deserialize<List<string>>(prediction.Insights) ?? new List<string>();

        return new DemandPredictionDto
        {
            Id = prediction.Id,
            Make = prediction.Make,
            Model = prediction.Model,
            Year = prediction.Year,
            CurrentDemand = prediction.CurrentDemand.ToString(),
            DemandScore = prediction.DemandScore,
            Trend = prediction.Trend.ToString(),
            TrendStrength = prediction.TrendStrength,
            PredictedDemand30Days = prediction.PredictedDemand30Days.ToString(),
            PredictedDemand90Days = prediction.PredictedDemand90Days.ToString(),
            SearchesPerDay = prediction.SearchesPerDay,
            AvailableInventory = prediction.AvailableInventory,
            AvgDaysToSale = prediction.AvgDaysToSale,
            BuyRecommendation = prediction.BuyRecommendation.ToString(),
            BuyRecommendationReason = prediction.BuyRecommendationReason,
            Insights = insights,
            PredictionDate = prediction.PredictionDate
        };
    }
}
