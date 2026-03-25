using CarDealer.Contracts.Enums;
using Xunit;

namespace AdminService.Tests.Shared;

/// <summary>
/// Tests for PlanConfiguration — the single source of truth for OKLA v5 plan mapping.
/// Ensures consistency between backend enum names, frontend display names, and pricing.
/// 6 dealer plans: Libre ($0), Visible ($29), Starter ($59), Pro ($99), Elite ($349), Enterprise ($599)
/// </summary>
public class PlanConfigurationTests
{
    // =========================================================================
    // GetDisplayName — maps internal names → v5 display names
    // =========================================================================

    [Theory]
    [InlineData("Free", "Libre")]
    [InlineData("free", "Libre")]
    [InlineData("FREE", "Libre")]
    [InlineData("Basic", "Visible")]
    [InlineData("basic", "Visible")]
    [InlineData("Professional", "Pro")]
    [InlineData("professional", "Pro")]
    [InlineData("Enterprise", "Enterprise")]
    [InlineData("enterprise", "Enterprise")]
    public void GetDisplayName_FromEnumNames_ReturnsV5Name(string internalName, string expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetDisplayName(internalName));
    }

    [Theory]
    [InlineData("Libre", "Libre")]
    [InlineData("Visible", "Visible")]
    [InlineData("Starter", "Starter")]
    [InlineData("Pro", "Pro")]
    [InlineData("Elite", "Elite")]
    [InlineData("libre", "Libre")]
    [InlineData("visible", "Visible")]
    [InlineData("starter", "Starter")]
    [InlineData("pro", "Pro")]
    [InlineData("elite", "Elite")]
    public void GetDisplayName_FromV5Names_IsIdempotent(string name, string expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetDisplayName(name));
    }

    [Theory]
    [InlineData("Premium", "Elite")]      // Alias maps to Elite
    [InlineData("Custom", "Enterprise")]   // Custom maps to Enterprise
    public void GetDisplayName_FromLegacyNames_MapsCorrectly(string legacyName, string expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetDisplayName(legacyName));
    }

    [Theory]
    [InlineData(null, "Libre")]
    [InlineData("", "Libre")]
    [InlineData("  ", "Libre")]
    [InlineData("none", "Libre")]
    [InlineData("unknown_plan", "Libre")]
    public void GetDisplayName_NullEmptyUnknown_ReturnsLibre(string? input, string expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetDisplayName(input));
    }

    // =========================================================================
    // GetMonthlyPrice — returns v5 USD prices
    // =========================================================================

    [Theory]
    [InlineData("Free", 0)]
    [InlineData("Basic", 29)]
    [InlineData("Professional", 99)]
    [InlineData("Enterprise", 599)]
    public void GetMonthlyPrice_FromEnumNames_ReturnsV5Price(string name, decimal expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetMonthlyPrice(name));
    }

    [Theory]
    [InlineData("Libre", 0)]
    [InlineData("Visible", 29)]
    [InlineData("Starter", 59)]
    [InlineData("Pro", 99)]
    [InlineData("Elite", 349)]
    [InlineData("Enterprise", 599)]
    public void GetMonthlyPrice_FromV5Names_ReturnsCorrectPrice(string name, decimal expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetMonthlyPrice(name));
    }

    [Theory]
    [InlineData("Premium", 349)]   // Premium→Elite=$349
    [InlineData("Custom", 599)]    // Custom→Enterprise=$599
    public void GetMonthlyPrice_FromLegacyNames_ReturnsV5Price(string name, decimal expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetMonthlyPrice(name));
    }

    [Fact]
    public void GetMonthlyPrice_NullOrEmpty_ReturnsZero()
    {
        Assert.Equal(0m, PlanConfiguration.GetMonthlyPrice(null));
        Assert.Equal(0m, PlanConfiguration.GetMonthlyPrice(""));
    }

    // =========================================================================
    // GetFrontendKey — returns lowercase keys for frontend DealerPlan enum
    // =========================================================================

    [Theory]
    [InlineData("Free", "libre")]
    [InlineData("Basic", "visible")]
    [InlineData("Professional", "pro")]
    [InlineData("Enterprise", "enterprise")]
    [InlineData("Starter", "starter")]
    [InlineData("Premium", "elite")]
    [InlineData(null, "libre")]
    [InlineData("", "libre")]
    public void GetFrontendKey_MapsCorrectly(string? input, string expected)
    {
        Assert.Equal(expected, PlanConfiguration.GetFrontendKey(input));
    }

    // =========================================================================
    // Static collections
    // =========================================================================

    [Fact]
    public void AllDisplayNames_HasSixTiersInOrder()
    {
        var names = PlanConfiguration.AllDisplayNames;
        Assert.Equal(6, names.Count);
        Assert.Equal("Libre", names[0]);
        Assert.Equal("Visible", names[1]);
        Assert.Equal("Starter", names[2]);
        Assert.Equal("Pro", names[3]);
        Assert.Equal("Elite", names[4]);
        Assert.Equal("Enterprise", names[5]);
    }

    [Fact]
    public void PricesByDisplayName_MatchesExpected()
    {
        var prices = PlanConfiguration.PricesByDisplayName;
        Assert.Equal(6, prices.Count);
        Assert.Equal(0m, prices["Libre"]);
        Assert.Equal(29m, prices["Visible"]);
        Assert.Equal(59m, prices["Starter"]);
        Assert.Equal(99m, prices["Pro"]);
        Assert.Equal(349m, prices["Elite"]);
        Assert.Equal(599m, prices["Enterprise"]);
    }

    [Fact]
    public void AllPrices_AreConsistentWithConstants()
    {
        Assert.Equal(PlanConfiguration.PriceLibre, PlanConfiguration.PricesByDisplayName["Libre"]);
        Assert.Equal(PlanConfiguration.PriceVisible, PlanConfiguration.PricesByDisplayName["Visible"]);
        Assert.Equal(PlanConfiguration.PriceStarter, PlanConfiguration.PricesByDisplayName["Starter"]);
        Assert.Equal(PlanConfiguration.PricePro, PlanConfiguration.PricesByDisplayName["Pro"]);
        Assert.Equal(PlanConfiguration.PriceElite, PlanConfiguration.PricesByDisplayName["Elite"]);
        Assert.Equal(PlanConfiguration.PriceEnterprise, PlanConfiguration.PricesByDisplayName["Enterprise"]);
    }

    // =========================================================================
    // Consistency: v5 prices match frontend plan-config.ts exactly
    // =========================================================================

    [Fact]
    public void V5Prices_MatchFrontendPlanConfig()
    {
        // These must match frontend/web-next/src/lib/plan-config.ts DEALER_PLAN_PRICES
        Assert.Equal(0m, PlanConfiguration.PriceLibre);
        Assert.Equal(29m, PlanConfiguration.PriceVisible);
        Assert.Equal(59m, PlanConfiguration.PriceStarter);
        Assert.Equal(99m, PlanConfiguration.PricePro);
        Assert.Equal(349m, PlanConfiguration.PriceElite);
        Assert.Equal(599m, PlanConfiguration.PriceEnterprise);
    }
}
