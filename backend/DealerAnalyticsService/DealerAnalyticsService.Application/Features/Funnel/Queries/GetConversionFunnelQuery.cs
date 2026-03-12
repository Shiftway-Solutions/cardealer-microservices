using MediatR;
using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;

namespace DealerAnalyticsService.Application.Features.Funnel.Queries;

public record GetConversionFunnelQuery(Guid DealerId, DateTime FromDate, DateTime ToDate) : IRequest<ConversionFunnelDto>;

public class GetConversionFunnelQueryHandler : IRequestHandler<GetConversionFunnelQuery, ConversionFunnelDto>
{
    private readonly IConversionFunnelRepository _funnelRepository;

    public GetConversionFunnelQueryHandler(IConversionFunnelRepository funnelRepository)
    {
        _funnelRepository = funnelRepository;
    }

    public async Task<ConversionFunnelDto> Handle(GetConversionFunnelQuery request, CancellationToken cancellationToken)
    {
        var funnel = await _funnelRepository.CalculateFunnelMetricsAsync(
            request.DealerId, request.FromDate, request.ToDate);

        return new ConversionFunnelDto
        {
            Id = funnel.Id,
            DealerId = funnel.DealerId,
            Date = funnel.Date,
            TotalViews = funnel.TotalViews,
            TotalContacts = funnel.TotalContacts,
            TestDriveRequests = funnel.TestDriveRequests,
            ActualSales = funnel.ActualSales,
            ViewToContactRate = funnel.ViewToContactRate,
            ContactToTestDriveRate = funnel.ContactToTestDriveRate,
            TestDriveToSaleRate = funnel.TestDriveToSaleRate,
            OverallConversionRate = funnel.OverallConversionRate,
            AverageTimeToSale = funnel.AverageTimeToSale
        };
    }
}
