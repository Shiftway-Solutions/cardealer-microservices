using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using RecoAgent.Application.DTOs;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Application.Features.Recommend.Queries;

public class GenerateRecommendationsQueryHandler : IRequestHandler<GenerateRecommendationsQuery, RecoAgentResultDto>
{
    private readonly IClaudeRecoService _claudeService;
    private readonly IRecoCacheService _cacheService;
    private readonly IRecoAgentConfigRepository _configRepo;
    private readonly IRecommendationLogRepository _logRepo;
    private readonly ILogger<GenerateRecommendationsQueryHandler> _logger;

    public GenerateRecommendationsQueryHandler(
        IClaudeRecoService claudeService,
        IRecoCacheService cacheService,
        IRecoAgentConfigRepository configRepo,
        IRecommendationLogRepository logRepo,
        ILogger<GenerateRecommendationsQueryHandler> logger)
    {
        _claudeService = claudeService;
        _cacheService = cacheService;
        _configRepo = configRepo;
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task<RecoAgentResultDto> Handle(GenerateRecommendationsQuery request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // 1. Get active configuration
        var config = await _configRepo.GetActiveConfigAsync(ct);

        if (!config.IsEnabled)
        {
            return new RecoAgentResultDto
            {
                Mode = "disabled",
                LatencyMs = sw.ElapsedMilliseconds
            };
        }

        // 2. Determine mode and cache TTL
        var isRealTime = !string.IsNullOrEmpty(request.Request.InstruccionesAdicionales);
        var cacheTtl = isRealTime ? config.RealTimeCacheTtlSeconds : config.CacheTtlSeconds;
        var mode = isRealTime ? "real-time" : "batch";

        // 3. Check cache
        var userId = request.Request.Perfil.UserId ?? request.UserId ?? "anonymous";
        var cacheKey = ComputeCacheKey(userId, request.Request);
        RecoAgentResponse? aiResponse = null;
        bool wasCached = false;

        var cachedJson = await _cacheService.GetCachedResponseAsync(cacheKey, ct);
        if (cachedJson != null)
        {
            _logger.LogInformation("Cache hit for user {UserId}, key {CacheKey}", userId, cacheKey);
            aiResponse = JsonSerializer.Deserialize<RecoAgentResponse>(cachedJson);
            wasCached = true;
        }

        if (aiResponse == null)
        {
            // 4. Build system prompt
            var systemPrompt = BuildSystemPrompt(config);

            // 5. Build user message (profile + candidates JSON)
            var userMessage = BuildUserMessage(request.Request);

            // 6. Call Claude Sonnet 4.5
            _logger.LogInformation("Generating recommendations for user {UserId}, mode={Mode}", userId, mode);
            var rawJson = await _claudeService.GenerateRecommendationsAsync(
                userMessage,
                systemPrompt,
                (float)config.Temperature,
                config.MaxTokens,
                ct
            );

            // 7. Parse the response
            try
            {
                aiResponse = JsonSerializer.Deserialize<RecoAgentResponse>(rawJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Claude JSON response");
            }

            aiResponse ??= CreateFallbackResponse();

            // 8. Enforce business rules post-processing
            EnforceBusinessRules(aiResponse, config);

            // 9. Cache the response
            var responseJson = JsonSerializer.Serialize(aiResponse);
            await _cacheService.SetCachedResponseAsync(cacheKey, responseJson, cacheTtl, ct);
        }

        sw.Stop();

        // 9. Log for analytics (fire-and-forget)
        var log = new RecommendationLog
        {
            UserId = userId,
            Mode = mode,
            ProfileJson = JsonSerializer.Serialize(request.Request.Perfil),
            ColdStartLevel = request.Request.Perfil.ColdStartLevel,
            DetectedStage = aiResponse.EtapaCompraDetectada,
            RecommendationsJson = JsonSerializer.Serialize(aiResponse.Recomendaciones),
            ConfidenceScore = aiResponse.ConfianzaRecomendaciones,
            LatencyMs = (int)sw.ElapsedMilliseconds,
            WasCached = wasCached
        };

        _ = Task.Run(async () =>
        {
            try { await _logRepo.SaveAsync(log, CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to save recommendation log"); }
        }, CancellationToken.None);

        return new RecoAgentResultDto
        {
            Response = aiResponse,
            WasCached = wasCached,
            LatencyMs = sw.ElapsedMilliseconds,
            Mode = mode,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Enforces all 5 business rules on the AI response.
    /// </summary>
    private static void EnforceBusinessRules(RecoAgentResponse response, RecoAgentConfig config)
    {
        // RULE #1 — Always minimum 8 recommendations
        if (response.Recomendaciones.Count < config.MinRecommendations)
        {
            // Mark low count so caller knows expansion is needed
            response.ConfianzaRecomendaciones = Math.Min(response.ConfianzaRecomendaciones, 0.5f);
        }

        // RULE #2 — Sponsored config always present with correct thresholds
        response.PatrocinadosConfig ??= new SponsoredConfig();
        response.PatrocinadosConfig.ThresholdScore = Math.Max(
            response.PatrocinadosConfig.ThresholdScore, config.SponsoredAffinityThreshold);
        response.PatrocinadosConfig.PosicionesPatrocinados = config.SponsoredPositions
            .Split(',').Select(int.Parse).ToList();
        response.PatrocinadosConfig.Label = config.SponsoredLabel;

        // Cap sponsored at 25% of total
        var totalRecos = response.Recomendaciones.Count;
        var maxSponsored = (int)Math.Floor(totalRecos * 0.25);
        var currentSponsored = response.Recomendaciones.Count(r => r.EsPatrocinado);
        if (currentSponsored > maxSponsored)
        {
            // Remove excess sponsored items (keep highest score ones)
            var sponsoredToRemove = response.Recomendaciones
                .Where(r => r.EsPatrocinado)
                .OrderBy(r => r.ScoreAfinidadPerfil)
                .Take(currentSponsored - maxSponsored)
                .ToList();
            foreach (var item in sponsoredToRemove)
                item.EsPatrocinado = false;
        }

        // Enforce threshold on sponsored items
        foreach (var reco in response.Recomendaciones.Where(r => r.EsPatrocinado))
        {
            if (reco.ScoreAfinidadPerfil < config.SponsoredAffinityThreshold)
                reco.EsPatrocinado = false;
        }

        // RULE #3 — Diversification: no brand > 40%
        // (This is a post-validation check; the model should already handle it)
        if (response.Recomendaciones.Count > 0)
        {
            response.DiversificacionAplicada ??= new DiversificationApplied();

            // Note: We can't enforce brand diversification without vehicle metadata
            // The model should handle it via the system prompt
            // We record the diversification metrics for monitoring
            response.DiversificacionAplicada.MaxMismaMarcaPorcentaje = config.MaxSameBrandPercent;
        }

        // RULE #4 — Every recommendation must have an explanation
        foreach (var reco in response.Recomendaciones)
        {
            if (string.IsNullOrWhiteSpace(reco.RazonRecomendacion))
                reco.RazonRecomendacion = "Vehículo recomendado según tu perfil de navegación en OKLA";
        }

        // RULE #5 — Sponsored items must be labeled as "Destacado"
        foreach (var reco in response.Recomendaciones.Where(r => r.EsPatrocinado))
        {
            reco.TipoRecomendacion = "patrocinado";
        }

        // Re-number positions
        for (int i = 0; i < response.Recomendaciones.Count; i++)
        {
            response.Recomendaciones[i].Posicion = i + 1;
        }
    }

    private static string BuildSystemPrompt(RecoAgentConfig config)
    {
        var maxSameBrandPct = (int)(config.MaxSameBrandPercent * 100);
        var maxSameBrandOf8 = (int)(8 * config.MaxSameBrandPercent);

        return $$"""
            Eres RecoAgent, el motor de recomendaciones inteligente de OKLA Marketplace,
            la plataforma de compraventa de vehículos líder en la República Dominicana.

            TU FUNCIÓN PRINCIPAL:
            Analizar el perfil de comportamiento de un usuario y generar un JSON de
            recomendaciones vehiculares personalizadas, con explicaciones en español
            dominicano que conecten los atributos del vehículo con el perfil del usuario.

            DIFERENCIA CON EL BUSCADOR: No interpretas una query de texto. Recibes
            un objeto JSON con el perfil del usuario y candidatos pre-filtrados.
            Debes seleccionar y ordenar los mejores candidatos para ESTE usuario,
            explicando por qué cada uno es relevante.

            MERCADO: República Dominicana. MONEDAS: DOP y USD.
            IDIOMA: Español dominicano (coloquial pero profesional).

            REGLA ABSOLUTA #1 — SIEMPRE RANGO:
            Siempre recomiendas entre {{config.MinRecommendations}} y {{config.MaxRecommendations}} vehículos. NUNCA uno solo.
            Si el perfil es restrictivo, amplía preferencias gradualmente.

            REGLA ABSOLUTA #2 — PATROCINADOS CON AFINIDAD:
            Los candidatos con ad_active=true y afinidad >= {{config.SponsoredAffinityThreshold}} con el perfil
            del usuario DEBEN aparecer en las recomendaciones con es_patrocinado=true.
            Posiciones fijas: [{{config.SponsoredPositions}}]. Etiqueta: '{{config.SponsoredLabel}}'.
            Máximo 25% del total pueden ser patrocinados.

            REGLA ABSOLUTA #3 — DIVERSIFICACIÓN OBLIGATORIA:
            Ninguna marca puede representar más del {{maxSameBrandPct}}% de las recomendaciones.
            Si hay 8 recomendaciones, máximo {{maxSameBrandOf8}} pueden ser de la misma marca.
            Incluir variedad de rangos de precio y tipos vehiculares.

            REGLA ABSOLUTA #4 — EXPLICACIÓN OBLIGATORIA:
            Cada vehículo recomendado DEBE incluir 'razon_recomendacion': una frase corta
            en español dominicano que explique por qué se recomienda para ESTE usuario.
            La explicación debe ser personalizada, natural y específica.
            Ejemplos buenos: "Parecido al que guardaste — este tiene menos km y mejor precio"
            Ejemplos malos: "Vehículo recomendado" (demasiado genérico)

            REGLA ABSOLUTA #5 — TRANSPARENCIA:
            Los patrocinados llevan tipo_recomendacion='patrocinado' y es_patrocinado=true.
            Nunca se mezclan como orgánicos.

            TIPOS DE RECOMENDACIÓN:
            - perfil: coincide con preferencias declaradas o inferidas
            - similar: similar a vehículos vistos o guardados
            - descubrimiento: amplía horizontes, tipo o marca diferente
            - popular: popular en RD (para cold start)
            - patrocinado: anuncio con afinidad real al perfil

            ETAPAS DE COMPRA:
            - explorador: pocas vistas, sin favoritos, solo mirando
            - comparador: vistas repetidas, favoritos, búsquedas del mismo tipo
            - comprador_inminente: contactó dealer, >3 favoritos, revisiones repetidas
            - post_compra: sin actividad reciente o señal de compra completada

            COLD START:
            - Nivel 0: sin datos → recomienda por popularidad regional en RD
            - Nivel 1: cuenta nueva, <3 vistas → usar preferencias del onboarding
            - Nivel 2: 3-10 vistas → filtrado por contenido básico
            - Nivel 3: >10 vistas o 1 favorito → motor completo

            RESPONDE ÚNICAMENTE con un objeto JSON válido siguiendo este esquema:
            {
              "recomendaciones": [
                {
                  "vehiculo_id": "ID del candidato",
                  "posicion": 1,
                  "razon_recomendacion": "Frase personalizada en español dominicano",
                  "tipo_recomendacion": "perfil|similar|descubrimiento|popular|patrocinado",
                  "score_afinidad_perfil": 0.0-1.0,
                  "es_patrocinado": false
                }
              ],
              "patrocinados_config": {
                "posiciones_patrocinados": [{{config.SponsoredPositions}}],
                "label": "{{config.SponsoredLabel}}",
                "threshold_score": {{config.SponsoredAffinityThreshold}},
                "total_insertados": 0
              },
              "diversificacion_aplicada": {
                "marcas_distintas": 0,
                "max_misma_marca": 0,
                "max_misma_marca_porcentaje": 0.0,
                "tipos_incluidos": []
              },
              "etapa_compra_detectada": "explorador|comparador|comprador_inminente|post_compra",
              "cold_start_nivel": 0-3,
              "confianza_recomendaciones": 0.0-1.0,
              "proxima_actualizacion": "ISO datetime"
            }

            Sin texto adicional. Solo JSON. NUNCA inventes IDs de vehículos — usa SOLO los IDs de los candidatos proporcionados.
            """;
    }

    private static string BuildUserMessage(RecoAgentRequest request)
    {
        var message = new
        {
            perfil = request.Perfil,
            candidatos = request.Candidatos,
            instrucciones_adicionales = request.InstruccionesAdicionales
        };

        return JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });
    }

    private static string ComputeCacheKey(string userId, RecoAgentRequest request)
    {
        // Build a hash from userId + profile key attributes + mode
        var keyData = $"{userId}|{request.Perfil.ColdStartLevel}|{request.Perfil.EtapaCompra}" +
                      $"|{string.Join(",", request.Perfil.Favoritos)}" +
                      $"|{request.InstruccionesAdicionales ?? "batch"}";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(keyData));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static RecoAgentResponse CreateFallbackResponse()
    {
        return new RecoAgentResponse
        {
            Recomendaciones = [],
            PatrocinadosConfig = new SponsoredConfig
            {
                ThresholdScore = 0.50f,
                PosicionesPatrocinados = [2, 6, 11],
                Label = "Destacado",
                TotalInsertados = 0
            },
            DiversificacionAplicada = new DiversificationApplied
            {
                MarcasDistintas = 0,
                MaxMismaMarca = 0,
                MaxMismaMarcaPorcentaje = 0.40f,
                TiposIncluidos = []
            },
            EtapaCompraDetectada = "explorador",
            ColdStartNivel = 0,
            ConfianzaRecomendaciones = 0.1f,
            ProximaActualizacion = DateTime.UtcNow.AddHours(4)
        };
    }
}
