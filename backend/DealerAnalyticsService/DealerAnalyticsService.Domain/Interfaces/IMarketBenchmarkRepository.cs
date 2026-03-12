using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

public interface IMarketBenchmarkRepository
{
    Task<MarketBenchmark?> GetBenchmarkAsync(string vehicleCategory, string priceRange, DateTime date);
    Task<IEnumerable<MarketBenchmark>> GetBenchmarksAsync(DateTime date);
    Task<MarketBenchmark> CreateOrUpdateBenchmarkAsync(MarketBenchmark benchmark);
    Task DeleteBenchmarkAsync(string vehicleCategory, string priceRange, DateTime date);

    // Comparison methods
    Task<decimal> CompareDealerToBenchmarkAsync(Guid dealerId, string metric, DateTime date);
    Task RecalculateBenchmarksAsync(DateTime date);
}
