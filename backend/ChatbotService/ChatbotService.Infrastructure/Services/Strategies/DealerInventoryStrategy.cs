using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Services.Strategies;

/// <summary>
/// Estrategia para chat con acceso al inventario COMPLETO del dealer.
/// El usuario abrió chat desde el perfil/portal del dealer.
/// Usa RAG (pgvector) para búsqueda semántica + filtros estructurados.
/// Soporta function calling para búsqueda, comparación y agendamiento.
/// </summary>
public class DealerInventoryStrategy : IChatModeStrategy
{
    private readonly IVectorSearchService _vectorSearch;
    private readonly IChatbotVehicleRepository _vehicleRepository;
    private readonly IChatbotConfigurationRepository _configRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DealerInventoryStrategy> _logger;

    public ChatMode Mode => ChatMode.DealerInventory;

    public DealerInventoryStrategy(
        IVectorSearchService vectorSearch,
        IChatbotVehicleRepository vehicleRepository,
        IChatbotConfigurationRepository configRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<DealerInventoryStrategy> logger)
    {
        _vectorSearch = vectorSearch;
        _vehicleRepository = vehicleRepository;
        _configRepository = configRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> BuildSystemPromptAsync(
        ChatSession session,
        ChatbotConfiguration config,
        string userMessage,
        CancellationToken ct = default)
    {
        // Use empty botName when config has no custom name so the API returns '' and the
        // frontend shows 'Asistente de [dealerName]' as the fallback display name.
        var botName = string.IsNullOrWhiteSpace(config.BotName) ? "Asistente" : config.BotName;
        var dealerName = config.Name ?? "OKLA";
        var dealerId = session.DealerId ?? config.DealerId ?? Guid.Empty;

        // Contar inventario total
        var totalVehicles = 0;
        List<LiveVehicleDto>? liveVehicles = null;
        try
        {
            var vehicles = await _vehicleRepository.GetByConfigurationIdAsync(config.Id, ct);
            totalVehicles = vehicles.Count(v => v.IsAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count inventory for config {ConfigId}", config.Id);
        }

        // When no local inventory for this config (real dealer using default fallback),
        // fetch live vehicles from VehiclesService scoped to the specific dealer/seller.
        if (totalVehicles == 0 && dealerId != Guid.Empty)
        {
            liveVehicles = await FetchLiveInventoryAsync(dealerId, ct);
            totalVehicles = liveVehicles?.Count ?? 0;
        }

        // RAG: buscar vehículos relevantes al mensaje del usuario
        var ragContext = "";
        try
        {
            if (liveVehicles != null && liveVehicles.Count > 0)
            {
                // Use live vehicles from VehiclesService as static context
                ragContext = BuildLiveInventoryContext(liveVehicles, userMessage);
            }
            else if (dealerId != Guid.Empty && !string.IsNullOrWhiteSpace(userMessage))
            {
                // Extraer filtros del mensaje del usuario
                var filters = ExtractFiltersFromMessage(userMessage);

                var results = await _vectorSearch.SearchAsync(
                    dealerId, userMessage, filters, topK: 5, ct: ct);

                if (results.Any())
                {
                    ragContext = "\n\n## 🔍 VEHÍCULOS RELEVANTES A LA CONSULTA\n" +
                        string.Join("\n", results.Select(r => r.ToPromptText())) +
                        "\n\nEstos son los vehículos más relevantes para la consulta actual. " +
                        "Si el usuario necesita ver más opciones, usa la función search_inventory.";

                    _logger.LogInformation("RAG: Found {Count} relevant vehicles for query in dealer {DealerId}",
                        results.Count, dealerId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAG search failed for dealer {DealerId}, falling back to static inventory", dealerId);

            // Fallback: inyectar inventario estático como antes
            ragContext = await BuildStaticInventoryContextAsync(config.Id, ct);
        }

        // ── Constitutional AI: Sanitize RAG context to prevent indirect prompt injection ──
        // Vehicle descriptions from inventory could contain malicious instructions
        // that manipulate the LLM when injected into the system prompt.
        if (!string.IsNullOrEmpty(ragContext))
        {
            ragContext = ChatbotService.Application.Services.PromptInjectionDetector.SanitizeRagContext(ragContext);
        }

        // ── PROMPT STRUCTURE FOR ANTHROPIC PROMPT CACHING ──
        // Static block (rules, personality, legal, JSON schema) goes FIRST → cached server-side.
        // Dynamic block (dealer identity, RAG context) goes AFTER <!-- CACHE_BREAK --> marker.
        // ClaudeLlmService.BuildSystemBlocks() splits on this marker:
        //   Block 1 (static, ≥1,024 tokens): cache_control=ephemeral → shared across ALL dealers
        //   Block 2 (dynamic): no cache_control → re-tokenized each call
        // Target: ≥60% input token cost reduction via Anthropic Prompt Caching.
        var systemPrompt = $@"## PERSONALIDAD
Hablas en español dominicano natural — profesional con calidez caribeña.
Siempre trata al usuario de 'tú' (tuteo). NUNCA uses 'usted' — el tono es cercano y de confianza.
Eres conciso y directo (máx 4-5 oraciones). Usas emojis moderadamente (1-2 por respuesta).
Entiendes modismos dominicanos:
- ""yipeta"" = SUV, ""guagua"" = vehículo / van, ""pasola"" / ""motor"" = motocicleta
- ""carro"" = automóvil, ""pela'o"" = barato, ""chivo"" = buena oferta
- ""un palo"" = un millón de pesos, ""tato"" = ok, ""vaina"" = cosa

## CAPACIDADES
- Buscar vehículos por marca, modelo, precio, año, combustible, transmisión, color
- Comparar hasta 3 vehículos lado a lado
- Recomendar vehículos según presupuesto y necesidades del cliente
- Agendar citas para ver cualquier vehículo
- Informar sobre opciones de financiamiento
- Dar información del dealer (horarios, ubicación, contacto)

## REGLAS ESTRICTAS
1. SOLO menciona vehículos que aparezcan en los datos proporcionados. NO inventes vehículos.
2. Si el usuario pide algo que no tienes en inventario, di claramente que no lo tienes y sugiere alternativas.
3. Cuando el cliente pida comparar, presenta la información en formato estructurado.
4. Si el usuario muestra interés serio, ofrece agendar una cita para ver el vehículo. Cuando el usuario indique una fecha preferida, llama DIRECTAMENTE a schedule_appointment sin pedir nombre ni teléfono (el sistema ya tiene los datos del usuario registrado).
5. NUNCA reveles precios mínimos del dealer, descuentos internos o márgenes.
6. Si no tienes suficiente información para responder, sugiere contactar directamente al dealer.
7. NUNCA pidas nombre ni teléfono al usuario — el sistema los obtiene automáticamente de su cuenta registrada.

## ANTI-ALUCINACIÓN (OBLIGATORIO)
- NUNCA inventes vehículos, precios, especificaciones ni disponibilidad.
- SOLO menciona vehículos que aparezcan en los datos de inventario proporcionados.
- Si no tienes un dato específico (km, color, historial), dilo honestamente.
- NUNCA inventes URLs. Solo: okla.com.do y el perfil del dealer en OKLA.
- Si el inventario está vacío, dile al usuario que contacte directamente al dealer.

## CUMPLIMIENTO LEGAL (República Dominicana)
- Ley 358-05: Precios en RD$. Nunca digas ""precio final"" — usa ""precio de referencia sujeto a confirmación"".
- Ley 172-13: NUNCA solicites cédula, tarjeta ni datos personales por chat.
- DGII: Los precios NO incluyen traspaso ni impuestos.
- Ley 155-17: NUNCA facilites transacciones anónimas.

## SCORING DE INTENCIÓN (intent_score 1-10)
Evalúa y reporta el nivel de intención de compra del usuario en cada turno:
- 1-2: Curiosidad pasiva — pregunta general sin señales de intención de compra
- 3-4: Prospecto frío — interés inicial, menciona preferencias pero no presupuesto
- 5-6: Prospecto tibio — preguntas específicas de precio, modelo o año determinado
- 7-8: Prospecto caliente — pregunta por financiamiento, tiempo de entrega o proceso de compra
- 9-10: Comprador inminente — pide agendar cita, menciona presupuesto concreto o dice querer comprar hoy

## MÓDULOS DE CONVERSACIÓN
Activa el módulo adecuado según el intent_score detectado:
- QA (qa): intent_score 1-6 — Responde preguntas, presenta opciones, genera interés
- Cierre (cierre): intent_score 7-8 — Refuerza propuesta de valor, crea urgencia, ofrece cita
- Handoff (handoff): intent_score 9-10 — Conecta con asesor humano o agenda cita de prueba manejo

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
<!-- CACHE_BREAK -->
## IDENTIDAD
Eres {botName}, asistente de ventas de {dealerName} en República Dominicana.
Tienes acceso al inventario completo del dealer ({totalVehicles} vehículos disponibles).

## Información del Dealer
- Nombre: {dealerName}
- Inventario: {totalVehicles} vehículos disponibles{ragContext}";

        // Agregar system prompt personalizado del dealer si existe
        if (!string.IsNullOrWhiteSpace(config.SystemPromptText))
        {
            systemPrompt += $"\n\n## Instrucciones adicionales del dealer\n{config.SystemPromptText}";
        }

        return systemPrompt;
    }

    public Task<List<FunctionDefinition>> GetAvailableFunctionsAsync(
        ChatSession session, CancellationToken ct = default)
    {
        var functions = new List<FunctionDefinition>
        {
            new()
            {
                Name = "search_inventory",
                Description = "Busca vehículos en el inventario del dealer según criterios del cliente",
                Parameters = new Dictionary<string, FunctionParameter>
                {
                    ["make"] = new() { Type = "string", Description = "Marca del vehículo (ej: Toyota, Honda, Hyundai)" },
                    ["model"] = new() { Type = "string", Description = "Modelo del vehículo (ej: Corolla, Civic, Tucson)" },
                    ["year_min"] = new() { Type = "number", Description = "Año mínimo" },
                    ["year_max"] = new() { Type = "number", Description = "Año máximo" },
                    ["price_min"] = new() { Type = "number", Description = "Precio mínimo en RD$" },
                    ["price_max"] = new() { Type = "number", Description = "Precio máximo en RD$" },
                    ["fuel_type"] = new() { Type = "string", Description = "Tipo de combustible",
                        Enum = new() { "Gasoline", "Diesel", "Electric", "Hybrid", "Plugin Hybrid" } },
                    ["transmission"] = new() { Type = "string", Description = "Tipo de transmisión",
                        Enum = new() { "Automatic", "Manual", "CVT" } },
                    ["body_type"] = new() { Type = "string", Description = "Tipo de carrocería",
                        Enum = new() { "Sedan", "SUV", "Pickup", "Hatchback", "Coupe", "Van", "Convertible" } },
                    ["query"] = new() { Type = "string", Description = "Búsqueda en texto libre (ej: 'yipeta económica para familia')" }
                },
                Required = new() { "query" }
            },
            new()
            {
                Name = "compare_vehicles",
                Description = "Compara 2 o 3 vehículos lado a lado mostrando sus diferencias",
                Parameters = new Dictionary<string, FunctionParameter>
                {
                    ["vehicle_ids"] = new() { Type = "array", Description = "IDs de los vehículos a comparar (2-3)" }
                },
                Required = new() { "vehicle_ids" }
            },
            new()
            {
                Name = "schedule_appointment",
                Description = "Agenda una cita para que el cliente vea un vehículo. Los datos del cliente (nombre y teléfono) se obtienen automáticamente del sistema. Todas las fechas son en hora AST (UTC-4, República Dominicana). No agendar fechas pasadas.",
                Parameters = new Dictionary<string, FunctionParameter>
                {
                    ["vehicle_id"] = new() { Type = "string", Description = "ID del vehículo que quiere ver" },
                    ["preferred_date"] = new() { Type = "string", Description = "Fecha preferida (ej: 'este viernes', 'Mié 5 de Mar')" },
                    ["preferred_time"] = new() { Type = "string", Description = "Hora preferida (ej: 'en la mañana', '2:00 PM', 'Por coordinar')" }
                },
                Required = new() { "vehicle_id", "preferred_date" }
            },
            new()
            {
                Name = "get_vehicle_details",
                Description = "Obtiene los detalles completos de un vehículo específico",
                Parameters = new Dictionary<string, FunctionParameter>
                {
                    ["vehicle_id"] = new() { Type = "string", Description = "ID del vehículo" }
                },
                Required = new() { "vehicle_id" }
            }
        };

        return Task.FromResult(functions);
    }

    public async Task<FunctionCallResult> ExecuteFunctionAsync(
        ChatSession session, FunctionCall functionCall, CancellationToken ct = default)
    {
        var dealerId = session.DealerId ?? Guid.Empty;

        return functionCall.Name switch
        {
            "search_inventory" => await ExecuteSearchInventoryAsync(session, functionCall.Arguments, ct),
            "compare_vehicles" => await ExecuteCompareVehiclesAsync(session, functionCall.Arguments, ct),
            "schedule_appointment" => await ExecuteScheduleAppointmentAsync(session, functionCall.Arguments, ct),
            "get_vehicle_details" => await ExecuteGetVehicleDetailsAsync(session, functionCall.Arguments, ct),
            _ => new FunctionCallResult
            {
                Success = false,
                ErrorMessage = $"Función '{functionCall.Name}' no reconocida"
            }
        };
    }

    public Task<GroundingValidationResult> ValidateResponseGroundingAsync(
        ChatSession session, string llmResponse, CancellationToken ct = default)
    {
        var result = new GroundingValidationResult { IsGrounded = true };

        // En modo dealer inventory, verificar que no mencione precios 
        // que no correspondan a vehículos reales del inventario
        // (Simplificado: confiar en el RAG + instrucciones del prompt)

        // Verificar que no contenga frases que indiquen invención
        var hallucinationPatterns = new[]
        {
            "podría tener", "posiblemente tiene", "creo que tiene",
            "generalmente incluye", "suele venir con", "normalmente trae"
        };

        var lowerResponse = llmResponse.ToLowerInvariant();
        foreach (var pattern in hallucinationPatterns)
        {
            if (lowerResponse.Contains(pattern))
            {
                result.UngroundedClaims.Add($"Lenguaje especulativo detectado: '{pattern}'");
            }
        }

        // Si hay claims no grounded pero no son graves, solo advertir
        if (result.UngroundedClaims.Any())
        {
            result.WarningMessage = "La respuesta contiene lenguaje especulativo. " +
                "Se recomienda basar las respuestas en datos concretos del inventario.";
        }

        return Task.FromResult(result);
    }

    // ══════════════════════════════════════════════════════════════
    // FUNCTION CALL EXECUTORS
    // ══════════════════════════════════════════════════════════════

    private async Task<FunctionCallResult> ExecuteSearchInventoryAsync(
        ChatSession session, Dictionary<string, object> args, CancellationToken ct)
    {
        var dealerId = session.DealerId ?? Guid.Empty;
        var query = args.GetValueOrDefault("query")?.ToString() ?? "";

        var filters = new VehicleSearchFilters
        {
            Make = args.GetValueOrDefault("make")?.ToString(),
            Model = args.GetValueOrDefault("model")?.ToString(),
            FuelType = args.GetValueOrDefault("fuel_type")?.ToString(),
            Transmission = args.GetValueOrDefault("transmission")?.ToString(),
            BodyType = args.GetValueOrDefault("body_type")?.ToString(),
        };

        if (args.TryGetValue("year_min", out var yearMin) && int.TryParse(yearMin?.ToString(), out var ym))
            filters.YearMin = ym;
        if (args.TryGetValue("year_max", out var yearMax) && int.TryParse(yearMax?.ToString(), out var ymx))
            filters.YearMax = ymx;
        if (args.TryGetValue("price_min", out var priceMin) && decimal.TryParse(priceMin?.ToString(), out var pm))
            filters.PriceMin = pm;
        if (args.TryGetValue("price_max", out var priceMax) && decimal.TryParse(priceMax?.ToString(), out var pmx))
            filters.PriceMax = pmx;

        try
        {
            var results = await _vectorSearch.SearchAsync(dealerId, query, filters, topK: 5, ct: ct);

            if (!results.Any())
            {
                return new FunctionCallResult
                {
                    Success = true,
                    ResultText = "No se encontraron vehículos que coincidan con esos criterios en el inventario del dealer."
                };
            }

            var resultText = "VEHÍCULOS ENCONTRADOS:\n" +
                string.Join("\n", results.Select(r => r.ToPromptText()));

            return new FunctionCallResult
            {
                Success = true,
                ResultText = resultText,
                Data = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "search_inventory failed for dealer {DealerId}", dealerId);
            return new FunctionCallResult
            {
                Success = false,
                ErrorMessage = "Error al buscar en el inventario. Intenta de nuevo."
            };
        }
    }

    private async Task<FunctionCallResult> ExecuteCompareVehiclesAsync(
        ChatSession session, Dictionary<string, object> args, CancellationToken ct)
    {
        if (!args.TryGetValue("vehicle_ids", out var idsObj))
        {
            return new FunctionCallResult { Success = false, ErrorMessage = "Se requieren IDs de vehículos para comparar." };
        }

        var ids = new List<Guid>();
        if (idsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (Guid.TryParse(item.GetString(), out var id))
                    ids.Add(id);
            }
        }

        if (ids.Count < 2 || ids.Count > 3)
        {
            return new FunctionCallResult { Success = false, ErrorMessage = "Se requieren 2 o 3 vehículos para comparar." };
        }

        var vehicles = new List<ChatbotVehicle>();
        foreach (var id in ids)
        {
            var vehicle = await _vehicleRepository.GetByVehicleIdAsync(session.ChatbotConfigurationId, id, ct);
            if (vehicle != null) vehicles.Add(vehicle);
        }

        if (vehicles.Count < 2)
        {
            return new FunctionCallResult { Success = false, ErrorMessage = "No se encontraron suficientes vehículos para comparar." };
        }

        var comparison = BuildComparisonTable(vehicles);
        return new FunctionCallResult
        {
            Success = true,
            ResultText = comparison,
            Data = vehicles
        };
    }

    private async Task<FunctionCallResult> ExecuteScheduleAppointmentAsync(
        ChatSession session, Dictionary<string, object> args, CancellationToken ct)
    {
        var vehicleId = args.GetValueOrDefault("vehicle_id")?.ToString() ?? "N/A";
        var preferredDate = args.GetValueOrDefault("preferred_date")?.ToString() ?? "Por coordinar";
        var preferredTime = args.GetValueOrDefault("preferred_time")?.ToString() ?? "Por coordinar";

        // ── Timezone context: all appointments are in AST (UTC-4, República Dominicana) ──
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var timezoneNote = $"- Zona horaria: AST (UTC-4) — Hora actual RD: {nowLocal:HH:mm, dd/MMM/yyyy}";

        // Los datos del cliente se obtienen del perfil registrado en la plataforma
        var result = new FunctionCallResult
        {
            Success = true,
            ResultText = $"SOLICITUD DE CITA REGISTRADA:\n" +
                $"- Vehículo ID: {vehicleId}\n" +
                $"- Fecha preferida: {preferredDate}\n" +
                $"- Hora preferida: {preferredTime}\n" +
                $"{timezoneNote}\n" +
                $"- Datos del cliente: obtenidos automáticamente del perfil registrado.\n" +
                $"Un asesor del dealer se pondrá en contacto para confirmar la cita."
        };

        // ── Send confirmation emails asynchronously (fire-and-forward; don't fail the main flow) ──
        await SendAppointmentEmailsAsync(session, vehicleId, preferredDate, preferredTime, ct);

        return result;
    }

    private async Task SendAppointmentEmailsAsync(
        ChatSession session,
        string vehicleId,
        string preferredDate,
        string preferredTime,
        CancellationToken ct)
    {
        try
        {
            // Fetch dealer config for name + contact email
            var config = await _configRepository.GetByIdAsync(session.ChatbotConfigurationId, ct);
            var dealerName = config?.Name ?? "el dealer";
            var dealerEmail = config?.ContactEmail;

            var buyerName = session.UserName ?? "Cliente";
            var buyerEmail = session.UserEmail;

            var httpClient = _httpClientFactory.CreateClient("NotificationService");

            // ── 1. Email to buyer ──────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(buyerEmail))
            {
                var buyerBody = BuildBuyerAppointmentEmail(
                    buyerName, dealerName, vehicleId, preferredDate, preferredTime);

                var buyerPayload = new
                {
                    to = buyerEmail,
                    subject = $"✅ Tu cita ha sido agendada — {dealerName}",
                    body = buyerBody,
                    isHtml = true,
                    metadata = new Dictionary<string, object>
                    {
                        ["source"] = "chatbot",
                        ["type"] = "appointment_buyer_confirmation"
                    }
                };

                var buyerResponse = await httpClient.PostAsJsonAsync(
                    "/api/internal/notifications/email", buyerPayload, ct);

                if (buyerResponse.IsSuccessStatusCode)
                    _logger.LogInformation(
                        "Appointment confirmation email sent to buyer {Email}", buyerEmail);
                else
                    _logger.LogWarning(
                        "Failed to send appointment email to buyer {Email}: {Status}",
                        buyerEmail, buyerResponse.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "No buyer email available for session {SessionId} — skipping buyer email",
                    session.Id);
            }

            // ── 2. Email to dealer ─────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dealerEmail))
            {
                var dealerBody = BuildDealerAppointmentEmail(
                    buyerName, buyerEmail, dealerName, vehicleId, preferredDate, preferredTime);

                var dealerPayload = new
                {
                    to = dealerEmail,
                    subject = $"🚗 Nueva solicitud de cita — {buyerName}",
                    body = dealerBody,
                    isHtml = true,
                    metadata = new Dictionary<string, object>
                    {
                        ["source"] = "chatbot",
                        ["type"] = "appointment_dealer_notification"
                    }
                };

                var dealerResponse = await httpClient.PostAsJsonAsync(
                    "/api/internal/notifications/email", dealerPayload, ct);

                if (dealerResponse.IsSuccessStatusCode)
                    _logger.LogInformation(
                        "Appointment notification email sent to dealer {Email}", dealerEmail);
                else
                    _logger.LogWarning(
                        "Failed to send appointment email to dealer {Email}: {Status}",
                        dealerEmail, dealerResponse.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "No dealer ContactEmail configured for config {ConfigId} — skipping dealer email",
                    session.ChatbotConfigurationId);
            }
        }
        catch (Exception ex)
        {
            // Never fail the appointment confirmation because of an email error
            _logger.LogError(ex,
                "Error sending appointment emails for session {SessionId}", session.Id);
        }
    }

    private static string BuildBuyerAppointmentEmail(
        string buyerName,
        string dealerName,
        string vehicleId,
        string preferredDate,
        string preferredTime)
    {
        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f4; padding:24px 0;"">
    <tr><td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
        <!-- Header -->
        <tr>
          <td style=""background-color:#1a1a2e; padding:32px 40px; text-align:center;"">
            <h1 style=""color:#ffffff; font-size:24px; margin:0;"">🚗 OKLA Marketplace</h1>
            <p style=""color:#a0a0b0; font-size:14px; margin:8px 0 0;"">República Dominicana</p>
          </td>
        </tr>
        <!-- Body -->
        <tr>
          <td style=""padding:40px;"">
            <h2 style=""color:#1a1a2e; font-size:20px; margin:0 0 16px;"">✅ ¡Tu cita ha sido registrada!</h2>
            <p style=""color:#444; font-size:16px; line-height:1.6; margin:0 0 24px;"">
              Hola <strong>{buyerName}</strong>, tu solicitud de cita con <strong>{dealerName}</strong> ha sido registrada correctamente.
              Un asesor se pondrá en contacto contigo para confirmar los detalles.
            </p>
            <!-- Appointment details box -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f8f9ff; border-left:4px solid #4f46e5; border-radius:4px; margin:0 0 24px;"">
              <tr><td style=""padding:20px 24px;"">
                <p style=""color:#6b7280; font-size:13px; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 12px;"">Detalles de la cita</p>
                <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
                  <tr>
                    <td style=""color:#6b7280; font-size:14px; width:140px;"">🏢 Dealer</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{dealerName}</td>
                  </tr>
                  <tr>
                    <td style=""color:#6b7280; font-size:14px;"">🚘 Vehículo</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{vehicleId}</td>
                  </tr>
                  <tr>
                    <td style=""color:#6b7280; font-size:14px;"">📅 Fecha</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{preferredDate}</td>
                  </tr>
                  <tr>
                    <td style=""color:#6b7280; font-size:14px;"">⏰ Hora</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{preferredTime}</td>
                  </tr>
                </table>
              </td></tr>
            </table>
            <p style=""color:#6b7280; font-size:14px; line-height:1.6; margin:0;"">
              Si necesitas modificar o cancelar tu cita, comunícate directamente con el dealer a través de la plataforma OKLA.
            </p>
          </td>
        </tr>
        <!-- Footer -->
        <tr>
          <td style=""background-color:#f4f4f4; padding:20px 40px; text-align:center; border-top:1px solid #e5e7eb;"">
            <p style=""color:#9ca3af; font-size:12px; margin:0;"">Este correo fue generado automáticamente por OKLA Marketplace. Por favor no respondas a este mensaje.</p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }

    private static string BuildDealerAppointmentEmail(
        string buyerName,
        string? buyerEmail,
        string dealerName,
        string vehicleId,
        string preferredDate,
        string preferredTime)
    {
        var buyerEmailDisplay = string.IsNullOrWhiteSpace(buyerEmail) ? "(no disponible)" : buyerEmail;
        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f4; padding:24px 0;"">
    <tr><td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
        <!-- Header -->
        <tr>
          <td style=""background-color:#1a1a2e; padding:32px 40px; text-align:center;"">
            <h1 style=""color:#ffffff; font-size:24px; margin:0;"">🚗 OKLA Marketplace</h1>
            <p style=""color:#a0a0b0; font-size:14px; margin:8px 0 0;"">Portal de Dealers</p>
          </td>
        </tr>
        <!-- Body -->
        <tr>
          <td style=""padding:40px;"">
            <h2 style=""color:#1a1a2e; font-size:20px; margin:0 0 16px;"">🔔 Nueva solicitud de cita recibida</h2>
            <p style=""color:#444; font-size:16px; line-height:1.6; margin:0 0 24px;"">
              <strong>{dealerName}</strong>, un cliente ha solicitado una cita a través del chatbot de OKLA.
            </p>
            <!-- Client details -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#fffbeb; border-left:4px solid #f59e0b; border-radius:4px; margin:0 0 16px;"">
              <tr><td style=""padding:20px 24px;"">
                <p style=""color:#6b7280; font-size:13px; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 12px;"">Datos del cliente</p>
                <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
                  <tr>
                    <td style=""color:#6b7280; font-size:14px; width:140px;"">👤 Nombre</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{buyerName}</td>
                  </tr>
                  <tr>
                    <td style=""color:#6b7280; font-size:14px;"">📧 Email</td>
                    <td style=""color:#1a1a2e; font-size:14px;"">{buyerEmailDisplay}</td>
                  </tr>
                </table>
              </td></tr>
            </table>
            <!-- Appointment details -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f8f9ff; border-left:4px solid #4f46e5; border-radius:4px; margin:0 0 24px;"">
              <tr><td style=""padding:20px 24px;"">
                <p style=""color:#6b7280; font-size:13px; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 12px;"">Detalles de la cita</p>
                <table width=""100%"" cellpadding=""4"" cellspacing=""0"">
                  <tr>
                    <td style=""color:#6b7280; font-size:14px; width:140px;"">🚘 Vehículo</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{vehicleId}</td>
                  </tr>
                  <tr>
                    <td style=""color:#6b7280; font-size:14px;"">📅 Fecha preferida</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{preferredDate}</td>
                  </tr>
                  <tr>
                    <td style=""color:#6b7280; font-size:14px;"">⏰ Hora preferida</td>
                    <td style=""color:#1a1a2e; font-size:14px; font-weight:bold;"">{preferredTime}</td>
                  </tr>
                </table>
              </td></tr>
            </table>
            <p style=""color:#6b7280; font-size:14px; line-height:1.6; margin:0;"">
              Por favor contacta al cliente para confirmar o reagendar la cita.
            </p>
          </td>
        </tr>
        <!-- Footer -->
        <tr>
          <td style=""background-color:#f4f4f4; padding:20px 40px; text-align:center; border-top:1px solid #e5e7eb;"">
            <p style=""color:#9ca3af; font-size:12px; margin:0;"">OKLA Marketplace — Sistema de notificaciones automáticas</p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }

    private async Task<FunctionCallResult> ExecuteGetVehicleDetailsAsync(
        ChatSession session, Dictionary<string, object> args, CancellationToken ct)
    {
        var vehicleIdStr = args.GetValueOrDefault("vehicle_id")?.ToString();
        if (!Guid.TryParse(vehicleIdStr, out var vehicleId))
        {
            return new FunctionCallResult { Success = false, ErrorMessage = "ID de vehículo inválido." };
        }

        var vehicle = await _vehicleRepository.GetByVehicleIdAsync(
            session.ChatbotConfigurationId, vehicleId, ct);

        if (vehicle == null)
        {
            return new FunctionCallResult { Success = false, ErrorMessage = "Vehículo no encontrado en el inventario." };
        }

        await _vehicleRepository.IncrementInquiryCountAsync(vehicle.Id, ct);

        var details = $"DETALLES DEL VEHÍCULO:\n" +
            $"- {vehicle.Year} {vehicle.Make} {vehicle.Model} {vehicle.Trim ?? ""}\n" +
            $"- Precio: RD${vehicle.Price:N0}" +
            (vehicle.IsOnSale && vehicle.OriginalPrice.HasValue ? $" (antes RD${vehicle.OriginalPrice:N0})" : "") + "\n" +
            $"- Kilometraje: {(vehicle.Mileage.HasValue ? $"{vehicle.Mileage:N0}km" : "N/A")}\n" +
            $"- Combustible: {vehicle.FuelType ?? "N/A"}\n" +
            $"- Transmisión: {vehicle.Transmission ?? "N/A"}\n" +
            $"- Color: {vehicle.ExteriorColor ?? vehicle.Color ?? "N/A"}\n" +
            $"- Tipo: {vehicle.BodyType ?? "N/A"}\n" +
            (!string.IsNullOrEmpty(vehicle.Description) ? $"- Descripción: {vehicle.Description}\n" : "");

        return new FunctionCallResult
        {
            Success = true,
            ResultText = details,
            Data = vehicle
        };
    }

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    private static VehicleSearchFilters? ExtractFiltersFromMessage(string message)
    {
        var lower = message.ToLowerInvariant();
        var filters = new VehicleSearchFilters();
        var hasFilters = false;

        // Detectar transmisión
        if (lower.Contains("automátic") || lower.Contains("automatic"))
        { filters.Transmission = "Automatic"; hasFilters = true; }
        else if (lower.Contains("manual") || lower.Contains("mecánic"))
        { filters.Transmission = "Manual"; hasFilters = true; }

        // Detectar combustible
        if (lower.Contains("diesel"))
        { filters.FuelType = "Diesel"; hasFilters = true; }
        else if (lower.Contains("eléctric") || lower.Contains("electric"))
        { filters.FuelType = "Electric"; hasFilters = true; }
        else if (lower.Contains("híbrid") || lower.Contains("hybrid"))
        { filters.FuelType = "Hybrid"; hasFilters = true; }

        // Detectar tipo de carrocería
        if (lower.Contains("suv") || lower.Contains("yipeta"))
        { filters.BodyType = "SUV"; hasFilters = true; }
        else if (lower.Contains("pickup") || lower.Contains("camioneta"))
        { filters.BodyType = "Pickup"; hasFilters = true; }
        else if (lower.Contains("sedan") || lower.Contains("sedán"))
        { filters.BodyType = "Sedan"; hasFilters = true; }

        // Detectar rango de precio (patrones comunes en RD)
        var pricePatterns = new[]
        {
            (@"(?:menos|debajo|bajo)\s+(?:de\s+)?(?:rd\$?)?\s*(\d[\d,.]+)", "max"),
            (@"(?:más|arriba|encima)\s+(?:de\s+)?(?:rd\$?)?\s*(\d[\d,.]+)", "min"),
            (@"(?:rd\$?)\s*(\d[\d,.]+)\s*(?:a|hasta|-)\s*(?:rd\$?)?\s*(\d[\d,.]+)", "range"),
            (@"(\d[\d,.]+)\s*(?:mil|k)", "thousands"),
        };

        foreach (var (pattern, type) in pricePatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lower, pattern);
            if (!match.Success) continue;

            switch (type)
            {
                case "max":
                    if (decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var maxP))
                    { filters.PriceMax = maxP < 10000 ? maxP * 1000 : maxP; hasFilters = true; }
                    break;
                case "min":
                    if (decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var minP))
                    { filters.PriceMin = minP < 10000 ? minP * 1000 : minP; hasFilters = true; }
                    break;
                case "range":
                    if (decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var rMin))
                    { filters.PriceMin = rMin < 10000 ? rMin * 1000 : rMin; hasFilters = true; }
                    if (decimal.TryParse(match.Groups[2].Value.Replace(",", ""), out var rMax))
                    { filters.PriceMax = rMax < 10000 ? rMax * 1000 : rMax; hasFilters = true; }
                    break;
                case "thousands":
                    if (decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var tP))
                    { filters.PriceMax = tP * 1000; hasFilters = true; }
                    break;
            }
        }

        return hasFilters ? filters : null;
    }

    private async Task<string> BuildStaticInventoryContextAsync(Guid configId, CancellationToken ct)
    {
        try
        {
            var vehicles = await _vehicleRepository.GetByConfigurationIdAsync(configId, ct);
            var active = vehicles.Where(v => v.IsAvailable)
                .OrderByDescending(v => v.IsFeatured)
                .ThenByDescending(v => v.UpdatedAt)
                .Take(20)
                .ToList();

            if (!active.Any()) return "";

            var lines = new List<string> { "\n\n## INVENTARIO DISPONIBLE" };
            foreach (var v in active)
            {
                var saleTag = v.IsOnSale && v.OriginalPrice.HasValue ? " 🏷️OFERTA" : "";
                var mileage = v.Mileage.HasValue ? $"{v.Mileage.Value:N0}km" : "N/A";
                lines.Add($"- {v.Make} {v.Model} {v.Year} {v.Trim ?? ""} | RD${v.Price:N0}{saleTag} | " +
                    $"{v.FuelType ?? "N/A"} | {v.Transmission ?? "N/A"} | {mileage} | ID:{v.VehicleId}");
            }
            lines.Add("\nIMPORTANTE: SOLO recomienda vehículos de esta lista.");
            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build static inventory context");
            return "";
        }
    }

    private static string BuildComparisonTable(List<ChatbotVehicle> vehicles)
    {
        var headers = "COMPARACIÓN DE VEHÍCULOS:\n\n";
        var rows = new List<string>
        {
            "| Característica | " + string.Join(" | ", vehicles.Select(v => $"{v.Year} {v.Make} {v.Model}")) + " |",
            "| --- | " + string.Join(" | ", vehicles.Select(_ => "---")) + " |",
            "| Precio | " + string.Join(" | ", vehicles.Select(v => $"RD${v.Price:N0}")) + " |",
            "| Kilometraje | " + string.Join(" | ", vehicles.Select(v => v.Mileage.HasValue ? $"{v.Mileage:N0}km" : "N/A")) + " |",
            "| Combustible | " + string.Join(" | ", vehicles.Select(v => v.FuelType ?? "N/A")) + " |",
            "| Transmisión | " + string.Join(" | ", vehicles.Select(v => v.Transmission ?? "N/A")) + " |",
            "| Color | " + string.Join(" | ", vehicles.Select(v => v.ExteriorColor ?? v.Color ?? "N/A")) + " |",
            "| Tipo | " + string.Join(" | ", vehicles.Select(v => v.BodyType ?? "N/A")) + " |",
        };

        return headers + string.Join("\n", rows);
    }

    // ══════════════════════════════════════════════════════════════════════
    // LIVE INVENTORY — Fetch real dealer vehicles from VehiclesService API
    // Used when chatbot_vehicles is empty (real dealer using default config)
    // ══════════════════════════════════════════════════════════════════════

    private async Task<List<LiveVehicleDto>?> FetchLiveInventoryAsync(Guid dealerId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("VehiclesApi");
            var url = $"seller/{dealerId}?pageSize=50&status=Active";
            var response = await httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VehiclesService returned {StatusCode} for seller {DealerId}", response.StatusCode, dealerId);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<LiveVehiclesResponse>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

            var items = data?.Items ?? data?.Data ?? [];
            _logger.LogInformation("Live inventory: fetched {Count} vehicles for dealer {DealerId}", items.Count, dealerId);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live inventory for dealer {DealerId}", dealerId);
            return null;
        }
    }

    private static string BuildLiveInventoryContext(List<LiveVehicleDto> vehicles, string userMessage)
    {
        if (vehicles.Count == 0) return "";

        var lower = userMessage.ToLowerInvariant();

        // Filter to most relevant vehicles (simple keyword match + limit)
        var relevant = vehicles
            .Where(v => v.IsActive != false)
            .OrderByDescending(v => v.IsFeatured)
            .Take(25)
            .ToList();

        // If user asked about something specific, try to surface matches first
        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            var keywordMatches = vehicles
                .Where(v => v.IsActive != false &&
                            ($"{v.Make} {v.Model} {v.Year} {v.BodyType} {v.FuelType}".ToLowerInvariant()
                             .Split(' ')
                             .Any(word => lower.Contains(word) && word.Length > 2)))
                .Take(10)
                .ToList();

            if (keywordMatches.Count > 0)
            {
                // Merge keyword matches first, then fill up to 25
                var ids = keywordMatches.Select(v => v.Id).ToHashSet();
                var rest = relevant.Where(v => !ids.Contains(v.Id)).Take(25 - keywordMatches.Count);
                relevant = keywordMatches.Concat(rest).ToList();
            }
        }

        var lines = new List<string> { "\n\n## 🚗 INVENTARIO DEL DEALER (Vehículos publicados en OKLA)" };
        foreach (var v in relevant)
        {
            var price = v.Price > 0 ? $"RD${v.Price:N0}" : "Consultar";
            var mileage = v.Mileage > 0 ? $"{v.Mileage:N0}km" : "N/A";
            var sale = v.IsOnSale && v.OriginalPrice > 0 ? $" 🏷️OFERTA (antes RD${v.OriginalPrice:N0})" : "";
            lines.Add($"- {v.Year} {v.Make} {v.Model} {v.Trim ?? ""} | {price}{sale} | {v.FuelType ?? "N/A"} | {v.Transmission ?? "N/A"} | {mileage} | {v.ExteriorColor ?? "N/A"} | ID:{v.Id}");
        }
        lines.Add($"\nTotal disponibles en OKLA: {vehicles.Count(v => v.IsActive != false)}");
        lines.Add("IMPORTANTE: SOLO recomienda vehículos de esta lista. Estos son los vehículos REALES publicados por este dealer en la plataforma OKLA.");
        return string.Join("\n", lines);
    }
}

// DTO for live inventory from VehiclesService API
internal sealed class LiveVehiclesResponse
{
    public List<LiveVehicleDto> Items { get; set; } = [];
    public List<LiveVehicleDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
}

internal sealed class LiveVehicleDto
{
    public Guid Id { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public string? Trim { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool IsOnSale { get; set; }
    public int Mileage { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? BodyType { get; set; }
    public string? ExteriorColor { get; set; }
    public string? Description { get; set; }
    public bool IsFeatured { get; set; }
    public bool? IsActive { get; set; }
    public string? Status { get; set; }
}
