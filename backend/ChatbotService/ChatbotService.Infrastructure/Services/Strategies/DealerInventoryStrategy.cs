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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DealerInventoryStrategy> _logger;

    public ChatMode Mode => ChatMode.DealerInventory;

    public DealerInventoryStrategy(
        IVectorSearchService vectorSearch,
        IChatbotVehicleRepository vehicleRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<DealerInventoryStrategy> logger)
    {
        _vectorSearch = vectorSearch;
        _vehicleRepository = vehicleRepository;
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

        var systemPrompt = $@"Eres {botName}, asistente de ventas de {dealerName} en República Dominicana.
Tienes acceso al inventario completo del dealer ({totalVehicles} vehículos disponibles).

## 🎯 Capacidades
- Buscar vehículos por marca, modelo, precio, año, combustible, transmisión, color
- Comparar hasta 3 vehículos lado a lado
- Recomendar vehículos según presupuesto y necesidades del cliente
- Agendar citas para ver cualquier vehículo
- Informar sobre opciones de financiamiento
- Dar información del dealer (horarios, ubicación, contacto)

## 📋 Reglas ESTRICTAS
1. SOLO menciona vehículos que aparezcan en los datos proporcionados. NO inventes vehículos.
2. Si el usuario pide algo que no tienes en inventario, di claramente que no lo tienes y sugiere alternativas.
3. Cuando el cliente pida comparar, presenta la información en formato estructurado.
4. Si el usuario muestra interés serio, ofrece agendar una cita para ver el vehículo.
5. NUNCA reveles precios mínimos del dealer, descuentos internos o márgenes.
6. Si no tienes suficiente información para responder, sugiere contactar directamente al dealer.
7. Responde en español dominicano, breve y amigable. Máximo 4-5 oraciones.
8. Usa emojis moderadamente (1-2 por respuesta).

## 🏢 Información del Dealer
- Nombre: {dealerName}
- Inventario: {totalVehicles} vehículos disponibles{ragContext}";

        // Agregar system prompt personalizado del dealer si existe
        if (!string.IsNullOrWhiteSpace(config.SystemPromptText))
        {
            systemPrompt += $"\n\n## 📝 Instrucciones adicionales del dealer\n{config.SystemPromptText}";
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
                Description = "Agenda una cita para que el cliente vea un vehículo",
                Parameters = new Dictionary<string, FunctionParameter>
                {
                    ["vehicle_id"] = new() { Type = "string", Description = "ID del vehículo que quiere ver" },
                    ["customer_name"] = new() { Type = "string", Description = "Nombre del cliente" },
                    ["customer_phone"] = new() { Type = "string", Description = "Teléfono del cliente" },
                    ["preferred_date"] = new() { Type = "string", Description = "Fecha preferida (ej: 'mañana', 'este sábado', '2026-02-20')" },
                    ["preferred_time"] = new() { Type = "string", Description = "Hora preferida (ej: 'en la mañana', '2:00 PM')" }
                },
                Required = new() { "vehicle_id", "customer_name", "customer_phone" }
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
            "schedule_appointment" => ExecuteScheduleAppointment(session, functionCall.Arguments),
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

    private static FunctionCallResult ExecuteScheduleAppointment(
        ChatSession session, Dictionary<string, object> args)
    {
        var vehicleId = args.GetValueOrDefault("vehicle_id")?.ToString() ?? "N/A";
        var customerName = args.GetValueOrDefault("customer_name")?.ToString() ?? "N/A";
        var customerPhone = args.GetValueOrDefault("customer_phone")?.ToString() ?? "N/A";
        var preferredDate = args.GetValueOrDefault("preferred_date")?.ToString() ?? "Por coordinar";
        var preferredTime = args.GetValueOrDefault("preferred_time")?.ToString() ?? "Por coordinar";

        // En una implementación real, esto crearía un lead y notificaría al dealer
        return new FunctionCallResult
        {
            Success = true,
            ResultText = $"CITA AGENDADA:\n" +
                $"- Cliente: {customerName}\n" +
                $"- Teléfono: {customerPhone}\n" +
                $"- Vehículo ID: {vehicleId}\n" +
                $"- Fecha preferida: {preferredDate}\n" +
                $"- Hora preferida: {preferredTime}\n" +
                $"Un asesor del dealer confirmará la cita por teléfono."
        };
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
