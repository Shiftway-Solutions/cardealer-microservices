using MediatR;
using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Application.Features.Insights.Commands;

public record GenerateInsightsCommand(Guid DealerId) : IRequest<List<DealerInsightDto>>;

public class GenerateInsightsCommandHandler : IRequestHandler<GenerateInsightsCommand, List<DealerInsightDto>>
{
    private readonly IDealerInsightRepository _insightRepository;
    private readonly IDealerAnalyticsRepository _analyticsRepository;
    private readonly IMarketBenchmarkRepository _benchmarkRepository;

    public GenerateInsightsCommandHandler(
        IDealerInsightRepository insightRepository,
        IDealerAnalyticsRepository analyticsRepository,
        IMarketBenchmarkRepository benchmarkRepository)
    {
        _insightRepository = insightRepository;
        _analyticsRepository = analyticsRepository;
        _benchmarkRepository = benchmarkRepository;
    }

    public async Task<List<DealerInsightDto>> Handle(GenerateInsightsCommand request, CancellationToken cancellationToken)
    {
        var insights = new List<DealerInsight>();
        var today = DateTime.UtcNow;
        var thirtyDaysAgo = today.AddDays(-30);

        // Obtener analytics del dealer
        var analytics = await _analyticsRepository.GetDealerAnalyticsSummaryAsync(
            request.DealerId, thirtyDaysAgo, today);

        // Generar insights basados en performance
        await GeneratePricingInsights(request.DealerId, analytics, insights);
        await GenerateInventoryInsights(request.DealerId, analytics, insights);
        await GenerateMarketingInsights(request.DealerId, analytics, insights);

        // Guardar insights
        var savedInsights = new List<DealerInsightDto>();
        foreach (var insight in insights)
        {
            var saved = await _insightRepository.CreateInsightAsync(insight);
            savedInsights.Add(MapToDto(saved));
        }

        return savedInsights;
    }

    private async Task GeneratePricingInsights(Guid dealerId, DealerAnalytic analytics, List<DealerInsight> insights)
    {
        // Si el precio promedio está muy por encima del mercado
        if (analytics.AverageVehiclePrice > 0)
        {
            var comparison = await _benchmarkRepository.CompareDealerToBenchmarkAsync(
                dealerId, "price", DateTime.UtcNow);

            if (comparison > 15) // 15% por encima del mercado
            {
                insights.Add(new DealerInsight
                {
                    Id = Guid.NewGuid(),
                    DealerId = dealerId,
                    Type = InsightType.PricingRecommendation,
                    Priority = InsightPriority.High,
                    Title = "Precios por encima del mercado",
                    Description = $"Tus precios están un {comparison:F1}% por encima del mercado promedio.",
                    ActionRecommendation = "Considera ajustar precios o destacar valor agregado.",
                    PotentialImpact = comparison * 0.5m, // Estimado
                    Confidence = 85,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });
            }
        }
    }

    private async Task GenerateInventoryInsights(Guid dealerId, DealerAnalytic analytics, List<DealerInsight> insights)
    {
        // Si los vehículos tardan mucho en venderse
        if (analytics.AverageDaysOnMarket > 60)
        {
            insights.Add(new DealerInsight
            {
                Id = Guid.NewGuid(),
                DealerId = dealerId,
                Type = InsightType.InventoryOptimization,
                Priority = InsightPriority.Medium,
                Title = "Inventario con rotación lenta",
                Description = $"Los vehículos permanecen {analytics.AverageDaysOnMarket:F0} días en promedio.",
                ActionRecommendation = "Revisa precios o mejora presentación de listings.",
                PotentialImpact = 20,
                Confidence = 75,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14)
            });
        }
    }

    private async Task GenerateMarketingInsights(Guid dealerId, DealerAnalytic analytics, List<DealerInsight> insights)
    {
        // Si la tasa de conversión es muy baja
        if (analytics.ConversionRate < 2) // Menos del 2%
        {
            insights.Add(new DealerInsight
            {
                Id = Guid.NewGuid(),
                DealerId = dealerId,
                Type = InsightType.MarketingStrategy,
                Priority = InsightPriority.High,
                Title = "Baja tasa de conversión",
                Description = $"Tu tasa de conversión es {analytics.ConversionRate:F1}%, por debajo del promedio.",
                ActionRecommendation = "Mejora fotos, descripciones o respuesta a contactos.",
                PotentialImpact = 30,
                Confidence = 80,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(10)
            });
        }
    }

    private static DealerInsightDto MapToDto(DealerInsight insight)
    {
        return new DealerInsightDto
        {
            Id = insight.Id,
            DealerId = insight.DealerId,
            Type = insight.Type.ToString(),
            Priority = insight.Priority.ToString(),
            Title = insight.Title,
            Description = insight.Description,
            ActionRecommendation = insight.ActionRecommendation,
            PotentialImpact = insight.PotentialImpact,
            Confidence = insight.Confidence,
            IsRead = insight.IsRead,
            IsActedUpon = insight.IsActedUpon,
            ActionDate = insight.ActionDate,
            CreatedAt = insight.CreatedAt,
            UpdatedAt = insight.UpdatedAt,
            ExpiresAt = insight.ExpiresAt
        };
    }
}
