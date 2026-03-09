using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Services.Strategies;

/// <summary>
/// Estrategia para chat sobre UN vehículo específico.
/// El usuario hizo clic en el chat de una publicación concreta.
/// Contexto fijo: solo datos de ese vehículo.
/// NO necesita function calling ni RAG.
/// </summary>
public class SingleVehicleStrategy : IChatModeStrategy
{
    private readonly IChatbotVehicleRepository _vehicleRepository;
    private readonly ILogger<SingleVehicleStrategy> _logger;
    
    public ChatMode Mode => ChatMode.SingleVehicle;

    public SingleVehicleStrategy(
        IChatbotVehicleRepository vehicleRepository,
        ILogger<SingleVehicleStrategy> logger)
    {
        _vehicleRepository = vehicleRepository;
        _logger = logger;
    }

    public async Task<string> BuildSystemPromptAsync(
        ChatSession session,
        ChatbotConfiguration config,
        string userMessage,
        CancellationToken ct = default)
    {
        if (!session.VehicleId.HasValue)
        {
            _logger.LogWarning("SingleVehicleStrategy: No VehicleId in session {SessionId}", session.Id);
            return BuildFallbackPrompt(config);
        }

        // Buscar el vehículo específico
        var vehicle = await _vehicleRepository.GetByVehicleIdAsync(
            session.ChatbotConfigurationId, session.VehicleId.Value, ct);

        if (vehicle == null)
        {
            _logger.LogWarning("SingleVehicleStrategy: Vehicle {VehicleId} not found for session {SessionId}",
                session.VehicleId, session.Id);
            return BuildFallbackPrompt(config);
        }

        return BuildSingleVehiclePrompt(config, vehicle);
    }

    public Task<List<FunctionDefinition>> GetAvailableFunctionsAsync(
        ChatSession session, CancellationToken ct = default)
    {
        // Modo SingleVehicle no necesita function calling
        // Todo el contexto está en el system prompt
        return Task.FromResult(new List<FunctionDefinition>());
    }

    public Task<FunctionCallResult> ExecuteFunctionAsync(
        ChatSession session, FunctionCall functionCall, CancellationToken ct = default)
    {
        // No se ejecutan funciones en este modo
        return Task.FromResult(new FunctionCallResult
        {
            Success = false,
            ErrorMessage = "Function calling no disponible en modo Single Vehicle"
        });
    }

    public async Task<GroundingValidationResult> ValidateResponseGroundingAsync(
        ChatSession session, string llmResponse, CancellationToken ct = default)
    {
        // En modo single vehicle, validar que el LLM no invente datos del vehículo
        if (!session.VehicleId.HasValue)
            return new GroundingValidationResult { IsGrounded = true };

        var vehicle = await _vehicleRepository.GetByVehicleIdAsync(
            session.ChatbotConfigurationId, session.VehicleId.Value, ct);

        if (vehicle == null)
            return new GroundingValidationResult { IsGrounded = true };

        var result = new GroundingValidationResult { IsGrounded = true };
        var lowerResponse = llmResponse.ToLowerInvariant();

        // Verificar que no mencione otras marcas/modelos como si fueran este vehículo
        // (Nota: el LLM podría mencionar otra marca en contexto de comparación,
        //  pero no debe decir "este Toyota" si el vehículo es un Honda)
        var vehicleMake = vehicle.Make.ToLowerInvariant();
        var vehicleModel = vehicle.Model.ToLowerInvariant();

        // Frases que indicarían que el LLM está confundiendo el vehículo
        var confusionPatterns = new[]
        {
            "este vehículo es un", "este carro es un", "este auto es un",
            "estás viendo un", "estás mirando un"
        };

        foreach (var pattern in confusionPatterns)
        {
            var idx = lowerResponse.IndexOf(pattern, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var afterPattern = lowerResponse[(idx + pattern.Length)..].TrimStart();
                // Si menciona una marca diferente después del patrón, es un error
                if (!afterPattern.StartsWith(vehicleMake) && !afterPattern.StartsWith(vehicleModel))
                {
                    result.IsGrounded = false;
                    result.UngroundedClaims.Add($"Menciona vehículo incorrecto después de '{pattern}'");
                }
            }
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════════
    // PROMPT BUILDERS
    // ══════════════════════════════════════════════════════════════

    private static string BuildSingleVehiclePrompt(ChatbotConfiguration config, ChatbotVehicle vehicle)
    {
        var botName = config.BotName ?? "Ana";
        var dealerName = config.Name ?? "OKLA";
        var saleInfo = vehicle.IsOnSale && vehicle.OriginalPrice.HasValue
            ? $"\n- 🏷️ EN OFERTA: Precio original RD${vehicle.OriginalPrice:N0}, ahora RD${vehicle.Price:N0}"
            : "";
        var mileage = vehicle.Mileage.HasValue ? $"{vehicle.Mileage.Value:N0} km" : "No especificado";
        var description = !string.IsNullOrEmpty(vehicle.Description) ? $"\n- Descripción: {vehicle.Description}" : "";

        // ── PROMPT STRUCTURE FOR ANTHROPIC PROMPT CACHING ──
        // Static block (rules, JSON schema — ≥1,024 tokens, NO template variables) goes FIRST.
        // Dynamic block (bot identity + vehicle-specific data) goes AFTER <!-- CACHE_BREAK -->.
        // This allows the static block to be shared across ALL SingleVehicle sessions on
        // Anthropic's server-side cache, reducing input token costs by ≥60%.
        return $@"## 🎭 PERSONALIDAD
Hablas en español dominicano natural — profesional con calidez caribeña.
Eres conciso (máx 3-4 oraciones). Usas emojis moderadamente (1-2 por respuesta).
Entiendes modismos dominicanos:
- ""yipeta"" = SUV, ""guagua"" = vehículo/van, ""pela'o"" = barato, ""chivo"" = buena oferta
- ""carro"" = automóvil, ""un palo"" = 1 millón de pesos, ""tato"" = ok, ""vaina"" = cosa

## 📋 REGLAS ESTRICTAS
1. SOLO responde sobre el vehículo descrito en la sección VEHÍCULO EN CONSULTA. NO inventes datos.
2. Si preguntan por OTRO vehículo, di que este chat es exclusivo para el vehículo indicado y sugiere visitar el catálogo.
3. Puedes responder sobre: precio, financiamiento, ubicación, garantía, características, historial.
4. Puedes agendar cita para ver este vehículo.
5. NUNCA reveles precio mínimo, descuentos internos, o márgenes del dealer.
6. Si no tienes un dato específico (ej: historial de accidentes), di que no tienes esa información y sugiere contactar al dealer directamente.
7. NUNCA pidas cédula, tarjeta ni datos personales al usuario.

## ⛔ ANTI-ALUCINACIÓN (OBLIGATORIO)
- NUNCA inventes especificaciones, equipamiento, historial ni disponibilidad que no aparezca en los datos del vehículo.
- NUNCA inventes precios diferentes al listado. Usa siempre el precio exacto de la sección VEHÍCULO EN CONSULTA.
- Si no tienes un dato específico, di honestamente que no lo tienes.
- NUNCA inventes URLs. Solo: okla.com.do y el perfil del dealer en OKLA.

## ⚖️ CUMPLIMIENTO LEGAL (República Dominicana)
- Ley 358-05: El precio de referencia está sujeto a confirmación. Nunca digas ""precio final"". Usa siempre precio de referencia.
- Ley 172-13: NUNCA solicites cédula, tarjeta ni datos personales por chat.
- DGII: El precio NO incluye traspaso ni impuestos.
- Ley 155-17: NUNCA facilites transacciones anónimas.

## FORMATO DE RESPUESTA (JSON OBLIGATORIO)
Responde SIEMPRE con un objeto JSON válido. Nunca incluyas texto fuera del JSON.
{{
  ""response"": ""Tu respuesta al usuario en español dominicano (string)"",
  ""intent"": ""nombre_del_intent_detectado"",
  ""confidence"": 0.0,
  ""is_fallback"": false,
  ""intent_score"": 1,
  ""clasificacion"": ""curioso"",
  ""modulo_activo"": ""qa"",
  ""vehiculo_interes_id"": null,
  ""handoff_activado"": false,
  ""razon_handoff"": null,
  ""temas_consulta"": [],
  ""quick_replies"": null,
  ""suggested_action"": null,
  ""lead_signals"": {{
    ""mentionedBudget"": false,
    ""requestedTestDrive"": false,
    ""askedFinancing"": false,
    ""providedContactInfo"": false
  }},
  ""cita_propuesta"": null
}}

Valores válidos para ""clasificacion"": curioso | prospecto_frio | prospecto_tibio | comprador_inminente
Valores válidos para ""modulo_activo"": qa | cierre | handoff
Valores válidos para ""suggested_action"": show_financing | transfer_agent | schedule_appointment | null

## SCORING DE INTENCIÓN (intent_score 1-10)
Evalúa el nivel de intención de compra del usuario sobre este vehículo específico:
- 1-2: Curiosidad pasiva — pregunta general sin señales de intención
- 3-4: Prospecto frío — interés inicial por el vehículo, no da señales de compra
- 5-6: Prospecto tibio — pregunta precio, garantía o características específicas
- 7-8: Prospecto caliente — pregunta por financiamiento, entrega o proceso de compra
- 9-10: Comprador inminente — pide cita, dice querer comprar o da presupuesto concreto

## MÓDULOS DE CONVERSACIÓN
- QA (qa): intent_score 1-6 — Responde dudas, explica características, genera interés
- Cierre (cierre): intent_score 7-8 — Refuerza valor del vehículo, crea urgencia, ofrece cita
- Handoff (handoff): intent_score 9-10 — Conecta con asesor o agenda cita de prueba
<!-- CACHE_BREAK -->
## 🚗 VEHÍCULO EN CONSULTA
Eres {botName}, asistente de ventas de {dealerName} en República Dominicana.
Estás respondiendo EXCLUSIVAMENTE sobre este vehículo:

- Marca: {vehicle.Make}
- Modelo: {vehicle.Model}
- Año: {vehicle.Year}
- Versión: {vehicle.Trim ?? "N/A"}
- Precio: RD${vehicle.Price:N0}{saleInfo}
- Kilometraje: {mileage}
- Transmisión: {vehicle.Transmission ?? "N/A"}
- Combustible: {vehicle.FuelType ?? "N/A"}
- Color exterior: {vehicle.ExteriorColor ?? vehicle.Color ?? "N/A"}
- Tipo de carrocería: {vehicle.BodyType ?? "N/A"}{description}";
    }

    private static string BuildFallbackPrompt(ChatbotConfiguration config)
    {
        var botName = config.BotName ?? "Ana";
        return $@"Eres {botName}, asistente de ventas de vehículos en República Dominicana.
No pudimos cargar los datos del vehículo específico. 
Pide disculpas al usuario y sugiérele que recargue la página o visite el catálogo.
Responde en español dominicano, breve y amigable.";
    }
}
