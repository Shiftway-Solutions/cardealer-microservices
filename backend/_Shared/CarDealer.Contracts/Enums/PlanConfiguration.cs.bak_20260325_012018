namespace CarDealer.Contracts.Enums;

/// <summary>
/// Single source of truth for OKLA plan mapping (v2 — Freemium Model v3).
/// 
/// Backend enum (BillingService.Domain): Free=0, Basic=1, Professional=2, Enterprise=3, Custom=4
/// Frontend v2 display names:            Libre,   Visible,  Pro,           Elite
/// Prices (USD/month):                   $0,      $29,      $89,           $199
///
/// This class bridges the gap between internal enum names and the v2 display names
/// used by the frontend and shown to users. Always use this for any plan-related
/// display, pricing lookup, or breakdown aggregation.
/// </summary>
public static class PlanConfiguration
{
    // =========================================================================
    // V2 PLAN DISPLAY NAMES (match frontend/web-next/src/lib/plan-config.ts)
    // =========================================================================

    public const string DisplayLibre = "Libre";
    public const string DisplayVisible = "Visible";
    public const string DisplayPro = "Pro";
    public const string DisplayElite = "Elite";

    // =========================================================================
    // V2 MONTHLY PRICES (USD) — match frontend DEALER_PLAN_PRICES
    // =========================================================================

    public const decimal PriceLibre = 0m;
    public const decimal PriceVisible = 29m;
    public const decimal PricePro = 89m;
    public const decimal PriceElite = 199m;

    /// <summary>
    /// Maps an internal plan name (from SubscriptionPlan enum or DealerManagementService)
    /// to the v2 display name shown to users.
    /// Handles: enum names, old v1 names, v2 names (idempotent), and case-insensitive input.
    /// </summary>
    public static string GetDisplayName(string? internalPlan)
    {
        return (internalPlan ?? "").Trim().ToLowerInvariant() switch
        {
            // Enum names (from SubscriptionPlan.ToString())
            "free" => DisplayLibre,
            "basic" => DisplayVisible,
            "professional" => DisplayPro,
            "enterprise" => DisplayElite,

            // Old v1 names (from docs and seed data)
            "starter" => DisplayVisible,

            // V2 names (idempotent / from frontend)
            "libre" => DisplayLibre,
            "visible" => DisplayVisible,
            "pro" => DisplayPro,
            "elite" => DisplayElite,

            // Aliases
            "premium" => DisplayElite,
            "custom" => DisplayElite, // Custom plans show as Elite tier
            "none" or "" or null => DisplayLibre,

            _ => DisplayLibre
        };
    }

    /// <summary>
    /// Gets the v2 monthly price (USD) for a plan.
    /// For actual billing, always use PricePerCycle from the database.
    /// This is for MRR estimation when PricePerCycle is not available.
    /// </summary>
    public static decimal GetMonthlyPrice(string? internalPlan)
    {
        return (internalPlan ?? "").Trim().ToLowerInvariant() switch
        {
            "free" or "libre" or "none" or "" => PriceLibre,
            "basic" or "starter" or "visible" => PriceVisible,
            "professional" or "pro" => PricePro,
            "enterprise" or "elite" or "premium" or "custom" => PriceElite,
            _ => PriceLibre
        };
    }

    /// <summary>
    /// Gets the v2 frontend key (lowercase) for a plan.
    /// Used to match with frontend DealerPlan enum ('libre', 'visible', 'pro', 'elite').
    /// </summary>
    public static string GetFrontendKey(string? internalPlan)
    {
        return (internalPlan ?? "").Trim().ToLowerInvariant() switch
        {
            "free" or "libre" or "none" or "" => "libre",
            "basic" or "starter" or "visible" => "visible",
            "professional" or "pro" => "pro",
            "enterprise" or "elite" or "premium" or "custom" => "elite",
            _ => "libre"
        };
    }

    /// <summary>
    /// Returns all v2 plan display names in tier order (ascending).
    /// </summary>
    public static IReadOnlyList<string> AllDisplayNames { get; } = new[]
    {
        DisplayLibre, DisplayVisible, DisplayPro, DisplayElite
    };

    /// <summary>
    /// Returns all v2 plan prices in tier order (ascending).
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> PricesByDisplayName { get; } =
        new Dictionary<string, decimal>
        {
            [DisplayLibre] = PriceLibre,
            [DisplayVisible] = PriceVisible,
            [DisplayPro] = PricePro,
            [DisplayElite] = PriceElite,
        };
}
