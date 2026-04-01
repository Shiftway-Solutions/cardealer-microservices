using FluentAssertions;
using SupportAgent.Application.Services;
using Xunit;

namespace SupportAgent.Tests;

/// <summary>
/// Verifies LocalFaqMatcher returns correct responses for the 8 audit questions.
/// These tests confirm UF-081 through UF-084 pass without needing Claude API.
/// </summary>
public class LocalFaqMatcherTests
{
    [Theory]
    [InlineData("¿Cómo publico un vehículo?")]
    [InlineData("como publicar un carro")]
    [InlineData("quiero poner mi auto en venta")]
    [InlineData("cómo anunciar mi vehiculo")]
    public void TryMatch_PublicarVehiculo_ReturnsGuidance(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("okla.com.do/publicar");
        response.Should().Contain("RD$1,699");
    }

    [Theory]
    [InlineData("¿Cómo cambio mi contraseña?")]
    [InlineData("olvidé mi contraseña")]
    [InlineData("cambiar contraseña")]
    [InlineData("restablecer contrasena")]
    public void TryMatch_CambiarContrasena_ReturnsInstructions(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("okla.com.do/cuenta/seguridad");
        response.Should().Contain("recuperar-contrasena");
    }

    [Theory]
    [InlineData("¿Cuánto cuesta publicar?")]
    [InlineData("cuáles son los planes de okla")]
    [InlineData("precios para publicar")]
    [InlineData("cuanto cobran")]
    public void TryMatch_Precios_ReturnsAllPlans(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("RD$1,699");
        response.Should().Contain("RD$2,899");
        response.Should().Contain("RD$7,499");
    }

    [Theory]
    [InlineData("Me estafaron con un vehículo")]
    [InlineData("fraude con un carro")]
    [InlineData("me timaron con un auto")]
    public void TryMatch_Estafa_EscalatesAndGuidesReporting(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("okla.com.do/reportar");
        response.Should().Contain("soporte@okla.com.do");
        response.Should().Contain("proconsumidor");
    }

    [Theory]
    [InlineData("Quiero hablar con una persona")]
    [InlineData("necesito contactar un agente")]
    [InlineData("quiero hablar con alguien")]
    public void TryMatch_HablarPersona_ProvideContactInfo(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("soporte@okla.com.do");
        response.Should().Contain("WhatsApp");
        response.Should().Contain("okla.com.do/contacto");
    }

    [Theory]
    [InlineData("¿Qué es OKLA Score?")]
    [InlineData("okla score que es")]
    [InlineData("cómo funciona el okla score")]
    public void TryMatch_OklaScore_ExplainsReputation(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("OKLA Score");
        response.Should().Contain("reputación");
    }

    [Theory]
    [InlineData("¿OKLA garantiza el vehículo?")]
    [InlineData("okla garantiza el carro")]
    [InlineData("garantía de okla")]
    public void TryMatch_Garantia_HonestDisclaimer(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("intermediación");
        response.Should().Contain("KYC");
    }

    [Theory]
    [InlineData("¿Qué documentos necesito para comprar?")]
    [InlineData("que documentos piden para comprar un carro")]
    [InlineData("documentos para compra de vehiculo")]
    public void TryMatch_DocumentosCompra_ListsRDDocs(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().NotBeNull();
        response.Should().Contain("Cédula");
        response.Should().Contain("INTRANT");
        response.Should().Contain("DGII");
        response.Should().Contain("notariado");
    }

    [Theory]
    [InlineData("¿Qué tiempo tiene de garantía el motor?")]
    [InlineData("Quiero pagar con bitcoin")]
    [InlineData("")]
    public void TryMatch_UnknownQuestion_ReturnsNull(string question)
    {
        var response = LocalFaqMatcher.TryMatch(question);
        response.Should().BeNull("Unknown questions should fall through to Claude API");
    }
}
