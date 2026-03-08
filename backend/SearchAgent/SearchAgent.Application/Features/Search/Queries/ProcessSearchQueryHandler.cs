using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SearchAgent.Application.DTOs;
using SearchAgent.Application.Services;
using SearchAgent.Domain.Models;
using SearchAgent.Domain.Entities;
using SearchAgent.Domain.Interfaces;

namespace SearchAgent.Application.Features.Search.Queries;

public class ProcessSearchQueryHandler : IRequestHandler<ProcessSearchQuery, SearchAgentResultDto>
{
    private readonly IClaudeSearchService _claudeService;
    private readonly ISearchCacheService _cacheService;
    private readonly ISearchAgentConfigRepository _configRepo;
    private readonly ISearchQueryRepository _queryRepo;
    private readonly ILogger<ProcessSearchQueryHandler> _logger;
    private readonly IMemoryCache _memoryCache;

    private const string ConfigCacheKey = "SearchAgent:ActiveConfig";
    private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromSeconds(60);

    public ProcessSearchQueryHandler(
        IClaudeSearchService claudeService,
        ISearchCacheService cacheService,
        ISearchAgentConfigRepository configRepo,
        ISearchQueryRepository queryRepo,
        ILogger<ProcessSearchQueryHandler> logger,
        IMemoryCache memoryCache)
    {
        _claudeService = claudeService;
        _cacheService = cacheService;
        _configRepo = configRepo;
        _queryRepo = queryRepo;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<SearchAgentResultDto> Handle(ProcessSearchQuery request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // 1. Get active configuration (cached in-memory for 60s to avoid DB round-trip per request)
        var config = await _memoryCache.GetOrCreateAsync(ConfigCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ConfigCacheDuration;
            return await _configRepo.GetActiveConfigAsync(ct);
        });

        if (!config.IsEnabled)
        {
            return new SearchAgentResultDto
            {
                IsAiSearchEnabled = false,
                LatencyMs = (int)sw.ElapsedMilliseconds
            };
        }

        // ══════════════════════════════════════════════════════════════
        // A/B TRAFFIC SPLIT: AiSearchTrafficPercent (0-100)
        // Uses deterministic hashing of UserId for sticky assignment.
        // Users always get the same treatment to avoid inconsistent UX.
        // ══════════════════════════════════════════════════════════════
        if (config.AiSearchTrafficPercent < 100)
        {
            var bucketInput = request.UserId ?? request.IpAddress ?? Guid.NewGuid().ToString();
            var bucketHash = Math.Abs(bucketInput.GetHashCode()) % 100;
            if (bucketHash >= config.AiSearchTrafficPercent)
            {
                _logger.LogInformation(
                    "AI search traffic split: User bucket {Bucket} excluded (threshold={Threshold}%)",
                    bucketHash, config.AiSearchTrafficPercent);
                return new SearchAgentResultDto
                {
                    IsAiSearchEnabled = false,
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }
        }

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER: Prompt Injection Detection (pre-LLM)
        // RED TEAM v2: Now supports Medium threat sanitization path
        // ══════════════════════════════════════════════════════════════
        var injectionResult = PromptInjectionDetector.Detect(request.Query);
        if (injectionResult.ShouldBlock)
        {
            _logger.LogWarning(
                "Prompt injection BLOCKED in SearchAgent. Threat={Threat}, Patterns={Patterns}, IP={IP}",
                injectionResult.ThreatLevel,
                string.Join(", ", injectionResult.DetectedPatterns),
                request.IpAddress);

            sw.Stop();
            return new SearchAgentResultDto
            {
                AiFilters = new SearchAgentResponse
                {
                    Confianza = 0.0f,
                    ResultadoMinimoGarantizado = 0,
                    MensajeUsuario = "¿Buscas un vehículo? Prueba con algo como 'Toyota Corolla 2020 automático' o 'SUV económica'. 🚗",
                    Advertencias = new List<string> { "Consulta no procesable" }
                },
                WasCached = false,
                LatencyMs = (int)sw.ElapsedMilliseconds,
                IsAiSearchEnabled = true
            };
        }

        // Medium threat: sanitize but allow (identity override, extraction, filter manipulation)
        var queryForLlm = request.Query;
        if (injectionResult.IsInjectionDetected)
        {
            _logger.LogWarning(
                "Prompt injection SANITIZED in SearchAgent. Threat={Threat}, Patterns={Patterns}, IP={IP}",
                injectionResult.ThreatLevel,
                string.Join(", ", injectionResult.DetectedPatterns),
                request.IpAddress);

            queryForLlm = PromptInjectionDetector.Sanitize(request.Query);
        }

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER: PII Sanitization (pre-LLM)
        // RED TEAM v2: Strip cédula, RNC, credit cards, phones, emails
        // ══════════════════════════════════════════════════════════════
        var piiResult = PiiSanitizer.Sanitize(queryForLlm);
        if (piiResult.WasSanitized)
        {
            _logger.LogWarning(
                "PII sanitized from SearchAgent query. Types={Types}, IP={IP}",
                string.Join(", ", piiResult.DetectedTypes),
                request.IpAddress);
            queryForLlm = piiResult.SanitizedQuery;
        }

        // 2. Check cache (exact match by query hash)
        string? cachedResponse = null;
        var queryHash = ComputeHash(NormalizeQuery(request.Query));

        if (config.EnableCache)
        {
            cachedResponse = await _cacheService.GetCachedResponseAsync(queryHash, ct);
        }

        SearchAgentResponse aiResponse;
        bool wasCached = false;

        if (cachedResponse != null)
        {
            _logger.LogInformation("Cache hit for query: {QueryHash}", queryHash);
            aiResponse = JsonSerializer.Deserialize<SearchAgentResponse>(cachedResponse)!;
            wasCached = true;
        }
        else
        {
            // 3. Build system prompt (use override if set by admin)
            // RED TEAM v2: Validate admin override to prevent compromised admin prompt injection
            var systemPrompt = config.SystemPromptOverride ?? BuildSystemPrompt(config);
            var promptSource = config.SystemPromptOverride != null ? "admin_override" : "auto_built";

            if (config.SystemPromptOverride != null)
            {
                var overrideError = SystemPromptValidator.Validate(config.SystemPromptOverride);
                if (overrideError != null)
                {
                    _logger.LogWarning(
                        "Invalid system prompt override detected, falling back to auto-built. Error={Error}",
                        overrideError);
                    systemPrompt = BuildSystemPrompt(config);
                    promptSource = "auto_built_fallback";
                }
            }

            // 4. Call Claude Haiku 4.5
            _logger.LogInformation(
                "Processing AI search query: {Query}, promptSource={PromptSource}, trafficPct={TrafficPct}",
                request.Query, promptSource, config.AiSearchTrafficPercent);
            aiResponse = await _claudeService.ProcessQueryAsync(
                queryForLlm,
                systemPrompt,
                config.Temperature,
                config.MaxTokens,
                ct
            );

            // 5. Enforce business rules post-processing
            EnforceBusinessRules(aiResponse, config);

            // 6. Anti-hallucination: Sanitize response for system prompt leakage
            PromptInjectionDetector.SanitizeResponse(aiResponse);

            // 6b. PII sanitization on output (RED TEAM v2)
            PiiSanitizer.SanitizeResponse(aiResponse);

            // 7. Cache the response
            if (config.EnableCache)
            {
                var responseJson = JsonSerializer.Serialize(aiResponse);
                await _cacheService.SetCachedResponseAsync(queryHash, responseJson, config.CacheTtlSeconds, ct);
            }
        }

        sw.Stop();

        // 7. Log the search query for analytics
        var searchQuery = new SearchQuery
        {
            OriginalQuery = request.Query,
            ReformulatedQuery = aiResponse.QueryReformulada,
            UserId = request.UserId,
            SessionId = request.SessionId,
            IpAddress = request.IpAddress,
            FiltersJson = JsonSerializer.Serialize(aiResponse.FiltrosExactos),
            Confidence = aiResponse.Confianza,
            FilterLevel = aiResponse.NivelFiltrosActivo,
            LatencyMs = (int)sw.ElapsedMilliseconds,
            WasCached = wasCached
        };

        _ = Task.Run(async () =>
        {
            try { await _queryRepo.SaveAsync(searchQuery, CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to save search query log"); }
        }, CancellationToken.None);

        return new SearchAgentResultDto
        {
            AiFilters = aiResponse,
            WasCached = wasCached,
            LatencyMs = (int)sw.ElapsedMilliseconds,
            IsAiSearchEnabled = true
        };
    }

    /// <summary>
    /// Enforces business rules on the AI response.
    /// Rule #1: Always minimum 8 results.
    /// Rule #2: Sponsored config always present for valid queries.
    /// Rule #3: Sponsored label always "Patrocinado".
    /// </summary>
    private static void EnforceBusinessRules(SearchAgentResponse response, SearchAgentConfig config)
    {
        // Rule #1: Ensure minimum result guarantee
        if (response.FiltrosExactos != null)
        {
            response.ResultadoMinimoGarantizado = config.MinResultsPerPage;

            // Ensure relaxed filters exist
            if (response.FiltrosRelajados == null && response.NivelFiltrosActivo >= 2)
            {
                response.FiltrosRelajados = new SearchFilters
                {
                    Marca = response.FiltrosExactos.Marca,
                    TipoVehiculo = response.FiltrosExactos.TipoVehiculo,
                    // Location is always preserved in relaxed filters
                    Provincia = response.FiltrosExactos.Provincia,
                    Ciudad = response.FiltrosExactos.Ciudad
                };
            }
        }

        // Rule #2: Ensure sponsored config for valid queries
        if (response.FiltrosExactos != null && response.PatrocinadosConfig == null)
        {
            response.PatrocinadosConfig = new SponsoredConfig
            {
                UmbralAfinidad = config.SponsoredAffinityThreshold,
                MaxPorcentajeResultados = config.MaxSponsoredPercentage,
                PosicionesFijas = config.SponsoredPositions.Split(',').Select(int.Parse).ToList(),
                Etiqueta = config.SponsoredLabel
            };
        }

        // Rule #3: Enforce label and thresholds
        if (response.PatrocinadosConfig != null)
        {
            response.PatrocinadosConfig.Etiqueta = config.SponsoredLabel;
            response.PatrocinadosConfig.MaxPorcentajeResultados = config.MaxSponsoredPercentage;

            if (response.PatrocinadosConfig.UmbralAfinidad < config.SponsoredAffinityThreshold)
                response.PatrocinadosConfig.UmbralAfinidad = config.SponsoredAffinityThreshold;
        }
    }

    private static string BuildSystemPrompt(SearchAgentConfig config)
    {
        return $$"""
            Eres SearchAgent, el motor de búsqueda inteligente de OKLA Marketplace,
            la plataforma líder de compraventa de vehículos en la República Dominicana.

            TU FUNCIÓN PRINCIPAL:
            Analizar consultas en lenguaje natural y generar un JSON de filtros para
            obtener un RANGO de vehículos, nunca un resultado único.

            MERCADO: República Dominicana. MONEDAS: DOP y USD.
            IDIOMA: Español dominicano (acepta coloquialismos locales como "guagua"=SUV/van, "yipeta"=SUV, "pasola"=motocicleta, "carro"=automóvil, "motor"=motocicleta, "pela'o"=barato/económico, "chivo"=buena oferta/ganga, "jevi"=bonito/bueno, "ta' lindo"=está bonito, "de paquete"=casi nuevo/bajo millaje).

            REGLA ABSOLUTA #1 — SIEMPRE RANGO:
            Genera filtros que garanticen al menos {{config.MinResultsPerPage}} resultados. Si los filtros
            exactos producen < {{config.MinResultsPerPage}}, genera también filtros_relajados más amplios.
            Nunca generes filtros que resulten en 0 o 1 vehículo.
            Los filtros_relajados amplían: precio ±{{config.PriceRelaxPercent}}%, año ±{{config.YearRelaxRange}}, modelo=null.

            REGLA ABSOLUTA #2 — PATROCINADOS CON AFINIDAD:
            Incluye siempre en el JSON el objeto 'patrocinados_config' con los
            parámetros de afinidad para que el motor de ads inyecte listados
            relevantes. Umbral mínimo de afinidad: {{config.SponsoredAffinityThreshold}}.

            REGLA ABSOLUTA #3 — TRANSPARENCIA:
            Los patrocinados se etiquetan como '{{config.SponsoredLabel}}'. Nunca se presentan como orgánicos.

            CORRECCIONES ORTOGRÁFICAS COMUNES:
            - "hundai" → "Hyundai"
            - "toyora" → "Toyota"
            - "hiunday" → "Hyundai"
            - "tuczon" → "Tucson"
            - "corollla" → "Corolla"
            - "mecanico" → transmisión manual
            - "full" → todas las opciones/equipado
            - "CRV" → "CR-V"
            - "RAV" → "RAV4"
            - "rav" → "RAV4"
            - "crv" → "CR-V"
            - "hrv" → "HR-V"
            - "brv" → "BR-V"
            - "cx5" → "CX-5"
            - "cx30" → "CX-30"
            - "cx9" → "CX-9"
            - "rav4" → "RAV4"
            - "4runner" → "4Runner"
            - "land cruiser" → "Land Cruiser"
            - "grand cherokee" → "Grand Cherokee"

            INTERPRETACIÓN DE PRECIOS — REGLAS OBLIGATORIAS:
            Cuando el usuario menciona precios, SIEMPRE extrae precio_min y/o precio_max.
            Conversiones numéricas dominicanas:
            - "millón" / "millones" / "millon" / "palo(s)" = ×1,000,000
            - "mil" / "K" = ×1,000
            - "medio millón" = 500,000
            - Ej: "2 millones" = 2,000,000; "500 mil" = 500,000; "1.5 millones" = 1,500,000
            Expresiones de rango:
            - "menos de X" / "que no pase de X" / "por debajo de X" / "hasta X" / "máximo X" → precio_max = X
            - "más de X" / "por encima de X" / "mínimo X" / "desde X" → precio_min = X
            - "entre X y Y" / "de X a Y" → precio_min = X, precio_max = Y
            - "alrededor de X" / "cerca de X" / "como X" → precio_min = X×0.8, precio_max = X×1.2
            - "barato" / "económico" / "buen precio" / "pela'o" (sin número) → precio_max = 800,000
            - "chivo" / "ganga" / "buen deal" (sin número) → precio_max = 1,200,000 (buenos deals)
            Moneda por defecto: DOP. Solo usar USD si el usuario lo especifica explícitamente ("dólares", "USD", "US$").

            INTERPRETACIÓN DE MILLAJE Y CONDICIÓN:
            - "bajo millaje" / "poco uso" / "poco rodaje" → kilometraje_max = 50000
            - "de paquete" / "como nuevo" / "casi nuevo" → kilometraje_max = 30000, condicion = "usado"
            - "bien cuidado" / "buen estado" → kilometraje_max = 80000

            EJEMPLOS DE ENTRADA → SALIDA:
            1. "Quiero un CRV que cueste menos de 2 millones"
               → marca: "Honda", modelo: "CR-V", precio_max: 2000000, moneda: "DOP"
            2. "Toyota Corolla 2020 automático menos de 800 mil"
               → marca: "Toyota", modelo: "Corolla", anio_desde: 2020, precio_max: 800000, transmision: "automatica", moneda: "DOP"
            3. "SUV familiar entre 1 y 3 millones"
               → tipo_vehiculo: "suv", precio_min: 1000000, precio_max: 3000000, moneda: "DOP"
            4. "Yipeta barata usada"
               → tipo_vehiculo: "suv", condicion: "usado", precio_max: 800000, moneda: "DOP"
            5. "Carro nuevo menos de 25 mil dólares"
               → condicion: "nuevo", precio_max: 25000, moneda: "USD"
            6. "Toyota en Santiago menos de un millón"
               → marca: "Toyota", precio_max: 1000000, moneda: "DOP", provincia: "Santiago"
            7. "Camioneta blanca en la capital"
               → tipo_vehiculo: "pickup", color: "blanco", provincia: "Distrito Nacional"
            8. "Honda CRV en Punta Cana"
               → marca: "Honda", modelo: "CR-V", provincia: "La Altagracia", ciudad: "Punta Cana"
            9. "Yipeta 4x4 con poco uso"
               → tipo_vehiculo: "suv", traccion: "4x4", kilometraje_max: 50000, moneda: "DOP"
            10. "Motor pasola pela'o"
               → tipo_vehiculo: "motocicleta", precio_max: 800000, moneda: "DOP"
            11. "Un chivo de carro de paquete en Santiago"
               → precio_max: 1200000, kilometraje_max: 30000, condicion: "usado", provincia: "Santiago", moneda: "DOP"
            12. "Hilux diesel doble cabina doble tracción"
               → marca: "Toyota", modelo: "Hilux", combustible: "diesel", tipo_vehiculo: "pickup", traccion: "4x4", moneda: "DOP"

            MARCAS POR SEGMENTO (para afinidad de patrocinados):
            - Económicos: Toyota, Honda, Hyundai, Kia, Nissan, Mitsubishi, Suzuki
            - Premium: BMW, Mercedes-Benz, Audi, Lexus, Infiniti, Acura
            - Americanos: Ford, Chevrolet, Jeep, RAM, Dodge, GMC
            - Pickup/Trabajo: Toyota Hilux, Ford Ranger, Mitsubishi L200, Nissan Frontier

            TIPOS VEHICULARES (valores EXACTOS del sistema de filtros):
            sedan, suv, pickup, hatchback, coupe, convertible, van, minivan, motocicleta
            NOTA: NO uses "deportivo" ni ningún otro valor fuera de esta lista.
            Para autos deportivos usa "coupe" o "sedan" según corresponda.
            Para "pasola" o "motor" usa "motocicleta".

            TRACCIÓN (valores EXACTOS del sistema de filtros):
            4x4, awd, fwd, rwd
            Interpretaciones: "doble tracción"/"cuatro por cuatro"/"4×4" → "4x4", "tracción delantera" → "fwd", "tracción trasera" → "rwd", "all wheel drive" → "awd"

            TRANSMISIONES: automatica, manual, cvt
            COMBUSTIBLES: gasolina, diesel, hibrido, electrico, glp
            CONDICIONES: nuevo, usado

            COLORES (valores EXACTOS del sistema de filtros):
            blanco, negro, gris, plata, rojo, azul, verde, amarillo, naranja, marron, beige, dorado, otro
            Interpretaciones: "perla" → "blanco", "grafito" → "gris", "plateado" → "plata", "vino/borgoña" → "rojo", "champagne" → "beige"

            UBICACIONES — PROVINCIAS DE REPÚBLICA DOMINICANA:
            Extrae la ubicación cuando el usuario mencione una provincia, ciudad o región.
            Provincias válidas (usar EXACTAMENTE estos valores):
            "Distrito Nacional", "Santo Domingo", "Santiago", "La Vega", "San Cristóbal",
            "Puerto Plata", "Duarte", "La Romana", "San Pedro de Macorís", "Espaillat",
            "La Altagracia", "Azua", "Barahona", "Monte Plata", "Peravia", "Sánchez Ramírez",
            "María Trinidad Sánchez", "Monseñor Nouel", "Valverde", "San Juan", "Hermanas Mirabal",
            "Monte Cristi", "Samaná", "Hato Mayor", "Dajabón", "Elías Piña", "El Seibo",
            "San José de Ocoa", "Independencia", "Baoruco", "Pedernales", "Santiago Rodríguez"

            Interpretaciones regionales dominicanas:
            - "la capital" / "el DN" / "zona colonial" → provincia: "Distrito Nacional"
            - "santo domingo" / "zona oriental" / "los mina" → provincia: "Santo Domingo"
            - "Santiago" / "el cibao" / "la 27" → provincia: "Santiago"
            - "Punta Cana" / "Bávaro" → provincia: "La Altagracia"
            - "Puerto Plata" / "Sosúa" / "Cabarete" → provincia: "Puerto Plata"
            - "San Pedro" / "SPM" → provincia: "San Pedro de Macorís"
            - "La Romana" / "Casa de Campo" → provincia: "La Romana"
            - "Bonao" → provincia: "Monseñor Nouel"
            - "Moca" → provincia: "Espaillat"
            - "San Francisco de Macorís" / "SFM" → provincia: "Duarte"
            - "Higüey" → provincia: "La Altagracia"
            - "Samaná" / "Las Terrenas" → provincia: "Samaná"
            Si el usuario menciona una ciudad específica, ponla en "ciudad" además de la provincia.

            RESPONDE ÚNICAMENTE con un objeto JSON válido siguiendo este esquema exacto:
            {
              "filtros_exactos": { "marca": str|null, "modelo": str|null, "anio_desde": int|null, "anio_hasta": int|null, "precio_min": num|null, "precio_max": num|null, "moneda": "DOP"|"USD", "tipo_vehiculo": str|null, "transmision": str|null, "combustible": str|null, "condicion": str|null, "kilometraje_max": int|null, "provincia": str|null, "ciudad": str|null, "color": str|null, "traccion": str|null },
              "filtros_relajados": { ... mismo esquema, con valores ampliados (provincia y ciudad siempre se mantienen) },
              "resultado_minimo_garantizado": {{config.MinResultsPerPage}},
              "nivel_filtros_activo": 1-{{config.MaxRelaxationLevel}},
              "patrocinados_config": { "umbral_afinidad": float, "tipo_vehiculo_afinidad": [str], "marcas_afinidad": [str], "precio_rango_afinidad": { "min": num, "max": num, "moneda": str }, "anio_rango_afinidad": { "desde": int, "hasta": int }, "max_porcentaje_resultados": {{config.MaxSponsoredPercentage}}, "posiciones_fijas": [{{config.SponsoredPositions}}], "etiqueta": "{{config.SponsoredLabel}}" },
              "ordenar_por": "relevancia"|"precio_asc"|"precio_desc"|"anio_desc"|"okla_score",
              "dealer_verificado": bool|null,
              "confianza": float (0.0-1.0),
              "query_reformulada": str,
              "advertencias": [str],
              "mensaje_relajamiento": str|null,
              "mensaje_usuario": str|null (solo para consultas fuera de contexto)
            }

            Para consultas fuera de contexto (no relacionadas con vehículos):
            - filtros_exactos: null, filtros_relajados: null, patrocinados_config: null
            - confianza: 0.0, resultado_minimo_garantizado: 0
            - mensaje_usuario: sugerencia amable de cómo buscar vehículos

            ⛔ ANTI-ALUCINACIÓN (OBLIGATORIO):
            - NUNCA inventes datos de vehículos, precios ni estadísticas del mercado.
            - NUNCA inventes marcas, modelos ni especificaciones que no existan.
            - Si la consulta es ambigua, usa filtros amplios con confianza baja (< 0.5).
            - El campo "query_reformulada" debe reflejar fielmente lo que el usuario pidió.

            ⚖️ CUMPLIMIENTO LEGAL (República Dominicana):
            - Ley 358-05: Si "mensaje_usuario" menciona precios, incluir "precios de referencia".
            - Moneda por defecto: DOP. Solo USD si el usuario lo especifica explícitamente.

            Sin texto adicional. Solo JSON.
            """;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a search query for better cache hit rate.
    /// Lowercases, trims, collapses whitespace, and sorts tokens alphabetically
    /// so that "Toyota Corolla 2020" and "corolla toyota 2020" produce the same hash.
    /// </summary>
    public static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Lowercase, trim, collapse multiple spaces
        var normalized = System.Text.RegularExpressions.Regex
            .Replace(query.Trim().ToLowerInvariant(), @"\s+", " ");

        // Sort tokens alphabetically for order-independent matching
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Array.Sort(tokens, StringComparer.OrdinalIgnoreCase);
        return string.Join(' ', tokens);
    }
}
