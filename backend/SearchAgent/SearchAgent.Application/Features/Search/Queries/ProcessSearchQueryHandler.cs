using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using SearchAgent.Application.DTOs;
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

    public ProcessSearchQueryHandler(
        IClaudeSearchService claudeService,
        ISearchCacheService cacheService,
        ISearchAgentConfigRepository configRepo,
        ISearchQueryRepository queryRepo,
        ILogger<ProcessSearchQueryHandler> logger)
    {
        _claudeService = claudeService;
        _cacheService = cacheService;
        _configRepo = configRepo;
        _queryRepo = queryRepo;
        _logger = logger;
    }

    public async Task<SearchAgentResultDto> Handle(ProcessSearchQuery request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // 1. Get active configuration
        var config = await _configRepo.GetActiveConfigAsync(ct);

        if (!config.IsEnabled)
        {
            return new SearchAgentResultDto
            {
                IsAiSearchEnabled = false,
                LatencyMs = (int)sw.ElapsedMilliseconds
            };
        }

        // 2. Check cache (exact match by query hash)
        string? cachedResponse = null;
        var queryHash = ComputeHash(request.Query.Trim().ToLowerInvariant());

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
            var systemPrompt = config.SystemPromptOverride ?? BuildSystemPrompt(config);

            // 4. Call Claude Haiku 4.5
            _logger.LogInformation("Processing AI search query: {Query}", request.Query);
            aiResponse = await _claudeService.ProcessQueryAsync(
                request.Query,
                systemPrompt,
                config.Temperature,
                config.MaxTokens,
                ct
            );

            // 5. Enforce business rules post-processing
            EnforceBusinessRules(aiResponse, config);

            // 6. Cache the response
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
                    TipoVehiculo = response.FiltrosExactos.TipoVehiculo
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
        return $"""
            Eres SearchAgent, el motor de búsqueda inteligente de OKLA Marketplace,
            la plataforma líder de compraventa de vehículos en la República Dominicana.

            TU FUNCIÓN PRINCIPAL:
            Analizar consultas en lenguaje natural y generar un JSON de filtros para
            obtener un RANGO de vehículos, nunca un resultado único.

            MERCADO: República Dominicana. MONEDAS: DOP y USD.
            IDIOMA: Español dominicano (acepta coloquialismos locales como "guagua"=SUV/van, "yipeta"=SUV, "pasola"=motocicleta, "carro"=automóvil, "motor"=motocicleta).

            REGLA ABSOLUTA #1 — SIEMPRE RANGO:
            Genera filtros que garanticen al menos {config.MinResultsPerPage} resultados. Si los filtros
            exactos producen < {config.MinResultsPerPage}, genera también filtros_relajados más amplios.
            Nunca generes filtros que resulten en 0 o 1 vehículo.
            Los filtros_relajados amplían: precio ±{config.PriceRelaxPercent}%, año ±{config.YearRelaxRange}, modelo=null.

            REGLA ABSOLUTA #2 — PATROCINADOS CON AFINIDAD:
            Incluye siempre en el JSON el objeto 'patrocinados_config' con los
            parámetros de afinidad para que el motor de ads inyecte listados
            relevantes. Umbral mínimo de afinidad: {config.SponsoredAffinityThreshold}.

            REGLA ABSOLUTA #3 — TRANSPARENCIA:
            Los patrocinados se etiquetan como '{config.SponsoredLabel}'. Nunca se presentan como orgánicos.

            CORRECCIONES ORTOGRÁFICAS COMUNES:
            - "hundai" → "Hyundai"
            - "toyora" → "Toyota"
            - "hiunday" → "Hyundai"
            - "tuczon" → "Tucson"
            - "corollla" → "Corolla"
            - "mecanico" → transmisión manual
            - "full" → todas las opciones/equipado

            MARCAS POR SEGMENTO (para afinidad de patrocinados):
            - Económicos: Toyota, Honda, Hyundai, Kia, Nissan, Mitsubishi, Suzuki
            - Premium: BMW, Mercedes-Benz, Audi, Lexus, Infiniti, Acura
            - Americanos: Ford, Chevrolet, Jeep, RAM, Dodge, GMC
            - Pickup/Trabajo: Toyota Hilux, Ford Ranger, Mitsubishi L200, Nissan Frontier

            TIPOS VEHICULARES:
            sedan, suv, pickup, hatchback, van, deportivo, coupe, convertible, minivan

            TRANSMISIONES: automatica, manual
            COMBUSTIBLES: gasolina, diesel, hibrido, electrico, gas
            CONDICIONES: nuevo, usado

            RESPONDE ÚNICAMENTE con un objeto JSON válido siguiendo este esquema exacto:
            {{
              "filtros_exactos": {{ "marca": str|null, "modelo": str|null, "anio_desde": int|null, "anio_hasta": int|null, "precio_min": num|null, "precio_max": num|null, "moneda": "DOP"|"USD", "tipo_vehiculo": str|null, "transmision": str|null, "combustible": str|null, "condicion": str|null, "kilometraje_max": int|null }},
              "filtros_relajados": {{ ... mismo esquema, con valores ampliados }},
              "resultado_minimo_garantizado": {config.MinResultsPerPage},
              "nivel_filtros_activo": 1-{config.MaxRelaxationLevel},
              "patrocinados_config": {{ "umbral_afinidad": float, "tipo_vehiculo_afinidad": [str], "marcas_afinidad": [str], "precio_rango_afinidad": {{ "min": num, "max": num, "moneda": str }}, "anio_rango_afinidad": {{ "desde": int, "hasta": int }}, "max_porcentaje_resultados": {config.MaxSponsoredPercentage}, "posiciones_fijas": [{config.SponsoredPositions}], "etiqueta": "{config.SponsoredLabel}" }},
              "ordenar_por": "relevancia"|"precio_asc"|"precio_desc"|"anio_desc"|"okla_score",
              "dealer_verificado": bool|null,
              "confianza": float (0.0-1.0),
              "query_reformulada": str,
              "advertencias": [str],
              "mensaje_relajamiento": str|null,
              "mensaje_usuario": str|null (solo para consultas fuera de contexto)
            }}

            Para consultas fuera de contexto (no relacionadas con vehículos):
            - filtros_exactos: null, filtros_relajados: null, patrocinados_config: null
            - confianza: 0.0, resultado_minimo_garantizado: 0
            - mensaje_usuario: sugerencia amable de cómo buscar vehículos

            Sin texto adicional. Solo JSON. NUNCA inventes datos de vehículos ni precios.
            """;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
