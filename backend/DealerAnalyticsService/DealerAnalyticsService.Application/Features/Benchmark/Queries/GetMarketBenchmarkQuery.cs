using MediatR;
using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;

namespace DealerAnalyticsService.Application.Features.Benchmark.Queries;

public record GetMarketBenchmarkQuery(DateTime Date) : IRequest<List<MarketBenchmarkDto>>;

public class GetMarketBenchmarkQueryHandler : IRequestHandler<GetMarketBenchmarkQuery, List<MarketBenchmarkDto>>
{
    private readonly IMarketBenchmarkRepository _benchmarkRepository;

    public GetMarketBenchmarkQueryHandler(IMarketBenchmarkRepository benchmarkRepository)
    {
        _benchmarkRepository = benchmarkRepository;
    }

    public async Task<List<MarketBenchmarkDto>> Handle(GetMarketBenchmarkQuery request, CancellationToken cancellationToken)
    {
        var benchmarks = await _benchmarkRepository.GetBenchmarksAsync(request.Date);

        return benchmarks.Select(b => new MarketBenchmarkDto
        {
            Id = b.Id,
            Date = b.Date,
            VehicleCategory = b.VehicleCategory,
            PriceRange = b.PriceRange,
            MarketAveragePrice = b.MarketAveragePrice,
            MarketAverageDaysOnMarket = b.MarketAverageDaysOnMarket,
            MarketAverageViews = b.MarketAverageViews,
            MarketConversionRate = b.MarketConversionRate,
            PricePercentile25 = b.PricePercentile25,
            PricePercentile50 = b.PricePercentile50,
            PricePercentile75 = b.PricePercentile75,
            TotalDealersInSample = b.TotalDealersInSample,
            TotalVehiclesInSample = b.TotalVehiclesInSample
        }).ToList();
    }
}
