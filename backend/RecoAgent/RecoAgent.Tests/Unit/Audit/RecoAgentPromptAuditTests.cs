using FluentAssertions;
using System.Reflection;
using RecoAgent.Application.Features.Recommend.Queries;
using RecoAgent.Domain.Entities;

namespace RecoAgent.Tests.Unit.Audit;

// ════════════════════════════════════════════════════════════════════════════
// RecoAgent System Prompt Audit — Structure, Dominican Tone, Legal, Output
// ════════════════════════════════════════════════════════════════════════════

public class RecoAgentPromptAuditTests
{
    private readonly string _prompt;

    public RecoAgentPromptAuditTests()
    {
        var handlerType = typeof(GenerateRecommendationsQueryHandler);
        var method = handlerType.GetMethod("BuildSystemPrompt",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("GenerateRecommendationsQueryHandler must have BuildSystemPrompt method");

        var config = new RecoAgentConfig(); // defaults
        _prompt = (string)method!.Invoke(null, new object[] { config })!;
    }

    // ═══ A. Structure & Role ═══

    [Fact]
    public void Prompt_IdentifiesAsRecoAgent()
    {
        _prompt.Should().Contain("RecoAgent",
            "must identify itself as RecoAgent");
        _prompt.Should().Contain("OKLA Marketplace");
    }

    [Fact]
    public void Prompt_DefinesPrimaryFunction()
    {
        _prompt.Should().Contain("TU FUNCIÓN PRINCIPAL");
        _prompt.Should().Contain("recomendaciones vehiculares personalizadas");
    }

    [Fact]
    public void Prompt_Has5AbsoluteRules()
    {
        _prompt.Should().Contain("REGLA ABSOLUTA #1");
        _prompt.Should().Contain("REGLA ABSOLUTA #2");
        _prompt.Should().Contain("REGLA ABSOLUTA #3");
        _prompt.Should().Contain("REGLA ABSOLUTA #4");
        _prompt.Should().Contain("REGLA ABSOLUTA #5");
    }

    [Fact]
    public void Prompt_DefinesRecommendationTypes()
    {
        _prompt.Should().Contain("perfil");
        _prompt.Should().Contain("similar");
        _prompt.Should().Contain("descubrimiento");
        _prompt.Should().Contain("popular");
        _prompt.Should().Contain("patrocinado");
    }

    [Fact]
    public void Prompt_DefinesPurchaseStages()
    {
        _prompt.Should().Contain("explorador");
        _prompt.Should().Contain("comparador");
        _prompt.Should().Contain("comprador_inminente");
        _prompt.Should().Contain("post_compra");
    }

    [Fact]
    public void Prompt_DefinesColdStartLevels()
    {
        _prompt.Should().Contain("Nivel 0");
        _prompt.Should().Contain("Nivel 1");
        _prompt.Should().Contain("Nivel 2");
        _prompt.Should().Contain("Nivel 3");
    }

    // ═══ B. Dominican Tone ═══

    [Fact]
    public void Prompt_SpecifiesDominicanSpanish()
    {
        _prompt.Should().Contain("español dominicano",
            "must specify Dominican Spanish for explanations");
    }

    [Fact]
    public void Prompt_HasGoodDominicanExamples()
    {
        // Must have examples of razon_recomendacion in Dominican Spanish
        _prompt.Should().Contain("yipeta",
            "must include 'yipeta' in example phrases");
        _prompt.Should().Contain("dale un ojo",
            "must include Dominican expressions like 'dale un ojo'");
    }

    [Fact]
    public void Prompt_HasBadExamples()
    {
        _prompt.Should().Contain("Ejemplos MALOS",
            "must include anti-examples for guidance");
        _prompt.Should().Contain("demasiado genérico",
            "must explain WHY bad examples are bad");
    }

    [Fact]
    public void Prompt_MentionsGeographicProximity()
    {
        _prompt.Should().Contain("Santiago",
            "must reference Dominican cities for proximity");
        _prompt.Should().Contain("la capital");
    }

    // ═══ C. Anti-Hallucination ═══

    [Fact]
    public void Prompt_HasAntiHallucinationSection()
    {
        _prompt.Should().Contain("ANTI-ALUCINACIÓN",
            "must have explicit anti-hallucination section");
    }

    [Fact]
    public void Prompt_Prohibits5SpecificHallucinations()
    {
        _prompt.Should().Contain("NUNCA inventes vehiculo_ids");
        _prompt.Should().Contain("NO repitas el mismo vehiculo_id");
    }

    [Fact]
    public void Prompt_HandlesEmptyCandidates()
    {
        _prompt.Should().Contain("candidatos",
            "must handle empty candidate list");
        _prompt.Should().Contain("cold_start_nivel: 0",
            "empty candidates must result in cold_start_nivel 0");
    }

    // ═══ D. Legal Compliance ═══

    [Fact]
    public void Prompt_ReferencesLey35805()
    {
        _prompt.Should().Contain("358-05",
            "must reference Ley 358-05 (consumer protection)");
    }

    [Fact]
    public void Prompt_ReferencesLey17213()
    {
        _prompt.Should().Contain("172-13",
            "must reference Ley 172-13 (data protection)");
    }

    [Fact]
    public void Prompt_ReferencesLey15517()
    {
        _prompt.Should().Contain("155-17",
            "must reference Ley 155-17 (anti-money laundering)");
    }

    [Fact]
    public void Prompt_ContainsPriceReferenceRule()
    {
        _prompt.Should().Contain("precio de referencia",
            "must instruct to use 'precio de referencia' not 'precio final'");
    }

    // ═══ E. Output Format ═══

    [Fact]
    public void Prompt_RequiresOnlyJSON()
    {
        _prompt.Should().Contain("Solo JSON",
            "must require JSON-only output");
    }

    [Fact]
    public void Prompt_HasFullJSONSchema()
    {
        _prompt.Should().Contain("recomendaciones");
        _prompt.Should().Contain("vehiculo_id");
        _prompt.Should().Contain("razon_recomendacion");
        _prompt.Should().Contain("score_afinidad_perfil");
        _prompt.Should().Contain("diversificacion_aplicada");
        _prompt.Should().Contain("confianza_recomendaciones");
    }

    [Fact]
    public void Prompt_HasSponsoredConfig()
    {
        _prompt.Should().Contain("patrocinados_config");
        _prompt.Should().Contain("posiciones_patrocinados");
        _prompt.Should().Contain("threshold_score");
    }

    // ═══ F. Diversification Rules ═══

    [Fact]
    public void Prompt_HasDiversificationRules()
    {
        _prompt.Should().Contain("DIVERSIFICACIÓN OBLIGATORIA",
            "must enforce brand diversification");
    }

    [Fact]
    public void Prompt_HasFeedbackHandling()
    {
        _prompt.Should().Contain("feedback_reco",
            "must handle user feedback signals");
        _prompt.Should().Contain("thumbs_down");
        _prompt.Should().Contain("thumbs_up");
    }
}
