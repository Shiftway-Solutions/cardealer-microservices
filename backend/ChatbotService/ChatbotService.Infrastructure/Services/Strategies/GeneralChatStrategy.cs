using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Services.Strategies;

/// <summary>
/// Estrategia para chat general sin contexto de vehículo específico.
/// FAQ, soporte, información general del marketplace.
/// </summary>
public class GeneralChatStrategy : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.General;

    public Task<string> BuildSystemPromptAsync(
        ChatSession session,
        ChatbotConfiguration config,
        string userMessage,
        CancellationToken ct = default)
    {
        var botName = config.BotName ?? "Ana";

        var prompt = $@"Eres {botName}, asistente virtual de OKLA, el marketplace de vehículos #1 en República Dominicana.

## � PERSONALIDAD
Eres amigable, clara y directa. Hablas en español dominicano natural —
profesional con calidez caribeña. Usas frases cortas (2-4 oraciones).
Usas emojis con moderación (1-2 por respuesta).
Entiendes modismos dominicanos:
- ""yipeta"" = SUV
- ""guagua"" = vehículo / van
- ""pasola"" / ""motor"" = motocicleta
- ""carro"" = automóvil
- ""pela'o"" = barato
- ""chivo"" = buena oferta
- ""un palo"" = un millón de pesos
- ""tato"" = ok / entendido
- ""vaina"" = cosa

## 🎯 TU ROL
- Ayudar a los usuarios a navegar el marketplace OKLA
- Responder preguntas generales sobre compra/venta de vehículos en RD
- Dirigir a los usuarios a las secciones apropiadas del portal
- Dar información general sobre financiamiento, documentación y proceso de compra
- Orientar sobre impuestos vehiculares y traspaso en RD

## 📋 TEMAS QUE PUEDES ORIENTAR
• **Traspaso**: Cambio de nombre ante DGII (~2% del valor), documentos necesarios (título, cédulas, contrato), 5-10 días hábiles.
• **Impuestos vehiculares**: Marbete anual (RD$1,500-24,000+), IPI en traspaso (2%), primera placa para nuevos.
• **Financiamiento**: Bancos (Popular, BHD, Reservas, Scotiabank), inicial típica 20-30%, hipoteca del título.
• **Seguro**: Básico obligatorio (Ley 146-02, desde ~RD$5,000/año), full/todo riesgo opcional. Seguro del vendedor NO se transfiere.
• **Inspección INTRANT**: Centros autorizados, revisan frenos/luces/emisiones/llantas, ~RD$1,000-3,000.
• **Vehículos importados**: Subastas USA (Copart/IAAI), título salvage vs limpio, impuestos 40-80% valor CIF, límite 5 años para importar.
• **Zona Franca**: Vehículos deben pagar nacionalización completa al salir de zona franca.
• **Garantía**: Nuevos (3-5 años fábrica), usados de dealer (posible 30-90 días), particular (sin garantía, ""como está"").
• **Negociación**: Común en RD, investigar precio de mercado primero, usar chat OKLA, contrato de promesa.
• **Dealer vs Particular**: Dealer (más formal, garantía posible, financiamiento) vs Particular (precio menor, trato directo, sin garantía).

## 📋 REGLAS
1. NO tienes acceso a vehículos específicos en este modo.
2. Si preguntan por un vehículo específico, sugiéreles buscar en okla.com.do/vehiculos o visitar el perfil del dealer.
3. Puedes responder preguntas generales sobre: financiamiento, documentación, proceso de compra, KYC, registro, impuestos vehiculares en RD.
4. Si el usuario necesita asistencia específica de un dealer, ofrece transferir a un agente.
5. NUNCA pidas nombre, cédula, teléfono ni datos bancarios al usuario.

## ⛔ ANTI-ALUCINACIÓN (OBLIGATORIO)
- NUNCA inventes precios, datos de vehículos ni estadísticas del mercado.
- NUNCA inventes URLs. Solo puedes mencionar estas:
  • okla.com.do (homepage)
  • okla.com.do/vehiculos (catálogo)
  • okla.com.do/cuenta/verificacion (KYC)
  • okla.com.do/publicar (crear listing)
  • okla.com.do/planes (planes de dealer)
- Si no sabes algo, di: ""No tengo esa información exacta, pero puedes verificarlo en okla.com.do o contactar a nuestro soporte.""
- NUNCA proceses pagos, modifiques cuentas ni accedas a datos del usuario.

## ⚖️ CUMPLIMIENTO LEGAL (República Dominicana)
- Ley 358-05 (Protección al Consumidor): Precios en RD$. Nunca digas ""precio final"" — usa ""precio de referencia"".
- Ley 172-13 (Datos Personales): NUNCA solicites cédula, tarjeta ni datos sensibles por chat.
- DGII: Los precios de vehículos NO incluyen traspaso ni impuestos.
- Ley 155-17 (Lavado de Activos): NUNCA facilites transacciones anónimas.

## 📌 URLs DE REFERENCIA AUTORIZADAS
- Buscar vehículos → okla.com.do/vehiculos
- Verificar identidad → okla.com.do/cuenta/verificacion
- Publicar vehículo → okla.com.do/publicar
- Planes de dealer → okla.com.do/planes
- Soporte → okla.com.do/soporte";

        if (!string.IsNullOrWhiteSpace(config.SystemPromptText))
        {
            prompt += $"\n\n## Instrucciones adicionales\n{config.SystemPromptText}";
        }

        return Task.FromResult(prompt);
    }

    public Task<List<FunctionDefinition>> GetAvailableFunctionsAsync(
        ChatSession session, CancellationToken ct = default)
    {
        return Task.FromResult(new List<FunctionDefinition>());
    }

    public Task<FunctionCallResult> ExecuteFunctionAsync(
        ChatSession session, FunctionCall functionCall, CancellationToken ct = default)
    {
        return Task.FromResult(new FunctionCallResult
        {
            Success = false,
            ErrorMessage = "Function calling no disponible en modo general"
        });
    }

    /// <summary>
    /// Constitutional AI: Validates that the LLM's general chat response
    /// doesn't fabricate URLs, prices, action capabilities, or vehicle data.
    /// </summary>
    public Task<GroundingValidationResult> ValidateResponseGroundingAsync(
        ChatSession session, string llmResponse, CancellationToken ct = default)
    {
        var result = new GroundingValidationResult { IsGrounded = true };
        if (string.IsNullOrWhiteSpace(llmResponse))
            return Task.FromResult(result);

        var lower = llmResponse.ToLowerInvariant();

        // 1. Detect fabricated URLs (only allowed OKLA URLs)
        var urlPattern = new System.Text.RegularExpressions.Regex(
            @"(?:https?://)?(?:www\.)?([a-zA-Z0-9\-]+(?:\.[a-zA-Z0-9\-]+)+(?:/[^\s,\)\]\""']*)?)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var allowedDomains = new[] { "okla.com.do", "dgii.gov.do", "intrant.gob.do", "proconsumidor.gob.do" };
        foreach (System.Text.RegularExpressions.Match match in urlPattern.Matches(llmResponse))
        {
            var url = match.Value.TrimEnd('.', ',', ')', ']').Replace("https://", "").Replace("http://", "").Replace("www.", "");
            if (!allowedDomains.Any(d => url.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            {
                result.IsGrounded = false;
                result.UngroundedClaims.Add($"URL fabricada: {match.Value}");
            }
        }

        // 2. Detect action claims (bot saying it can do things it can't)
        var actionPatterns = new[]
        {
            "puedo activar", "puedo desactivar", "puedo eliminar", "puedo modificar",
            "voy a procesar", "voy a contactar", "te consigo", "te garantizo",
            "crédito pre-aprobado", "ya lo activé", "ya lo eliminé",
            "estoy viendo tu cuenta", "revisando tu perfil"
        };

        foreach (var pattern in actionPatterns)
        {
            if (lower.Contains(pattern))
            {
                result.IsGrounded = false;
                result.UngroundedClaims.Add($"Reclamo de acción no autorizado: '{pattern}'");
            }
        }

        // 3. Detect hallucination phrases
        var hallucinationPatterns = new[]
        {
            "según nuestros registros", "he verificado en el sistema",
            "puedo confirmar que tu", "estoy revisando tu cuenta"
        };

        foreach (var pattern in hallucinationPatterns)
        {
            if (lower.Contains(pattern))
            {
                result.IsGrounded = false;
                result.UngroundedClaims.Add($"Frase alucinatoria: '{pattern}'");
            }
        }

        // 4. If ungrounded, provide sanitized fallback
        if (!result.IsGrounded)
        {
            result.SanitizedResponse = "No tengo información confirmada sobre eso. " +
                "Te recomiendo verificar en okla.com.do o contactar a nuestro soporte. " +
                "¿Hay algo más en lo que pueda orientarte? 😊";
        }

        return Task.FromResult(result);
    }
}
