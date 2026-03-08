using FluentAssertions;
using System.Reflection;
using SearchAgent.Application.Features.Search.Queries;
using SearchAgent.Domain.Entities;

namespace SearchAgent.Tests.Unit.Audit;

// ════════════════════════════════════════════════════════════════════════════
// SearchAgent System Prompt Audit — Structure, Dominican Tone, Legal, Output
// ════════════════════════════════════════════════════════════════════════════

public class SearchAgentPromptAuditTests
{
    private readonly string _prompt;

    public SearchAgentPromptAuditTests()
    {
        // BuildSystemPrompt is private static — invoke via reflection
        var handlerType = typeof(ProcessSearchQueryHandler);
        var method = handlerType.GetMethod("BuildSystemPrompt",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("ProcessSearchQueryHandler must have BuildSystemPrompt method");

        var config = new SearchAgentConfig(); // defaults
        _prompt = (string)method!.Invoke(null, new object[] { config })!;
    }

    // ═══ A. Structure & Role ═══

    [Fact]
    public void Prompt_IdentifiesAsSearchAgent()
    {
        _prompt.Should().Contain("SearchAgent",
            "must identify itself as SearchAgent");
        _prompt.Should().Contain("OKLA Marketplace",
            "must reference OKLA Marketplace");
    }

    [Fact]
    public void Prompt_DefinesPrimaryFunction()
    {
        _prompt.Should().Contain("TU FUNCIÓN PRINCIPAL",
            "must clearly define its primary function");
        _prompt.Should().Contain("JSON de filtros",
            "primary function is to generate filter JSON");
    }

    [Fact]
    public void Prompt_HasAbsoluteRules()
    {
        _prompt.Should().Contain("REGLA ABSOLUTA #1");
        _prompt.Should().Contain("REGLA ABSOLUTA #2");
        _prompt.Should().Contain("REGLA ABSOLUTA #3");
    }

    [Fact]
    public void Prompt_HasJSONSchema()
    {
        _prompt.Should().Contain("filtros_exactos");
        _prompt.Should().Contain("filtros_relajados");
        _prompt.Should().Contain("patrocinados_config");
        _prompt.Should().Contain("confianza");
    }

    // ═══ B. Dominican Tone ═══

    [Theory]
    [InlineData("yipeta")]
    [InlineData("guagua")]
    [InlineData("pasola")]
    [InlineData("carro")]
    [InlineData("motor")]
    public void Prompt_ContainsDominicanSlang(string term)
    {
        _prompt.Should().Contain(term,
            $"must understand Dominican term '{term}'");
    }

    [Theory]
    [InlineData("millón")]
    [InlineData("palo")]
    [InlineData("medio millón")]
    [InlineData("mil")]
    public void Prompt_ContainsDominicanPriceTerms(string term)
    {
        _prompt.Should().Contain(term,
            $"must understand Dominican price term '{term}'");
    }

    [Fact]
    public void Prompt_HasSpellingCorrections()
    {
        _prompt.Should().Contain("hundai");
        _prompt.Should().Contain("Hyundai");
        _prompt.Should().Contain("toyora");
    }

    [Fact]
    public void Prompt_HasProvinceList()
    {
        _prompt.Should().Contain("Distrito Nacional");
        _prompt.Should().Contain("Santiago");
        _prompt.Should().Contain("Santo Domingo");
        _prompt.Should().Contain("La Altagracia");
        _prompt.Should().Contain("Puerto Plata");
    }

    [Fact]
    public void Prompt_HasRegionalInterpretations()
    {
        _prompt.Should().Contain("la capital");
        _prompt.Should().Contain("el cibao");
        _prompt.Should().Contain("Punta Cana");
    }

    // ═══ C. Anti-Hallucination ═══

    [Fact]
    public void Prompt_HasAntiHallucinationSection()
    {
        _prompt.Should().Contain("ANTI-ALUCINACIÓN",
            "must have explicit anti-hallucination section");
    }

    [Fact]
    public void Prompt_ProhibitsDataInvention()
    {
        _prompt.Should().Contain("NUNCA inventes",
            "must prohibit inventing data");
    }

    [Fact]
    public void Prompt_HandlesOutOfContextQueries()
    {
        _prompt.Should().Contain("fuera de contexto",
            "must handle non-vehicle queries gracefully");
        _prompt.Should().Contain("mensaje_usuario",
            "must provide user message for off-topic queries");
    }

    // ═══ D. Legal Compliance ═══

    [Fact]
    public void Prompt_ReferencesLey35805()
    {
        _prompt.Should().Contain("358-05",
            "must reference Ley 358-05 for consumer protection");
    }

    [Fact]
    public void Prompt_HasCurrencyDefaults()
    {
        _prompt.Should().Contain("DOP",
            "default currency must be DOP");
        _prompt.Should().Contain("USD",
            "must support USD when explicitly requested");
    }

    // ═══ E. Output Format ═══

    [Fact]
    public void Prompt_RequiresOnlyJSON()
    {
        _prompt.Should().Contain("Solo JSON",
            "must require JSON-only output");
    }

    [Fact]
    public void Prompt_HasPriceInterpretationRules()
    {
        _prompt.Should().Contain("INTERPRETACIÓN DE PRECIOS",
            "must have price interpretation section");
        _prompt.Should().Contain("precio_min");
        _prompt.Should().Contain("precio_max");
    }

    [Fact]
    public void Prompt_HasVehicleTypeList()
    {
        _prompt.Should().Contain("sedan");
        _prompt.Should().Contain("suv");
        _prompt.Should().Contain("pickup");
        _prompt.Should().Contain("hatchback");
    }

    [Fact]
    public void Prompt_HasTransmissionList()
    {
        _prompt.Should().Contain("automatica");
        _prompt.Should().Contain("manual");
        _prompt.Should().Contain("cvt");
    }

    [Fact]
    public void Prompt_HasColorList()
    {
        _prompt.Should().Contain("blanco");
        _prompt.Should().Contain("negro");
        _prompt.Should().Contain("gris");
        _prompt.Should().Contain("rojo");
    }
}
