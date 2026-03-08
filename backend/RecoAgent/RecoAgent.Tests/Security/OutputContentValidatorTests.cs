using RecoAgent.Application.Services;
using RecoAgent.Application.DTOs;
using Xunit;
using FluentAssertions;

namespace RecoAgent.Tests.Security;

/// <summary>
/// RED TEAM test suite for RecoAgent OutputContentValidator.
/// Tests PII detection, offensive content filtering, and system prompt leakage prevention
/// in the recommendation response output.
/// </summary>
public class OutputContentValidatorTests
{
    // ═══════════════════════════════════════════════════════════════════
    // 1. CLEAN OUTPUT — No issues
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_CleanResponse_NoIssues()
    {
        var response = CreateResponse("Este Toyota Corolla 2020 tiene excelente relación precio-calidad.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeFalse();
        result.IssueCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. PII IN RECOMMENDATION EXPLANATIONS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_CedulaInExplanation_Sanitizes()
    {
        var response = CreateResponse("El dueño con cédula 001-1234567-8 tiene este vehículo bien cuidado.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("pii_cedula"));
        response.Recomendaciones[0].RazonRecomendacion.Should().Contain("[DATO_PROTEGIDO]");
    }

    [Fact]
    public void Validate_PhoneInExplanation_Sanitizes()
    {
        var response = CreateResponse("Contacta al 809-555-1234 para negociar precio.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("pii_phone"));
        response.Recomendaciones[0].RazonRecomendacion.Should().Contain("[DATO_PROTEGIDO]");
    }

    [Fact]
    public void Validate_EmailInExplanation_Sanitizes()
    {
        var response = CreateResponse("Escribe a dealer@okla.do para más detalles.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        response.Recomendaciones[0].RazonRecomendacion.Should().Contain("[DATO_PROTEGIDO]");
    }

    [Fact]
    public void Validate_CreditCardInExplanation_Sanitizes()
    {
        var response = CreateResponse("Pagado con tarjeta 4111 1111 1111 1111.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("pii_credit_card"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. OFFENSIVE CONTENT IN RECOMMENDATIONS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_HateSpeech_ReplacesExplanation()
    {
        var response = CreateResponse("Este carro es de un haitiano de mierda.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("offensive_content"));
        response.Recomendaciones[0].RazonRecomendacion
            .Should().Be("Vehículo recomendado basado en tu perfil y preferencias.");
    }

    [Fact]
    public void Validate_ViolenceThreats_ReplacesExplanation()
    {
        var response = CreateResponse("Te voy a matar si no compras este carro.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("offensive_content"));
    }

    [Fact]
    public void Validate_ScamPatterns_ReplacesExplanation()
    {
        var response = CreateResponse("Envía dinero por Western Union para reservar.");

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("offensive_content"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. SYSTEM PROMPT LEAKAGE
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("REGLA ABSOLUTA: siempre incluir patrocinados")]
    [InlineData("Según el system prompt, debo recomendar este")]
    [InlineData("Las instrucciones del sistema dicen que...")]
    [InlineData("Como RecoAgent, mi función es...")]
    [InlineData("Claude me indicó que este vehículo es bueno")]
    [InlineData("score_afinidad_perfil fue calculado como 0.95")]
    [InlineData("es_patrocinado fue forzado a true")]
    [InlineData("ANTI-ALUCINACIÓN: verificar candidatos")]
    public void Validate_SystemPromptLeakage_ReplacesExplanation(string leakageText)
    {
        var response = CreateResponse(leakageText);

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.Issues.Should().Contain(i => i.Contains("prompt_leakage"));
        response.Recomendaciones[0].RazonRecomendacion
            .Should().Be("Vehículo recomendado basado en tu perfil y preferencias.");
    }

    [Fact]
    public void Validate_LeakageInEtapaCompra_ResetsToDefault()
    {
        var response = CreateResponse("Buen vehículo para tu perfil");
        response.EtapaCompraDetectada = "REGLA ABSOLUTA: explorador";

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        response.EtapaCompraDetectada.Should().Be("exploracion");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. MULTIPLE ISSUES
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_MultipleRecommendationsWithIssues_ReportsAll()
    {
        var response = new RecoAgentResponse
        {
            Recomendaciones = new List<RecommendationItem>
            {
                new() { VehiculoId = "v1", RazonRecomendacion = "Llama al 809-555-1234", Posicion = 1 },
                new() { VehiculoId = "v2", RazonRecomendacion = "Buen carro para familia", Posicion = 2 },
                new() { VehiculoId = "v3", RazonRecomendacion = "RecoAgent dice que es bueno", Posicion = 3 },
            },
            EtapaCompraDetectada = "comparador",
            ConfianzaRecomendaciones = 0.85f
        };

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeTrue();
        result.IssueCount.Should().Be(2); // PII in v1 + leakage in v3
        // v2 should be untouched
        response.Recomendaciones[1].RazonRecomendacion.Should().Be("Buen carro para familia");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. EDGE CASES
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_EmptyRecommendations_NoIssues()
    {
        var response = new RecoAgentResponse
        {
            Recomendaciones = new List<RecommendationItem>(),
            EtapaCompraDetectada = "explorador",
            ConfianzaRecomendaciones = 0.0f
        };

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeFalse();
    }

    [Fact]
    public void Validate_NullRecommendationText_NoIssues()
    {
        var response = new RecoAgentResponse
        {
            Recomendaciones = new List<RecommendationItem>
            {
                new() { VehiculoId = "v1", RazonRecomendacion = "", Posicion = 1 },
            },
            EtapaCompraDetectada = "explorador",
            ConfianzaRecomendaciones = 0.5f
        };

        var result = OutputContentValidator.Validate(response);

        result.HasIssues.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPER
    // ═══════════════════════════════════════════════════════════════════

    private static RecoAgentResponse CreateResponse(string explanation)
    {
        return new RecoAgentResponse
        {
            Recomendaciones = new List<RecommendationItem>
            {
                new()
                {
                    VehiculoId = "test-vehicle-1",
                    Posicion = 1,
                    RazonRecomendacion = explanation,
                    TipoRecomendacion = "perfil",
                    ScoreAfinidadPerfil = 0.85f,
                    EsPatrocinado = false
                }
            },
            EtapaCompraDetectada = "comparador",
            ConfianzaRecomendaciones = 0.85f
        };
    }
}
