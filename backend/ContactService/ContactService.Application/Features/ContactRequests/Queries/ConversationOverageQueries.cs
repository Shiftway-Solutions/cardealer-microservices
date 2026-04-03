using System.Globalization;
using CarDealer.Contracts.Enums;
using ContactService.Application.DTOs;
using ContactService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContactService.Application.Features.ContactRequests.Queries;

// ============================================================================
// GET CONVERSATION OVERAGE REPORT
// ============================================================================

/// <summary>
/// Query to get the overage report for a dealer in a specific billing period.
/// Returns a detailed breakdown of each overage conversation with timestamp.
/// The dealer can use this to understand their overage charges.
///
/// CONTRA #5 / OVERAGE BILLING FIX
/// </summary>
public record GetConversationOverageReportQuery : IRequest<ConversationOverageReportDto>
{
    /// <summary>Dealer requesting the report.</summary>
    public Guid DealerId { get; init; }

    /// <summary>Billing period (YYYY-MM). Defaults to current month if not specified.</summary>
    public string? BillingPeriod { get; init; }
}

public class GetConversationOverageReportQueryHandler
    : IRequestHandler<GetConversationOverageReportQuery, ConversationOverageReportDto>
{
    private readonly IConversationOverageRepository _overageRepo;
    private readonly IDealerPlanResolver _planResolver;
    private readonly ILogger<GetConversationOverageReportQueryHandler> _logger;

    public GetConversationOverageReportQueryHandler(
        IConversationOverageRepository overageRepo,
        IDealerPlanResolver planResolver,
        ILogger<GetConversationOverageReportQueryHandler> logger)
    {
        _overageRepo = overageRepo;
        _planResolver = planResolver;
        _logger = logger;
    }

    public async Task<ConversationOverageReportDto> Handle(
        GetConversationOverageReportQuery request,
        CancellationToken cancellationToken)
    {
        var billingPeriod = request.BillingPeriod
            ?? DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        var dealerPlan = await _planResolver.GetDealerPlanAsync(request.DealerId, cancellationToken);
        var limits = PlanFeatureLimits.GetLimits(dealerPlan);
        var includedLimit = limits.ChatAgentMonthlyMessages;

        var details = await _overageRepo.GetByDealerAndPeriodAsync(
            request.DealerId, billingPeriod, cancellationToken);

        var overageCount = details.Count;
        var unitCost = PlanFeatureLimits.OverageCostPerConversation;
        var totalCost = overageCount * unitCost;

        _logger.LogInformation(
            "[OverageReport] Generated for Dealer={DealerId} Period={Period} Overage={Count} Total=${Total}",
            request.DealerId, billingPeriod, overageCount, totalCost);

        return new ConversationOverageReportDto
        {
            DealerId = request.DealerId,
            BillingPeriod = billingPeriod,
            DealerPlan = dealerPlan,
            IncludedLimit = includedLimit,
            TotalConversations = includedLimit + overageCount,
            OverageCount = overageCount,
            UnitCost = unitCost,
            TotalOverageCost = totalCost,
            Currency = "USD",
            Details = details.Select(d => new ConversationOverageDetailDto
            {
                Id = d.Id,
                ContactRequestId = d.ContactRequestId,
                BuyerId = d.BuyerId,
                VehicleId = d.VehicleId,
                Subject = d.Subject,
                ConversationNumber = d.ConversationNumber,
                UnitCost = d.UnitCost,
                OccurredAtUtc = d.OccurredAtUtc
            }).ToList(),
            GeneratedAtUtc = DateTime.UtcNow
        };
    }
}

// ============================================================================
// GET MONTHLY CONVERSATION USAGE COUNT
// ============================================================================

/// <summary>
/// Query to get the current-month conversation usage for a dealer.
/// Returns the count, plan limit, and usage percentage.
/// Used by the dealer dashboard to display real-time usage vs. plan limit.
/// </summary>
public record GetMonthlyConversationUsageQuery : IRequest<MonthlyConversationUsageDto>
{
    /// <summary>Dealer whose usage is being queried.</summary>
    public Guid DealerId { get; init; }
}

public class GetMonthlyConversationUsageQueryHandler
    : IRequestHandler<GetMonthlyConversationUsageQuery, MonthlyConversationUsageDto>
{
    private readonly IConversationUsageTracker _usageTracker;
    private readonly IDealerPlanResolver _planResolver;
    private readonly ILogger<GetMonthlyConversationUsageQueryHandler> _logger;

    public GetMonthlyConversationUsageQueryHandler(
        IConversationUsageTracker usageTracker,
        IDealerPlanResolver planResolver,
        ILogger<GetMonthlyConversationUsageQueryHandler> logger)
    {
        _usageTracker = usageTracker;
        _planResolver = planResolver;
        _logger = logger;
    }

    public async Task<MonthlyConversationUsageDto> Handle(
        GetMonthlyConversationUsageQuery request,
        CancellationToken cancellationToken)
    {
        var dealerPlan = await _planResolver.GetDealerPlanAsync(request.DealerId, cancellationToken);
        var limits = PlanFeatureLimits.GetLimits(dealerPlan);
        var maxAllowed = limits.ChatAgentMonthlyMessages;

        var currentCount = await _usageTracker.GetCurrentMonthCountAsync(request.DealerId, cancellationToken);

        int remaining;
        double usagePercent;
        int overageCount;

        if (maxAllowed < 0)
        {
            // Unlimited plan
            remaining = -1;
            usagePercent = 0;
            overageCount = 0;
        }
        else
        {
            remaining = Math.Max(0, maxAllowed - currentCount);
            usagePercent = maxAllowed > 0 ? (double)currentCount / maxAllowed * 100.0 : 0;
            overageCount = currentCount > maxAllowed ? currentCount - maxAllowed : 0;
        }

        var billingPeriod = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        _logger.LogInformation(
            "[MonthlyUsage] Dealer={DealerId} Plan={Plan} Count={Count}/{Max} Usage={Pct:F1}%",
            request.DealerId, dealerPlan, currentCount, maxAllowed, usagePercent);

        return new MonthlyConversationUsageDto
        {
            CurrentCount = currentCount,
            MaxAllowed = maxAllowed,
            Remaining = remaining,
            UsagePercent = usagePercent,
            OverageCount = overageCount,
            BillingPeriod = billingPeriod
        };
    }
}
