using SearchAgent.Application.Services;
using SearchAgent.Domain.Models;
using Xunit;
using FluentAssertions;

namespace SearchAgent.Tests.Security;

/// <summary>
/// RED TEAM test suite for SearchAgent PII Sanitizer.
/// Tests Dominican Republic PII detection: cédula, RNC, credit cards, phones, emails.
/// Tests both input (pre-LLM) and output (post-LLM) sanitization.
/// </summary>
public class PiiSanitizerTests
{
    // ═══════════════════════════════════════════════════════════════════
    // 1. SAFE INPUTS — No PII detected
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Toyota Corolla 2020")]
    [InlineData("busco carro económico")]
    [InlineData("SUV precio menor a 500000")]
    [InlineData("")]
    public void Sanitize_NoPii_ReturnsSameQuery(string query)
    {
        var result = PiiSanitizer.Sanitize(query);

        result.WasSanitized.Should().BeFalse();
        result.SanitizedQuery.Should().Be(query);
        result.DetectedTypes.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. DOMINICAN CÉDULA DETECTION
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("mi cédula es 001-1234567-8 busco Toyota", "cedula")]
    [InlineData("busco carro 40212345678 en Santiago", "cedula")]
    public void Sanitize_Cedula_StripsFromQuery(string query, string expectedType)
    {
        var result = PiiSanitizer.Sanitize(query);

        result.WasSanitized.Should().BeTrue();
        result.DetectedTypes.Should().Contain(expectedType);
        result.SanitizedQuery.Should().NotContain("001-1234567-8");
        result.SanitizedQuery.Should().NotContain("40212345678");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. CREDIT CARD DETECTION
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("pago con 4111 1111 1111 1111 busco Honda")]
    [InlineData("tarjeta 4532015112830366")]
    public void Sanitize_CreditCard_StripsFromQuery(string query)
    {
        var result = PiiSanitizer.Sanitize(query);

        result.WasSanitized.Should().BeTrue();
        result.DetectedTypes.Should().Contain("credit_card");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. DOMINICAN PHONE DETECTION
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("llama al 809-555-1234 para más info")]
    [InlineData("contacto 829-123-4567")]
    [InlineData("+1-849-555-0001 dealer en Santiago")]
    public void Sanitize_Phone_StripsFromQuery(string query)
    {
        var result = PiiSanitizer.Sanitize(query);

        result.WasSanitized.Should().BeTrue();
        result.DetectedTypes.Should().Contain("phone");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. EMAIL DETECTION
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("escribir a juan@gmail.com busco Camry")]
    [InlineData("contacto: dealer@okla.do")]
    public void Sanitize_Email_StripsFromQuery(string query)
    {
        var result = PiiSanitizer.Sanitize(query);

        result.WasSanitized.Should().BeTrue();
        result.DetectedTypes.Should().Contain("email");
        result.SanitizedQuery.Should().NotContain("@");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. MULTIPLE PII TYPES
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Sanitize_MultiplePiiTypes_StripsAll()
    {
        var query = "mi cédula 001-1234567-8 y teléfono 809-555-1234 busco Toyota";
        var result = PiiSanitizer.Sanitize(query);

        result.WasSanitized.Should().BeTrue();
        result.DetectedTypes.Should().Contain("cedula");
        result.DetectedTypes.Should().Contain("phone");
        result.SanitizedQuery.Should().Contain("busco Toyota");
        result.SanitizedQuery.Should().NotContain("001-1234567-8");
        result.SanitizedQuery.Should().NotContain("809-555-1234");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. RESPONSE SANITIZATION (Post-LLM)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SanitizeResponse_PiiInMensajeUsuario_Sanitizes()
    {
        var response = new SearchAgentResponse
        {
            MensajeUsuario = "El dueño es Juan, cédula 001-1234567-8, teléfono 809-555-1234",
            Advertencias = new List<string>()
        };

        PiiSanitizer.SanitizeResponse(response);

        response.MensajeUsuario.Should().Contain("[DATO_PROTEGIDO]");
        response.MensajeUsuario.Should().NotContain("001-1234567-8");
        response.MensajeUsuario.Should().NotContain("809-555-1234");
    }

    [Fact]
    public void SanitizeResponse_PiiInAdvertencias_Sanitizes()
    {
        var response = new SearchAgentResponse
        {
            MensajeUsuario = "Búsqueda completada",
            Advertencias = new List<string>
            {
                "Contacta al 809-555-1234 para negociar",
                "Todo en orden"
            }
        };

        PiiSanitizer.SanitizeResponse(response);

        response.Advertencias[0].Should().Contain("[DATO_PROTEGIDO]");
        response.Advertencias[1].Should().Be("Todo en orden");
    }

    [Fact]
    public void SanitizeResponse_NoPii_NoChange()
    {
        var response = new SearchAgentResponse
        {
            MensajeUsuario = "Encontré 5 Toyota Corolla para ti",
            Advertencias = new List<string> { "Filtrando por automático" }
        };

        PiiSanitizer.SanitizeResponse(response);

        response.MensajeUsuario.Should().Be("Encontré 5 Toyota Corolla para ti");
    }
}
