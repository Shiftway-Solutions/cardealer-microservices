using FluentAssertions;
using SupportAgent.Application;
using Xunit;

namespace SupportAgent.Tests.Unit.Audit;

// ════════════════════════════════════════════════════════════════════════════
// SupportAgent System Prompt Audit — Structure, Dominican Tone, Legal, Output
// ════════════════════════════════════════════════════════════════════════════

public class SupportAgentPromptAuditTests
{
    private readonly string _prompt = SupportAgentPrompts.SystemPromptV2;

    // ═══ A. Structure & Role ═══

    [Fact]
    public void Prompt_IdentifiesAsSupportAgent()
    {
        _prompt.Should().Contain("SupportAgent",
            "must identify itself as SupportAgent");
        _prompt.Should().Contain("OKLA Marketplace");
    }

    [Fact]
    public void Prompt_HasTwoFunctions()
    {
        _prompt.Should().Contain("FUNCIÓN 1",
            "must define Function 1 (Soporte Técnico)");
        _prompt.Should().Contain("FUNCIÓN 2",
            "must define Function 2 (Orientación al Comprador)");
    }

    [Fact]
    public void Prompt_HasVersionMarker()
    {
        _prompt.Should().Contain("v2.0",
            "must have version marker for prompt tracking");
    }

    // ═══ B. Dominican Tone ═══

    [Fact]
    public void Prompt_SpecifiesDominicanSpanish()
    {
        _prompt.Should().Contain("español dominicano",
            "must specify Dominican Spanish");
    }

    [Fact]
    public void Prompt_HasNaturalTone()
    {
        _prompt.Should().Contain("amigable",
            "must be friendly in tone");
        _prompt.Should().Contain("claro",
            "must be clear in communication");
    }

    // ═══ C. Anti-Hallucination (5-Layer Defense) ═══

    [Fact]
    public void Prompt_HasAntiHallucinationSection()
    {
        _prompt.Should().Contain("ANTI-ALUCINACIÓN",
            "must have anti-hallucination section");
    }

    [Fact]
    public void Prompt_HasURLWhitelist()
    {
        _prompt.Should().Contain("okla.com.do",
            "must whitelist OKLA URLs");
    }

    [Fact]
    public void Prompt_ProhibitsActionsBeyondScope()
    {
        _prompt.Should().Contain("NO proceses pagos",
            "must prohibit payment processing");
        _prompt.Should().Contain("NO modifiques cuentas",
            "must prohibit account modification");
    }

    [Fact]
    public void Prompt_HasUncertaintyHandling()
    {
        // Layer 2: Explicit "I don't know"
        _prompt.Should().Contain("no estás seguro",
            "must have uncertainty handling");
    }

    // ═══ D. Legal Compliance ═══

    [Fact]
    public void Prompt_ReferencesMultipleDominicanLaws()
    {
        _prompt.Should().Contain("Ley 241",
            "must reference Ley 241 (Tránsito)");
        _prompt.Should().Contain("155-17",
            "must reference Ley 155-17 (Lavado de Activos)");
        _prompt.Should().Contain("358-05",
            "must reference Ley 358-05 (Consumidor)");
        _prompt.Should().Contain("146-02",
            "must reference Ley 146-02 (Seguros)");
    }

    [Fact]
    public void Prompt_HasLawWhitelist()
    {
        _prompt.Should().Contain("LEYES SOLO DE LA LISTA CONOCIDA",
            "must restrict to known laws only");
    }

    // ═══ E. Knowledge Base ═══

    [Fact]
    public void Prompt_HasKnowledgeBase()
    {
        _prompt.Should().Contain("KNOWLEDGE BASE",
            "must contain embedded knowledge base");
    }

    [Fact]
    public void Prompt_CoversKYC()
    {
        _prompt.Should().Contain("KYC",
            "must cover KYC process in knowledge base");
        _prompt.Should().Contain("verificación",
            "must explain verification process");
    }

    [Fact]
    public void Prompt_CoversPricingPlans()
    {
        // Must document OKLA's business model
        _prompt.Should().Contain("Gratis",
            "must mention free tier for buyers");
    }

    [Fact]
    public void Prompt_CoversRegistration()
    {
        _prompt.Should().Contain("registro",
            "must cover registration process");
    }

    // ═══ F. Consumer Protection Module ═══

    [Fact]
    public void Prompt_HasScamDetection()
    {
        _prompt.Should().Contain("estafa",
            "must help detect potential scams");
    }

    [Fact]
    public void Prompt_HasSafePurchaseProcess()
    {
        _prompt.Should().Contain("mecánico",
            "must recommend mechanical inspection");
    }

    // ═══ G. Few-Shot Examples ═══

    [Fact]
    public void Prompt_HasFewShotExamples()
    {
        _prompt.Should().Contain("EJEMPLO",
            "must include few-shot examples for consistent behavior");
    }

    // ═══ H. Output Format ═══

    [Fact]
    public void Prompt_DefinesResponseFormat()
    {
        _prompt.Should().Contain("FORMATO DE RESPUESTA",
            "must define expected response format");
    }

    [Fact]
    public void Prompt_LimitsResponseLength()
    {
        _prompt.Should().Contain("cortas",
            "must instruct for short responses");
    }

    // ═══ I. V1 alias ═══

    [Fact]
    public void V1_EqualsV2_ForRollbackSafety()
    {
        SupportAgentPrompts.SystemPromptV1.Should().Be(SupportAgentPrompts.SystemPromptV2,
            "V1 should alias to V2 for safe rollback");
    }
}
