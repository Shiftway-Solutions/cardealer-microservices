using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

public interface IConversionFunnelRepository
{
    Task<ConversionFunnel?> GetFunnelAsync(Guid dealerId, DateTime date);
    Task<IEnumerable<ConversionFunnel>> GetFunnelRangeAsync(Guid dealerId, DateTime fromDate, DateTime toDate);
    Task<ConversionFunnel> CreateOrUpdateFunnelAsync(ConversionFunnel funnel);
    Task DeleteFunnelAsync(Guid dealerId, DateTime date);

    // Analytics methods
    Task<ConversionFunnel> CalculateFunnelMetricsAsync(Guid dealerId, DateTime fromDate, DateTime toDate);
    Task<decimal> GetAverageConversionRateAsync(DateTime fromDate, DateTime toDate);
}
