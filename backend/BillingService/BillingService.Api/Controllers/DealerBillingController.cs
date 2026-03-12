using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BillingService.Application.DTOs;
using BillingService.Application.Services;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para operaciones de billing específicas para dealers
/// Proporciona todos los endpoints que el frontend de dealer billing necesita
/// </summary>
[ApiController]
[Route("api/dealer-billing")]
[Authorize]
public class DealerBillingController : BillingBaseController
{
    private readonly BillingApplicationService _billingService;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<DealerBillingController> _logger;

    public DealerBillingController(
        BillingApplicationService billingService,
        ISubscriptionRepository subscriptionRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        ILogger<DealerBillingController> logger)
    {
        _billingService = billingService;
        _subscriptionRepository = subscriptionRepository;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    // ========================================
    // DASHBOARD ENDPOINTS
    // ========================================

    /// <summary>
    /// Obtiene todos los datos del dashboard de billing para un dealer
    /// </summary>
    [HttpGet("dashboard/{dealerId:guid}")]
    public async Task<ActionResult<DealerBillingDashboardResponse>> GetDashboard(
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        // Security: Validate dealerId matches JWT claim (admin can override)
        dealerId = GetDealerIdOrOverride(dealerId);
        try
        {
            var summary = await _billingService.GetBillingSummaryAsync(dealerId, cancellationToken);
            var plans = await _billingService.GetPlanPricingAsync(dealerId, cancellationToken);

            // Calcular usage metrics
            var subscription = await _subscriptionRepository.GetByDealerIdAsync(dealerId, cancellationToken);
            var usage = await CalculateUsageMetrics(dealerId, subscription, cancellationToken);
            var stats = await CalculateBillingStats(dealerId, subscription, cancellationToken);

            return Ok(new DealerBillingDashboardResponse(
                Summary: summary,
                Plans: plans,
                Usage: usage,
                Stats: stats
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing dashboard for dealer {DealerId}", dealerId);
            return StatusCode(500, new { error = "Error fetching billing dashboard" });
        }
    }

    /// <summary>
    /// Obtiene la suscripción actual de un dealer
    /// </summary>
    [HttpGet("subscription")]
    public async Task<ActionResult<DealerSubscriptionDto>> GetSubscription(
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        var subscription = await _subscriptionRepository.GetByDealerIdAsync(dealerId, cancellationToken);
        if (subscription == null)
        {
            // Return a default free subscription info
            return Ok(new DealerSubscriptionDto(
                Id: Guid.Empty.ToString(),
                DealerId: dealerId.ToString(),
                Plan: "free",
                Status: "active",
                Cycle: "monthly",
                PricePerCycle: 0,
                Currency: "USD",
                StartDate: DateTime.UtcNow.ToString("o"),
                NextBillingDate: null,
                MaxUsers: 1,
                MaxVehicles: 3,
                Features: GetDefaultFeatures("free")
            ));
        }

        return Ok(MapToSubscriptionDto(subscription));
    }

    /// <summary>
    /// Obtiene las facturas de un dealer
    /// </summary>
    [HttpGet("invoices")]
    public async Task<ActionResult<List<DealerInvoiceDto>>> GetInvoices(
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        var invoices = await _invoiceRepository.GetByDealerIdAsync(dealerId, cancellationToken);
        return Ok(invoices.Select(MapToInvoiceDto).ToList());
    }

    /// <summary>
    /// Obtiene los pagos de un dealer
    /// </summary>
    [HttpGet("payments")]
    public async Task<ActionResult<List<DealerPaymentDto>>> GetPayments(
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        var payments = await _paymentRepository.GetByDealerIdAsync(dealerId, cancellationToken);
        return Ok(payments.Select(MapToPaymentDto).ToList());
    }

    /// <summary>
    /// Obtiene métricas de uso para un dealer
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<UsageMetricsDto>> GetUsage(
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        var subscription = await _subscriptionRepository.GetByDealerIdAsync(dealerId, cancellationToken);
        var usage = await CalculateUsageMetrics(dealerId, subscription, cancellationToken);
        return Ok(usage);
    }

    /// <summary>
    /// Obtiene estadísticas de billing para un dealer
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<BillingStatsDto>> GetStats(
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        var subscription = await _subscriptionRepository.GetByDealerIdAsync(dealerId, cancellationToken);
        var stats = await CalculateBillingStats(dealerId, subscription, cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Obtiene todos los planes disponibles
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous] // Plans are public for pricing page
    public async Task<ActionResult<List<PlanConfigDto>>> GetPlans(
        [FromHeader(Name = "X-Dealer-Id")] Guid? dealerId = null,
        CancellationToken cancellationToken = default)
    {
        var plans = await _billingService.GetPlanPricingAsync(dealerId, cancellationToken);
        return Ok(plans.Select(MapToPlanConfigDto).ToList());
    }

    /// <summary>
    /// Obtiene los métodos de pago de un dealer
    /// </summary>
    [HttpGet("payment-methods")]
    public async Task<ActionResult<List<PaymentMethodDto>>> GetPaymentMethods(
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        // For now, return mock data - in production, this would come from Stripe
        var customer = await _billingService.GetCustomerByDealerIdAsync(dealerId, cancellationToken);

        if (customer?.PaymentMethods != null && customer.PaymentMethods.Any())
        {
            return Ok(customer.PaymentMethods.Select(pm => new PaymentMethodDto(
                Id: pm.Id,
                Type: pm.Type == "card" ? "card" : "bank_account",
                IsDefault: pm.IsDefault,
                Card: pm.Type == "card" ? new CardInfoDto(
                    Brand: pm.Brand ?? "Unknown",
                    Last4: pm.Last4 ?? "****",
                    ExpMonth: pm.ExpMonth ?? 12,
                    ExpYear: pm.ExpYear ?? 2025
                ) : null,
                BankAccount: null,
                CreatedAt: DateTime.UtcNow.ToString("o")
            )).ToList());
        }

        // Return empty list if no payment methods
        return Ok(new List<PaymentMethodDto>());
    }

    /// <summary>
    /// Add a new payment method (card or bank account)
    /// </summary>
    [HttpPost("payment-methods")]
    public async Task<ActionResult<PaymentMethodDto>> AddPaymentMethod(
        [FromBody] AddPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        _logger.LogInformation("Adding payment method for dealer {DealerId}", dealerId);

        // In production, this would:
        // 1. Call Stripe/Azul to create the payment method
        // 2. Store the token reference in our database
        // 3. Return the created payment method

        var newPaymentMethod = new PaymentMethodDto(
            Id: Guid.NewGuid().ToString(),
            Type: request.Type,
            IsDefault: request.SetAsDefault,
            Card: request.Type == "card" ? new CardInfoDto(
                Brand: "Visa", // Would come from payment processor
                Last4: "****",
                ExpMonth: request.ExpMonth ?? 12,
                ExpYear: request.ExpYear ?? DateTime.UtcNow.Year + 3
            ) : null,
            BankAccount: null,
            CreatedAt: DateTime.UtcNow.ToString("o")
        );

        return Ok(newPaymentMethod);
    }

    /// <summary>
    /// Set a payment method as default
    /// </summary>
    [HttpPut("payment-methods/{paymentMethodId}/default")]
    public async Task<ActionResult> SetDefaultPaymentMethod(
        [FromRoute] string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        _logger.LogInformation("Setting default payment method {PaymentMethodId} for dealer {DealerId}",
            paymentMethodId, dealerId);

        // In production, this would:
        // 1. Update the default payment method in Stripe/Azul
        // 2. Update our database records

        return Ok(new { Success = true, Message = "Default payment method updated" });
    }

    /// <summary>
    /// Remove a payment method
    /// </summary>
    [HttpDelete("payment-methods/{paymentMethodId}")]
    public async Task<ActionResult> RemovePaymentMethod(
        [FromRoute] string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        var dealerId = GetDealerIdFromJwt();
        _logger.LogInformation("Removing payment method {PaymentMethodId} for dealer {DealerId}",
            paymentMethodId, dealerId);

        // In production, this would:
        // 1. Detach the payment method from Stripe/Azul
        // 2. Remove from our database
        // 3. Verify it's not the only payment method if subscription is active

        return Ok(new { Success = true, Message = "Payment method removed" });
    }

    // ========================================
    // PRIVATE HELPER METHODS
    // ========================================


    private async Task<UsageMetricsDto> CalculateUsageMetrics(
        Guid dealerId,
        Subscription? subscription,
        CancellationToken cancellationToken)
    {
        // TODO: Get actual usage from VehiclesSaleService
        // For now, return sample data based on plan
        var plan = subscription?.Plan ?? SubscriptionPlan.Free;
        var (maxUsers, maxVehicles) = StripePriceMapping.GetLimits(plan);

        // Simulated usage - in production, query actual vehicle count from VehiclesSaleService
        var currentListings = plan switch
        {
            SubscriptionPlan.Free => 2,
            SubscriptionPlan.Basic => 25,
            SubscriptionPlan.Professional => 150,
            SubscriptionPlan.Enterprise => 500,
            _ => 0
        };

        var currentUsers = plan switch
        {
            SubscriptionPlan.Free => 1,
            SubscriptionPlan.Basic => 3,
            SubscriptionPlan.Professional => 10,
            SubscriptionPlan.Enterprise => 25,
            _ => 1
        };

        return new UsageMetricsDto(
            CurrentListings: currentListings,
            MaxListings: maxVehicles == -1 ? "unlimited" : maxVehicles.ToString(),
            CurrentUsers: currentUsers,
            MaxUsers: maxUsers == -1 ? "unlimited" : maxUsers.ToString(),
            StorageUsed: "2.5 GB",
            StorageLimit: plan == SubscriptionPlan.Enterprise ? "unlimited" : "10 GB",
            ApiCalls: 15420,
            ApiLimit: plan == SubscriptionPlan.Enterprise ? "unlimited" : "50000"
        );
    }

    private async Task<BillingStatsDto> CalculateBillingStats(
        Guid dealerId,
        Subscription? subscription,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoiceRepository.GetByDealerIdAsync(dealerId, cancellationToken);
        var payments = await _paymentRepository.GetByDealerIdAsync(dealerId, cancellationToken);

        var currentYear = DateTime.UtcNow.Year;
        var yearlyPayments = payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.CreatedAt.Year == currentYear)
            .ToList();

        var monthlySpend = yearlyPayments
            .Where(p => p.CreatedAt.Month == DateTime.UtcNow.Month)
            .Sum(p => p.Amount);

        var yearlySpend = yearlyPayments.Sum(p => p.Amount);

        var outstandingBalance = invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Voided)
            .Sum(i => i.TotalAmount - i.PaidAmount);

        var totalPaid = payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .Sum(p => p.Amount);

        return new BillingStatsDto(
            CurrentPlan: subscription?.Plan.ToString().ToLower() ?? "free",
            MonthlySpend: monthlySpend,
            YearlySpend: yearlySpend,
            OutstandingBalance: outstandingBalance,
            NextBillingAmount: subscription?.PricePerCycle ?? 0,
            NextBillingDate: subscription?.NextBillingDate?.ToString("o") ?? DateTime.UtcNow.AddMonths(1).ToString("o"),
            TotalPaid: totalPaid,
            InvoiceCount: invoices.Count()
        );
    }

    private DealerSubscriptionDto MapToSubscriptionDto(Subscription sub)
    {
        return new DealerSubscriptionDto(
            Id: sub.Id.ToString(),
            DealerId: sub.DealerId.ToString(),
            Plan: sub.Plan.ToString().ToLower(),
            Status: sub.Status.ToString().ToLower(),
            Cycle: sub.Cycle.ToString().ToLower(),
            PricePerCycle: sub.PricePerCycle,
            Currency: sub.Currency,
            StartDate: sub.StartDate.ToString("o"),
            NextBillingDate: sub.NextBillingDate?.ToString("o"),
            MaxUsers: sub.MaxUsers,
            MaxVehicles: sub.MaxVehicles,
            Features: GetDefaultFeatures(sub.Plan.ToString().ToLower())
        );
    }

    private DealerInvoiceDto MapToInvoiceDto(Invoice inv)
    {
        return new DealerInvoiceDto(
            Id: inv.Id.ToString(),
            DealerId: inv.DealerId.ToString(),
            InvoiceNumber: inv.InvoiceNumber,
            SubscriptionId: inv.SubscriptionId?.ToString(),
            Status: inv.Status.ToString().ToLower().Replace("_", "-"),
            Subtotal: inv.Subtotal,
            TaxAmount: inv.TaxAmount,
            TotalAmount: inv.TotalAmount,
            PaidAmount: inv.PaidAmount,
            Currency: inv.Currency,
            IssueDate: inv.IssueDate.ToString("o"),
            DueDate: inv.DueDate.ToString("o"),
            PaidDate: inv.PaidDate?.ToString("o"),
            PdfUrl: inv.PdfUrl
        );
    }

    private DealerPaymentDto MapToPaymentDto(Payment pay)
    {
        return new DealerPaymentDto(
            Id: pay.Id.ToString(),
            DealerId: pay.DealerId.ToString(),
            SubscriptionId: pay.SubscriptionId?.ToString(),
            InvoiceId: pay.InvoiceId?.ToString(),
            Amount: pay.Amount,
            Currency: pay.Currency,
            Status: pay.Status.ToString().ToLower(),
            Method: pay.Method.ToString().ToLower().Replace("_", "-"),
            Description: pay.Description,
            ReceiptUrl: pay.ReceiptUrl,
            RefundedAmount: pay.RefundedAmount,
            CreatedAt: pay.CreatedAt.ToString("o"),
            ProcessedAt: pay.ProcessedAt?.ToString("o"),
            CardLast4: "4242", // In production, get from Stripe
            CardBrand: "Visa"  // In production, get from Stripe
        );
    }

    private PlanConfigDto MapToPlanConfigDto(PlanPricingInfo plan)
    {
        // Map to OKLA v2 display names
        var displayName = plan.Plan switch
        {
            SubscriptionPlan.Free => "Libre",
            SubscriptionPlan.Basic => "Visible",
            SubscriptionPlan.Professional => "Pro",
            SubscriptionPlan.Enterprise => "Elite",
            _ => plan.PlanName
        };

        return new PlanConfigDto(
            Id: plan.Plan.ToString().ToLower(),
            Name: displayName,
            Description: GetPlanDescription(plan.Plan),
            Prices: new PlanPricesDto(
                Monthly: plan.MonthlyPrice,
                Quarterly: plan.MonthlyPrice * 3 * 0.9m, // 10% discount for quarterly
                Yearly: plan.YearlyPrice
            ),
            Features: GetDefaultFeatures(plan.Plan.ToString().ToLower()),
            Popular: plan.Plan == SubscriptionPlan.Professional, // Pro is the recommended plan
            Enterprise: plan.Plan == SubscriptionPlan.Enterprise
        );
    }

    private string GetPlanDescription(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => "Libre — Publicaciones ilimitadas, hasta 10 fotos, posición estándar, 1 valoración IA gratis, perfil básico, soporte FAQ.",
        SubscriptionPlan.Basic => "Visible $29/mes — Publicaciones ilimitadas, hasta 20 fotos, prioridad media, 3 destacados/mes, $15 créditos, badge Verificado, analytics básico, 5 valoraciones IA/mes, perfil mejorado, soporte email 48h.",
        SubscriptionPlan.Professional => "Pro $89/mes — Publicaciones ilimitadas, hasta 30 fotos, alta prioridad, 10 destacados/mes, $45 créditos, ChatAgent Web+WhatsApp 500 conv/mes, agendamiento automático, human handoff email, valoración IA ilimitada, perfil premium, soporte chat 12h.",
        SubscriptionPlan.Enterprise => "Elite $199/mes — Publicaciones ilimitadas, hasta 40 fotos + video tour, top prioridad, 25 destacados/mes, $120 créditos, badge Premium dorado, ChatAgent ilimitado + WhatsApp, recordatorios WA, live chat + CRM handoff, valoración IA ilimitada + informe PDF, perfil premium + showcase homepage, soporte dedicado 4h.",
        _ => "Plan personalizado"
    };

    private FeaturesDto GetDefaultFeatures(string plan) => plan.ToLower() switch
    {
        // LIBRE: Publicaciones ∞ ILIMITADAS, 10 fotos, posición estándar, 1 valoración IA gratis
        "free" => new FeaturesDto(
            Listings: -1, Users: 1, Storage: "1 GB",
            Analytics: false, Api: false, CustomBranding: false, PrioritySupport: false,
            DedicatedManager: false, BulkUpload: false, Marketplace: false, RealEstate: false,
            Crm: false, Reporting: false,
            MaxPhotosPerListing: 10, FeaturedListingsPerMonth: 0, OklaCoinsMonthly: 0,
            ChatAgentConversations: 0, VideoTourEnabled: false, VerifiedBadge: false, WhatsAppIntegration: false,
            SearchPriority: "standard", PricingAgentValuations: 1, DealerProfileType: "basic",
            SupportLevel: "faq", AutoScheduling: false, HumanHandoff: "none"),
        // VISIBLE $29/mes: ∞ ILIMITADAS, 20 fotos, prioridad media, 3 destacados, $15 créditos
        "basic" => new FeaturesDto(
            Listings: -1, Users: 5, Storage: "5 GB",
            Analytics: true, Api: false, CustomBranding: false, PrioritySupport: false,
            DedicatedManager: false, BulkUpload: true, Marketplace: true, RealEstate: false,
            Crm: true, Reporting: true,
            MaxPhotosPerListing: 20, FeaturedListingsPerMonth: 3, OklaCoinsMonthly: 15,
            ChatAgentConversations: 0, VideoTourEnabled: false, VerifiedBadge: true, WhatsAppIntegration: false,
            SearchPriority: "medium", PricingAgentValuations: 5, DealerProfileType: "enhanced",
            SupportLevel: "email_48h", AutoScheduling: false, HumanHandoff: "none"),
        // PRO $89/mes: ∞ ILIMITADAS, 30 fotos, alta prioridad, 10 destacados, $45 créditos, ChatAgent 500 conv
        "professional" => new FeaturesDto(
            Listings: -1, Users: 20, Storage: "20 GB",
            Analytics: true, Api: false, CustomBranding: true, PrioritySupport: true,
            DedicatedManager: false, BulkUpload: true, Marketplace: true, RealEstate: true,
            Crm: true, Reporting: true,
            MaxPhotosPerListing: 30, FeaturedListingsPerMonth: 10, OklaCoinsMonthly: 45,
            ChatAgentConversations: 500, VideoTourEnabled: false, VerifiedBadge: true, WhatsAppIntegration: true,
            SearchPriority: "high", PricingAgentValuations: -1, DealerProfileType: "premium",
            SupportLevel: "chat_12h", AutoScheduling: true, HumanHandoff: "email_alert"),
        // ÉLITE $199/mes: ∞ ILIMITADAS, 40 fotos + video tour, top prioridad, 25 destacados, $120 créditos
        "enterprise" => new FeaturesDto(
            Listings: -1, Users: -1, Storage: "unlimited",
            Analytics: true, Api: true, CustomBranding: true, PrioritySupport: true,
            DedicatedManager: true, BulkUpload: true, Marketplace: true, RealEstate: true,
            Crm: true, Reporting: true,
            MaxPhotosPerListing: 40, FeaturedListingsPerMonth: 25, OklaCoinsMonthly: 120,
            ChatAgentConversations: -1, VideoTourEnabled: true, VerifiedBadge: true, WhatsAppIntegration: true,
            SearchPriority: "top", PricingAgentValuations: -1, DealerProfileType: "premium_showcase",
            SupportLevel: "dedicated_4h", AutoScheduling: true, HumanHandoff: "live_chat_crm"),
        _ => new FeaturesDto(
            Listings: -1, Users: 1, Storage: "1 GB",
            Analytics: false, Api: false, CustomBranding: false, PrioritySupport: false,
            DedicatedManager: false, BulkUpload: false, Marketplace: false, RealEstate: false,
            Crm: false, Reporting: false,
            MaxPhotosPerListing: 10, FeaturedListingsPerMonth: 0, OklaCoinsMonthly: 0,
            ChatAgentConversations: 0, VideoTourEnabled: false, VerifiedBadge: false, WhatsAppIntegration: false,
            SearchPriority: "standard", PricingAgentValuations: 1, DealerProfileType: "basic",
            SupportLevel: "faq", AutoScheduling: false, HumanHandoff: "none")
    };
}

// ========================================
// DTOs for Dealer Billing
// ========================================

public record DealerBillingDashboardResponse(
    BillingSummaryResponse Summary,
    List<PlanPricingInfo> Plans,
    UsageMetricsDto Usage,
    BillingStatsDto Stats
);

public record DealerSubscriptionDto(
    string Id,
    string DealerId,
    string Plan,
    string Status,
    string Cycle,
    decimal PricePerCycle,
    string Currency,
    string StartDate,
    string? NextBillingDate,
    int MaxUsers,
    int MaxVehicles,
    FeaturesDto Features
);

public record DealerInvoiceDto(
    string Id,
    string DealerId,
    string InvoiceNumber,
    string? SubscriptionId,
    string Status,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    string Currency,
    string IssueDate,
    string DueDate,
    string? PaidDate,
    string? PdfUrl
);

public record DealerPaymentDto(
    string Id,
    string DealerId,
    string? SubscriptionId,
    string? InvoiceId,
    decimal Amount,
    string Currency,
    string Status,
    string Method,
    string? Description,
    string? ReceiptUrl,
    decimal RefundedAmount,
    string CreatedAt,
    string? ProcessedAt,
    string? CardLast4,
    string? CardBrand
);

public record UsageMetricsDto(
    int CurrentListings,
    string MaxListings,
    int CurrentUsers,
    string MaxUsers,
    string StorageUsed,
    string StorageLimit,
    int ApiCalls,
    string ApiLimit
);

public record BillingStatsDto(
    string CurrentPlan,
    decimal MonthlySpend,
    decimal YearlySpend,
    decimal OutstandingBalance,
    decimal NextBillingAmount,
    string NextBillingDate,
    decimal TotalPaid,
    int InvoiceCount
);

public record PlanConfigDto(
    string Id,
    string Name,
    string Description,
    PlanPricesDto Prices,
    FeaturesDto Features,
    bool Popular,
    bool Enterprise
);

public record PlanPricesDto(
    decimal Monthly,
    decimal Quarterly,
    decimal Yearly
);

public record FeaturesDto(
    int Listings,
    int Users,
    string Storage,
    bool Analytics,
    bool Api,
    bool CustomBranding,
    bool PrioritySupport,
    bool DedicatedManager,
    bool BulkUpload,
    bool Marketplace,
    bool RealEstate,
    bool Crm,
    bool Reporting,
    // Plan-differentiating features (OKLA v2 — matches plan table)
    int MaxPhotosPerListing = 10,
    int FeaturedListingsPerMonth = 0,
    int OklaCoinsMonthly = 0,
    int ChatAgentConversations = 0,           // -1 = unlimited (Elite)
    bool VideoTourEnabled = false,
    bool VerifiedBadge = false,
    bool WhatsAppIntegration = false,
    // New features from OKLA plan table
    string SearchPriority = "standard",       // standard | medium | high | top
    int PricingAgentValuations = 1,           // -1 = unlimited
    string DealerProfileType = "basic",       // basic | enhanced | premium | premium_showcase
    string SupportLevel = "faq",              // faq | email_48h | chat_12h | dedicated_4h
    bool AutoScheduling = false,              // Agendamiento de citas automático
    string HumanHandoff = "none"              // none | email_alert | live_chat_crm
);

public record PaymentMethodDto(
    string Id,
    string Type,
    bool IsDefault,
    CardInfoDto? Card,
    BankAccountInfoDto? BankAccount,
    string CreatedAt
);

public record CardInfoDto(
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear
);

public record BankAccountInfoDto(
    string BankName,
    string Last4,
    string AccountType
);
