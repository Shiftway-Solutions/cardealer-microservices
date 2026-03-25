namespace CarDealer.Contracts.Enums;

/// <summary>
/// Backend single source of truth for plan feature limits.
/// Mirrors the frontend plan-config.ts to ensure backend enforcement.
/// 
/// KPI AUDIT: Critical gap — frontend defined feature limits but backend
/// had NO enforcement, allowing features to be used without paying.
/// </summary>
public static class PlanFeatureLimits
{
    /// <summary>
    /// Get the feature limits for a given plan.
    /// </summary>
    public static PlanLimits GetLimits(string planName)
    {
        var frontendKey = PlanConfiguration.GetFrontendKey(planName);
        return frontendKey switch
        {
            "libre" => LibreLimits,
            "visible" => VisibleLimits,
            "pro" => ProLimits,
            "elite" => EliteLimits,
            _ => LibreLimits, // Default to most restrictive
        };
    }

    /// <summary>
    /// Check if a specific feature is allowed for the given plan.
    /// </summary>
    public static bool IsFeatureAllowed(string planName, string featureName)
    {
        var limits = GetLimits(planName);
        return featureName.ToLowerInvariant() switch
        {
            "lead_management" => limits.HasLeadManagement,
            "bulk_upload" => limits.HasBulkUpload,
            "priority_support" => limits.HasPrioritySupport,
            "custom_branding" => limits.HasCustomBranding,
            "whatsapp_integration" => limits.HasWhatsAppIntegration,
            "video_tour" => limits.HasVideoTour,
            "api_access" => limits.HasApiAccess,
            "export_analytics" => limits.HasExportAnalytics,
            "dedicated_account_manager" => limits.HasDedicatedAccountManager,
            "competitor_analysis" => limits.HasCompetitorAnalysis,
            _ => false,
        };
    }

    /// <summary>
    /// Check if a numeric limit is within bounds.
    /// Returns true if the current count is below the limit. -1 = unlimited.
    /// </summary>
    public static bool IsWithinLimit(string planName, string limitName, int currentCount)
    {
        var limits = GetLimits(planName);
        var maxValue = limitName.ToLowerInvariant() switch
        {
            "max_images_per_vehicle" => limits.MaxImagesPerVehicle,
            "max_featured_listings" => limits.MaxFeaturedListingsPerMonth,
            "monthly_coins" => limits.MonthlyCoins,
            "chat_agent_monthly" => limits.ChatAgentMonthlyMessages,
            "recos_agent_monthly" => limits.RecosAgentMonthlyMessages,
            "max_promotions" => limits.MaxPromotionsPerMonth,
            "max_saved_searches" => limits.MaxSavedSearches,
            _ => 0,
        };

        // -1 means unlimited
        if (maxValue == -1) return true;
        return currentCount < maxValue;
    }

    // ═══════════════════════════════════════════
    // PLAN DEFINITIONS — Mirror of frontend plan-config.ts
    // ═══════════════════════════════════════════

    public static readonly PlanLimits LibreLimits = new()
    {
        PlanKey = "libre",
        DisplayName = "Libre",
        PricePerMonth = 0m,
        MaxImagesPerVehicle = 10,
        MaxFeaturedListingsPerMonth = 0,
        SearchPriority = "standard",
        MonthlyCoins = 0,
        BadgeType = "none",
        ChatAgentMonthlyMessages = 0,
        RecosAgentMonthlyMessages = 0,
        MaxPromotionsPerMonth = 1,
        MaxSavedSearches = 0,
        AnalyticsDashboard = "none",
        MaxStaffAccounts = 0,
        HasLeadManagement = false,
        HasBulkUpload = false,
        HasPrioritySupport = false,
        HasCustomBranding = false,
        HasWhatsAppIntegration = false,
        HasVideoTour = false,
        HasApiAccess = false,
        HasExportAnalytics = false,
        HasDedicatedAccountManager = false,
        HasCompetitorAnalysis = false,
    };

    public static readonly PlanLimits VisibleLimits = new()
    {
        PlanKey = "visible",
        DisplayName = "Visible",
        PricePerMonth = 29m,
        MaxImagesPerVehicle = 20,
        MaxFeaturedListingsPerMonth = 3,
        SearchPriority = "medium",
        MonthlyCoins = 15,
        BadgeType = "verified",
        ChatAgentMonthlyMessages = 0,
        RecosAgentMonthlyMessages = 0,
        MaxPromotionsPerMonth = 0,
        MaxSavedSearches = 5,
        AnalyticsDashboard = "basic",
        MaxStaffAccounts = 1,
        HasLeadManagement = true,
        HasBulkUpload = true,
        HasPrioritySupport = true,
        HasCustomBranding = false,
        HasWhatsAppIntegration = false,
        HasVideoTour = false,
        HasApiAccess = false,
        HasExportAnalytics = false,
        HasDedicatedAccountManager = false,
        HasCompetitorAnalysis = false,
    };

    public static readonly PlanLimits ProLimits = new()
    {
        PlanKey = "pro",
        DisplayName = "Pro",
        PricePerMonth = 89m,
        MaxImagesPerVehicle = 30,
        MaxFeaturedListingsPerMonth = 10,
        SearchPriority = "high",
        MonthlyCoins = 45,
        BadgeType = "verified-gold",
        ChatAgentMonthlyMessages = 500,
        RecosAgentMonthlyMessages = 500,
        MaxPromotionsPerMonth = 0,
        MaxSavedSearches = -1, // Unlimited
        AnalyticsDashboard = "advanced",
        MaxStaffAccounts = 2,
        HasLeadManagement = true,
        HasBulkUpload = true,
        HasPrioritySupport = true,
        HasCustomBranding = true,
        HasWhatsAppIntegration = true,
        HasVideoTour = true,
        HasApiAccess = false,
        HasExportAnalytics = false,
        HasDedicatedAccountManager = false,
        HasCompetitorAnalysis = true,
    };

    public static readonly PlanLimits EliteLimits = new()
    {
        PlanKey = "elite",
        DisplayName = "Elite",
        PricePerMonth = 199m,
        MaxImagesPerVehicle = 40,
        MaxFeaturedListingsPerMonth = 25,
        SearchPriority = "top",
        MonthlyCoins = 120,
        BadgeType = "verified-premium",
        ChatAgentMonthlyMessages = 2000, // Hard limit: 2,000 conversations/month — CONTRA #5 fix
        RecosAgentMonthlyMessages = -1, // Unlimited
        MaxPromotionsPerMonth = 0,
        MaxSavedSearches = -1, // Unlimited
        AnalyticsDashboard = "complete",
        MaxStaffAccounts = 5,
        HasLeadManagement = true,
        HasBulkUpload = true,
        HasPrioritySupport = true,
        HasCustomBranding = true,
        HasWhatsAppIntegration = true,
        HasVideoTour = true,
        HasApiAccess = true,
        HasExportAnalytics = true,
        HasDedicatedAccountManager = true,
        HasCompetitorAnalysis = true,
    };

    public static readonly PlanLimits StarterLimits = new()
    {
        PlanKey = "starter",
        DisplayName = "Starter",
        PricePerMonth = 59m,
        MaxImagesPerVehicle = 15,
        MaxFeaturedListingsPerMonth = 5,
        SearchPriority = "high",
        MonthlyCoins = 30,
        BadgeType = "verified-plus",
        ChatAgentMonthlyMessages = 100,
        RecosAgentMonthlyMessages = 100,
        MaxPromotionsPerMonth = 0,
        MaxSavedSearches = 10,
        AnalyticsDashboard = "basic",
        MaxStaffAccounts = 1,
        HasLeadManagement = true,
        HasBulkUpload = true,
        HasPrioritySupport = false,
        HasCustomBranding = false,
        HasWhatsAppIntegration = false,
        HasVideoTour = false,
        HasApiAccess = false,
        HasExportAnalytics = false,
        HasDedicatedAccountManager = false,
        HasCompetitorAnalysis = false,
    };

    public static readonly PlanLimits EnterpriseLimits = new()
    {
        PlanKey = "enterprise",
        DisplayName = "Enterprise",
        PricePerMonth = 599m,
        MaxImagesPerVehicle = 20,
        MaxFeaturedListingsPerMonth = 50,
        SearchPriority = "top",
        MonthlyCoins = 300,
        BadgeType = "enterprise",
        ChatAgentMonthlyMessages = -1, // Unlimited
        RecosAgentMonthlyMessages = -1, // Unlimited
        MaxPromotionsPerMonth = 0,
        MaxSavedSearches = -1, // Unlimited
        AnalyticsDashboard = "complete",
        MaxStaffAccounts = -1, // Unlimited
        HasLeadManagement = true,
        HasBulkUpload = true,
        HasPrioritySupport = true,
        HasCustomBranding = true,
        HasWhatsAppIntegration = true,
        HasVideoTour = true,
        HasApiAccess = true,
        HasExportAnalytics = true,
        HasDedicatedAccountManager = true,
        HasCompetitorAnalysis = true,
    };

    /// <summary>
    /// All available plans ordered by tier.
    /// </summary>
    public static readonly PlanLimits[] AllPlans = { LibreLimits, VisibleLimits, StarterLimits, ProLimits, EliteLimits, EnterpriseLimits };

    // ═══════════════════════════════════════════
    // CONVERSATION LIMIT CONSTANTS — CONTRA #5 (Elite Negative Margin Fix)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Percentage of monthly conversation limit at which a warning notification
    /// is sent to the dealer via WhatsApp and email (80% = 1,600 for Elite).
    /// </summary>
    public const double ConversationWarningThreshold = 0.80;

    /// <summary>
    /// Overage cost per conversation beyond the hard limit ($0.08 per conversation).
    /// Conversations 2,001+ are billed at this rate on the next invoice.
    /// </summary>
    public const decimal OverageCostPerConversation = 0.08m;

    /// <summary>
    /// Maximum projected monthly API cost for an Elite dealer before an internal alert fires.
    /// At 90% of the plan's revenue ($199 × 0.90 = $179.10), the team is alerted.
    /// </summary>
    public const decimal EliteCostAlertThreshold = 179.10m;

    // ═══════════════════════════════════════════
    // DEGRADATION ZONE — Soft limit with progressive cache escalation
    // Between 80%–100% of plan limit (1,600–2,000 for ÉLITE), the system
    // aggressively activates FAQ cache + cheaper providers to reduce the
    // marginal cost per conversation to ≤ $0.04.
    // ═══════════════════════════════════════════

    /// <summary>
    /// Maximum target cost per conversation in the degradation zone (80%–100% of limit).
    /// The gateway routes to cheaper providers + extended cache to hit this target.
    /// $0.04 = half the overage billing rate ($0.08), ensuring marginal profitability.
    /// </summary>
    public const decimal DegradationZoneMaxCostPerConversation = 0.04m;

    /// <summary>
    /// Extended cache TTL for dealers in the degradation zone.
    /// Standard 24h → 72h when usage ≥ 80%, ensuring FAQ-like queries are served from cache.
    /// </summary>
    public static readonly TimeSpan DegradationZoneCacheTtl = TimeSpan.FromHours(72);

    /// <summary>
    /// Traffic split percentages for degradation zone.
    /// Prioritize Gemini Flash (cheap) and Llama (free) over Claude (expensive).
    /// </summary>
    public const int DegradationClaudePercent = 15;
    public const int DegradationGeminiPercent = 60;
    public const int DegradationLlamaPercent = 25;

    // ═══════════════════════════════════════════════════════════════════════
    // OPEX & REVENUE THRESHOLD MONITORING — CONTRA #7 FIX
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Monthly Operating Expenses threshold.
    /// When projected monthly revenue falls below this amount, an automatic alert
    /// is sent to the founder with sufficient lead time for conversion actions.
    /// Breakdown: Infrastructure $285 + Development $1,600 + Marketing $200 + Buffer $130 = $2,215.
    /// </summary>
    public const decimal MonthlyOpexThreshold = 2215m;

    /// <summary>
    /// How often (in hours) to check revenue projection against OPEX threshold.
    /// Every 6 hours = 4 checks per day, giving the founder ~6h notice windows.
    /// </summary>
    public const int RevenueAlertCheckIntervalHours = 6;

    /// <summary>
    /// Minimum day-of-month before revenue projection alerts activate.
    /// Before day 5, projections are too volatile (small sample = wild extrapolations).
    /// </summary>
    public const int RevenueAlertMinDayOfMonth = 5;

    /// <summary>
    /// Safety margin multiplier for early-month alerts.
    /// If we're before day 15, require projected revenue to be at least (OPEX × 1.15)
    /// to account for projection volatility. After day 15, use exact threshold.
    /// </summary>
    public const decimal RevenueAlertEarlyMonthSafetyMargin = 1.15m;

    // ═══════════════════════════════════════════════════════════════════════
    // INFRASTRUCTURE COST CONTROL — CONTRA #8 FIX
    // DigitalOcean cloud budget alert + runbook for cost reduction.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maximum monthly DigitalOcean cloud budget before an alert fires.
    /// Current baseline ~$139/mo, budget ceiling $210 gives 51% headroom.
    /// Alert tiers: Warning ($168 = 80%), Critical ($189 = 90%), Emergency ($210 = 100%).
    /// </summary>
    public const decimal DigitalOceanMonthlyBudget = 210m;

    /// <summary>
    /// Warning threshold percentage of DigitalOcean budget (80% = $168).
    /// Fires a platform-team alert to review resource usage.
    /// </summary>
    public const decimal InfraCostWarningPercent = 0.80m;

    /// <summary>
    /// Critical threshold percentage of DigitalOcean budget (90% = $189).
    /// Fires a CTO alert requiring acknowledgment within 1 hour.
    /// </summary>
    public const decimal InfraCostCriticalPercent = 0.90m;

    /// <summary>
    /// How often (in hours) to check infrastructure cost against budget.
    /// Every 4 hours = 6 checks/day, faster than revenue checks due to cloud bursts.
    /// </summary>
    public const int InfraCostAlertCheckIntervalHours = 4;

    /// <summary>
    /// Runbook URL for infrastructure cost reduction procedures.
    /// </summary>
    public const string InfraCostRunbookUrl = "https://github.com/okla-rd/cardealer-microservices/blob/main/docs/runbooks/RUNBOOK_INFRA_COST.md";

    /// <summary>
    /// Determines whether a dealer is in the degradation zone (80%–100% of plan limit).
    /// </summary>
    public static bool IsInDegradationZone(string planName, int currentCount)
    {
        var limits = GetLimits(planName);
        var maxMessages = limits.ChatAgentMonthlyMessages;
        if (maxMessages <= 0) return false; // No access or unlimited

        var warningThreshold = (int)(maxMessages * ConversationWarningThreshold);
        return currentCount >= warningThreshold && currentCount <= maxMessages;
    }

    /// <summary>
    /// Get the dealer's current usage as a percentage of their plan limit (0.0–1.0+).
    /// Returns 0.0 for unlimited or no-access plans.
    /// </summary>
    public static double GetUsagePercent(string planName, int currentCount)
    {
        var limits = GetLimits(planName);
        var maxMessages = limits.ChatAgentMonthlyMessages;
        if (maxMessages <= 0) return 0.0;
        return (double)currentCount / maxMessages;
    }

    /// <summary>
    /// Get the warning threshold count for a given plan.
    /// Returns the number of conversations at which to send a warning notification.
    /// Returns -1 if the plan has unlimited conversations (no warning needed).
    /// </summary>
    public static int GetConversationWarningCount(string planName)
    {
        var limits = GetLimits(planName);
        if (limits.ChatAgentMonthlyMessages <= 0) return -1; // Unlimited or no access
        return (int)(limits.ChatAgentMonthlyMessages * ConversationWarningThreshold);
    }

    /// <summary>
    /// Check the conversation usage status for a dealer.
    /// Returns the status based on their current count vs plan limit.
    /// </summary>
    public static ConversationUsageStatus GetConversationStatus(string planName, int currentCount)
    {
        var limits = GetLimits(planName);
        var maxMessages = limits.ChatAgentMonthlyMessages;

        // No access to ChatAgent
        if (maxMessages == 0) return ConversationUsageStatus.NoAccess;

        // Unlimited (should not happen for Elite after fix, but safety check)
        if (maxMessages < 0) return ConversationUsageStatus.Normal;

        if (currentCount >= maxMessages)
            return ConversationUsageStatus.LimitReached;

        var warningThreshold = (int)(maxMessages * ConversationWarningThreshold);
        if (currentCount >= warningThreshold)
            return ConversationUsageStatus.WarningThreshold;

        return ConversationUsageStatus.Normal;
    }
}

/// <summary>
/// Conversation usage status for a dealer relative to their plan limit.
/// </summary>
public enum ConversationUsageStatus
{
    /// <summary>Plan does not include ChatAgent access.</summary>
    NoAccess,

    /// <summary>Usage is below warning threshold.</summary>
    Normal,

    /// <summary>Usage has reached 80% of limit. Trigger WhatsApp + email notification.</summary>
    WarningThreshold,

    /// <summary>Usage has reached or exceeded the hard limit. ChatAgent enters basic mode.</summary>
    LimitReached
}

/// <summary>
/// Feature limits for a single plan tier.
/// </summary>
public class PlanLimits
{
    public string PlanKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public decimal PricePerMonth { get; set; }

    // Numeric limits (-1 = unlimited)
    public int MaxImagesPerVehicle { get; set; }
    public int MaxFeaturedListingsPerMonth { get; set; }
    public string SearchPriority { get; set; } = "standard";
    public int MonthlyCoins { get; set; }
    public string BadgeType { get; set; } = "none";
    public int ChatAgentMonthlyMessages { get; set; }
    public int RecosAgentMonthlyMessages { get; set; }
    public int MaxPromotionsPerMonth { get; set; }
    public int MaxSavedSearches { get; set; }
    public string AnalyticsDashboard { get; set; } = "none";
    public int MaxStaffAccounts { get; set; }

    // Boolean feature flags
    public bool HasLeadManagement { get; set; }
    public bool HasBulkUpload { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasCustomBranding { get; set; }
    public bool HasWhatsAppIntegration { get; set; }
    public bool HasVideoTour { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasExportAnalytics { get; set; }
    public bool HasDedicatedAccountManager { get; set; }
    public bool HasCompetitorAnalysis { get; set; }
}
