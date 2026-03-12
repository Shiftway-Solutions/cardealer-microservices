using MediatR;
using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Application.Features.Analytics.Commands;

public record RecalculateAnalyticsCommand(Guid DealerId, DateTime FromDate, DateTime ToDate) : IRequest<DealerAnalyticsDto>;

public class RecalculateAnalyticsCommandHandler : IRequestHandler<RecalculateAnalyticsCommand, DealerAnalyticsDto>
{
    private readonly IDealerAnalyticsRepository _analyticsRepository;
    private readonly IConversionFunnelRepository _funnelRepository;

    public RecalculateAnalyticsCommandHandler(
        IDealerAnalyticsRepository analyticsRepository,
        IConversionFunnelRepository funnelRepository)
    {
        _analyticsRepository = analyticsRepository;
        _funnelRepository = funnelRepository;
    }

    public async Task<DealerAnalyticsDto> Handle(RecalculateAnalyticsCommand request, CancellationToken cancellationToken)
    {
        // Recalcular analytics del dealer
        var analytics = await _analyticsRepository.GetDealerAnalyticsSummaryAsync(
            request.DealerId, request.FromDate, request.ToDate);

        // Actualizar conversión funnel
        await _funnelRepository.CalculateFunnelMetricsAsync(
            request.DealerId, request.FromDate, request.ToDate);

        return MapToDto(analytics);
    }

    private static DealerAnalyticsDto MapToDto(DealerAnalytic analytics)
    {
        return new DealerAnalyticsDto
        {
            Id = analytics.Id,
            DealerId = analytics.DealerId,
            Date = analytics.Date,
            TotalViews = analytics.TotalViews,
            UniqueViews = analytics.UniqueViews,
            AverageViewDuration = analytics.AverageViewDuration,
            TotalContacts = analytics.TotalContacts,
            PhoneCalls = analytics.PhoneCalls,
            WhatsAppMessages = analytics.WhatsAppMessages,
            EmailInquiries = analytics.EmailInquiries,
            TestDriveRequests = analytics.TestDriveRequests,
            ActualSales = analytics.ActualSales,
            ConversionRate = analytics.ConversionRate,
            TotalRevenue = analytics.TotalRevenue,
            AverageVehiclePrice = analytics.AverageVehiclePrice,
            RevenuePerView = analytics.RevenuePerView,
            ActiveListings = analytics.ActiveListings,
            AverageDaysOnMarket = analytics.AverageDaysOnMarket,
            SoldVehicles = analytics.SoldVehicles,
            CreatedAt = analytics.CreatedAt,
            UpdatedAt = analytics.UpdatedAt
        };
    }
}
