using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using RecoAgent.Application.DTOs;
using RecoAgent.Application.Services;
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

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER: Prompt Injection Detection (pre-LLM)
        // InstruccionesAdicionales is user-supplied free text sent to Claude.
        // ══════════════════════════════════════════════════════════════
        if (isRealTime)
        {
            var injectionResult = PromptInjectionDetector.Detect(request.Request.InstruccionesAdicionales!);
            if (injectionResult.ShouldBlock)
            {
                _logger.LogWarning(
                    "RecoAgent prompt injection BLOCKED. Threat={Threat}, Patterns={Patterns}, UserId={UserId}",
                    injectionResult.ThreatLevel,
                    string.Join(", ", injectionResult.DetectedPatterns),
                    request.UserId);

                return new RecoAgentResultDto
                {
                    Mode = "blocked",
                    LatencyMs = sw.ElapsedMilliseconds,
                    Response = CreateFallbackResponse()
                };
            }

            if (injectionResult.IsInjectionDetected)
            {
                _logger.LogWarning(
                    "RecoAgent prompt injection SANITIZED. Threat={Threat}, Patterns={Patterns}, UserId={UserId}",
                    injectionResult.ThreatLevel,
                    string.Join(", ", injectionResult.DetectedPatterns),
                    request.UserId);

                request.Request.InstruccionesAdicionales =
                    PromptInjectionDetector.Sanitize(request.Request.InstruccionesAdicionales!);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER: Indirect Injection via Candidate Data (pre-LLM)
        // RED TEAM v2: Scan candidate text fields for injection markers
        // that malicious sellers could embed in vehicle listings.
        // ══════════════════════════════════════════════════════════════
        if (request.Request.Candidatos != null)
        {
            foreach (var candidate in request.Request.Candidatos)
            {
                var textFields = new[] { candidate.Marca, candidate.Modelo, candidate.Ubicacion, candidate.Tipo }
                    .Where(f => !string.IsNullOrEmpty(f));

                foreach (var field in textFields)
                {
                    var fieldCheck = PromptInjectionDetector.Detect(field!);
                    if (fieldCheck.IsInjectionDetected)
                    {
                        _logger.LogWarning(
                            "Indirect injection detected in candidate data. VehicleId={VehicleId}, Threat={Threat}, Patterns={Patterns}",
                            candidate.Id, fieldCheck.ThreatLevel,
                            string.Join(", ", fieldCheck.DetectedPatterns));

                        // Sanitize the field in-place
                        if (field == candidate.Marca) candidate.Marca = PromptInjectionDetector.Sanitize(candidate.Marca);
                        else if (field == candidate.Modelo) candidate.Modelo = PromptInjectionDetector.Sanitize(candidate.Modelo);
                        else if (field == candidate.Ubicacion) candidate.Ubicacion = PromptInjectionDetector.Sanitize(candidate.Ubicacion!);
                        else if (field == candidate.Tipo) candidate.Tipo = PromptInjectionDetector.Sanitize(candidate.Tipo);
                    }
                }
            }
        }

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
            // 4. Build system prompt (use admin override if configured, otherwise auto-build)
            // RED TEAM v2: Validate admin override to prevent compromised admin prompt injection
            var systemPrompt = config.SystemPromptOverride ?? BuildSystemPrompt(config);
            var promptSource = config.SystemPromptOverride != null ? "admin_override" : "auto_built";

            if (config.SystemPromptOverride != null)
            {
                var overrideError = SystemPromptValidator.Validate(config.SystemPromptOverride);
                if (overrideError != null)
                {
                    _logger.LogWarning(
                        "Invalid system prompt override detected, falling back to auto-built. Error={Error}, UserId={UserId}",
                        overrideError, userId);
                    systemPrompt = BuildSystemPrompt(config);
                    promptSource = "auto_built_fallback";
                }
            }

            // 5. Build user message (profile + candidates JSON)
            var userMessage = BuildUserMessage(request.Request);

            // 6. Call Claude Sonnet 4.5
            _logger.LogInformation(
                "Generating recommendations for user {UserId}, mode={Mode}, promptSource={PromptSource}, promptLength={PromptLength}",
                userId, mode, promptSource, systemPrompt.Length);
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
            EnforceBusinessRules(aiResponse, config, request.Request.Candidatos);

            // 8b. Deduplication: remove duplicate vehiculo_id entries
            DeduplicateRecommendations(aiResponse);

            // 8c. Enforce excluded brands from user profile
            EnforceExcludedBrands(aiResponse, request.Request.Candidatos,
                request.Request.Perfil.MarcasExcluidas);

            // 8d. Anti-hallucination guardrail: strip any vehiculo_ids not present in candidates
            ValidateAndFilterCandidateIds(aiResponse, request.Request.Candidatos);

            // ══════════════════════════════════════════════════════════
            // SAFETY LAYER: Output Content Validation (post-LLM)
            // RED TEAM v2: PII detection, offensive content, prompt leakage
            // ══════════════════════════════════════════════════════════
            var validationResult = OutputContentValidator.Validate(aiResponse);
            if (validationResult.HasIssues)
            {
                _logger.LogWarning(
                    "RecoAgent output validation found {Count} issues: {Issues}, UserId={UserId}",
                    validationResult.IssueCount,
                    string.Join(", ", validationResult.Issues),
                    userId);
            }

            // 9. Cache the response + store reverse key mapping for invalidation
            var responseJson = JsonSerializer.Serialize(aiResponse);
            await _cacheService.SetCachedResponseAsync(cacheKey, responseJson, cacheTtl, ct);
            await _cacheService.StoreUserCacheKeyMappingAsync(userId, cacheKey, mode, cacheTtl, ct);
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
    private static void EnforceBusinessRules(RecoAgentResponse response, RecoAgentConfig config, List<VehicleCandidate> candidates)
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

        // RULE #3 — Diversification: no brand > 40% — ENFORCED
        if (response.Recomendaciones.Count > 0)
        {
            response.DiversificacionAplicada ??= new DiversificationApplied();
            response.DiversificacionAplicada.MaxMismaMarcaPorcentaje = config.MaxSameBrandPercent;

            // Actually enforce brand cap by removing excess same-brand recommendations
            EnforceBrandDiversification(response, candidates, config.MaxSameBrandPercent);
        }

        // RULE #4 — Every recommendation must have an explanation (Dominican Spanish)
        var fallbackExplanations = new[]
        {
            "Este modelo es de los que más se mueve en RD — dale un ojo 👀",
            "Tiene buen kilometraje y precio competitivo pa' lo que ofrece",
            "Popular entre compradores con tu mismo perfil en OKLA",
            "Buen vehículo en su rango — revísalo, te puede gustar",
            "Opción sólida que encaja con lo que has estado viendo",
            "De los mejor puntuados en OKLA por calidad y precio",
            "Buena relación precio-equipamiento — compáralo con los que guardaste 📊",
            "Verificado por OKLA con buenas fotos y descripción completa ✅",
            "Opción interesante pa' diversificar tus opciones — no te lo pierdas",
            "De los más buscados esta semana en el marketplace 🔥",
            "Buen punto de partida si quieres comparar varias marcas",
            "Con historial limpio y buen score — vale la pena revisarlo 💰"
        };
        // Build candidate lookup for vehicle-specific fallbacks
        var candidateLookup = candidates.ToDictionary(c => c.Id, c => c, StringComparer.OrdinalIgnoreCase);
        var fallbackIndex = 0;
        foreach (var reco in response.Recomendaciones)
        {
            if (string.IsNullOrWhiteSpace(reco.RazonRecomendacion))
            {
                // Try vehicle-specific fallback first
                if (candidateLookup.TryGetValue(reco.VehiculoId, out var cand))
                {
                    reco.RazonRecomendacion = GenerateVehicleSpecificFallback(cand, fallbackIndex);
                }
                else
                {
                    reco.RazonRecomendacion = fallbackExplanations[fallbackIndex % fallbackExplanations.Length];
                }
                fallbackIndex++;
            }
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

    /// <summary>
    /// Enforces brand diversification by replacing excess same-brand items with
    /// candidates from underrepresented brands. Max 40% of recommendations can
    /// be from the same brand.
    /// </summary>
    private static void EnforceBrandDiversification(
        RecoAgentResponse response,
        List<VehicleCandidate> candidates,
        float maxSameBrandPercent)
    {
        if (response.Recomendaciones.Count == 0 || candidates.Count == 0)
            return;

        // Build a lookup from candidate ID → brand
        var candidateBrands = candidates
            .ToDictionary(c => c.Id, c => c.Marca, StringComparer.OrdinalIgnoreCase);

        // Count brands in current recommendations
        var brandCounts = response.Recomendaciones
            .Where(r => candidateBrands.ContainsKey(r.VehiculoId))
            .GroupBy(r => candidateBrands[r.VehiculoId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var maxAllowed = (int)Math.Ceiling(response.Recomendaciones.Count * maxSameBrandPercent);
        if (maxAllowed < 1) maxAllowed = 1;

        // Identify over-represented brands
        var overBrands = brandCounts
            .Where(kv => kv.Value > maxAllowed)
            .Select(kv => kv.Key)
            .ToList();

        if (overBrands.Count == 0)
        {
            // Update diversification metrics
            response.DiversificacionAplicada!.MarcasDistintas = brandCounts.Count;
            response.DiversificacionAplicada.MaxMismaMarca = brandCounts.Values.DefaultIfEmpty(0).Max();
            response.DiversificacionAplicada.TiposIncluidos = response.Recomendaciones
                .Where(r => candidateBrands.ContainsKey(r.VehiculoId))
                .Select(r => candidates.First(c => c.Id.Equals(r.VehiculoId, StringComparison.OrdinalIgnoreCase)).Tipo)
                .Distinct()
                .ToList();
            return;
        }

        // Get IDs already in recommendations
        var usedIds = new HashSet<string>(
            response.Recomendaciones.Select(r => r.VehiculoId),
            StringComparer.OrdinalIgnoreCase);

        // Get available replacement candidates (not already in recommendations, different brand)
        var replacementPool = candidates
            .Where(c => !usedIds.Contains(c.Id) && !overBrands.Contains(c.Marca, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(c => c.OklaScore)
            .ToList();

        foreach (var brand in overBrands)
        {
            var excess = brandCounts[brand] - maxAllowed;
            if (excess <= 0) continue;

            // Find the lowest-affinity items of this over-represented brand (non-sponsored first)
            var toReplace = response.Recomendaciones
                .Where(r => candidateBrands.TryGetValue(r.VehiculoId, out var b)
                             && b.Equals(brand, StringComparison.OrdinalIgnoreCase)
                             && !r.EsPatrocinado) // Never replace sponsored items
                .OrderBy(r => r.ScoreAfinidadPerfil)
                .Take(excess)
                .ToList();

            foreach (var item in toReplace)
            {
                if (replacementPool.Count == 0) break;

                var replacement = replacementPool[0];
                replacementPool.RemoveAt(0);

                item.VehiculoId = replacement.Id;
                item.TipoRecomendacion = "descubrimiento";
                item.RazonRecomendacion = $"Pa' que veas opciones diferentes — este {replacement.Marca} {replacement.Modelo} tiene buen score en OKLA";
                item.ScoreAfinidadPerfil = Math.Max(0.4f, item.ScoreAfinidadPerfil * 0.9f);
                usedIds.Add(replacement.Id);
            }
        }

        // Recalculate diversification metrics
        var updatedBrandCounts = response.Recomendaciones
            .Where(r => candidateBrands.ContainsKey(r.VehiculoId))
            .GroupBy(r => candidateBrands[r.VehiculoId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count());

        response.DiversificacionAplicada!.MarcasDistintas = updatedBrandCounts.Count;
        response.DiversificacionAplicada.MaxMismaMarca = updatedBrandCounts.Values.DefaultIfEmpty(0).Max();
        response.DiversificacionAplicada.TiposIncluidos = response.Recomendaciones
            .Where(r => candidateBrands.ContainsKey(r.VehiculoId))
            .Select(r => candidates.First(c => c.Id.Equals(r.VehiculoId, StringComparison.OrdinalIgnoreCase)).Tipo)
            .Distinct()
            .ToList();
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
            Ejemplos buenos (úsalos como referencia de TONO y ESTILO):
            - "Parecido al que guardaste — este tiene menos km y mejor precio"
            - "De los SUV más buscados en Santiago — y está en tu rango de precio 💰"
            - "Automático como te gusta, con buen historial y verificado por OKLA ✅"
            - "Este Hyundai Tucson está un pelo más barato que el Toyota que viste — dale un ojo"
            - "Pa' lo que cuesta, este tiene bastante equipo — aros, cámara y pantalla"
            - "Mismo tipo de yipeta que guardaste, pero de otra marca pa' que compares"
            - "Popular en RD este año — los compradores lo tienen como primera opción"
            - "Dealer verificado, buenas fotos y OKLA Score alto — señal de buen listado"
            Ejemplos MALOS (NUNCA uses estos):
            - "Vehículo recomendado" (demasiado genérico)
            - "Buen vehículo" (no dice nada)
            - "Recomendado para usted" (impersonal, no es dominicano)

            USO DE FEEDBACK DEL USUARIO:
            Si el perfil incluye 'feedback_reco' con items tipo 'thumbs_down' o 'dismiss',
            EVITA recomendar vehículos similares (misma marca+tipo+rango de precio).
            Si hay 'thumbs_up' o 'click', PRIORIZA vehículos similares a esos.

            PROXIMIDAD GEOGRÁFICA:
            Si el perfil tiene historial de búsquedas en una provincia, prioriza candidatos
            con 'ubicacion' en esa misma provincia o provincias cercanas.
            Menciona la ubicación en la explicación cuando sea relevante:
            - "Está en Santiago, cerca de tu zona 📍"
            - "Este lo tienen en la capital — verificado por OKLA"

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

            CAMPOS DE CANDIDATOS (referencia para interpretación correcta):
            - id: identificador único del vehículo en OKLA — copia este valor EXACTO en vehiculo_id
            - okla_score: score de calidad 0-100 (fotos, descripción, precio competitivo)
            - ad_active: true = este dealer paga para aparecer como patrocinado (aplica regla #2)
            - dealer_verificado: true = identidad del vendedor verificada por OKLA
            - fotos_count: número de fotos del listado (más fotos = mejor calidad)
            - ubicacion: provincia/ciudad en República Dominicana

            ⛔ ANTI-ALUCINACIÓN (CRÍTICO — INCUMPLIR ESTO ES UN ERROR GRAVE):
            1. NUNCA inventes vehiculo_ids. Usa SOLAMENTE los valores exactos del campo "id" de los candidatos.
            2. Si tienes menos de 8 candidatos, incluye TODOS los disponibles y no inventes más.
            3. NO repitas el mismo vehiculo_id en múltiples recomendaciones.
            4. Si "candidatos" está vacío, devuelve "recomendaciones": [] y cold_start_nivel: 0.
            5. Cada vehiculo_id en tu respuesta DEBE existir literalmente en la lista de candidatos.

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

            ⚖️ CUMPLIMIENTO LEGAL (República Dominicana):
            - Ley 358-05: Las "razon_recomendacion" NUNCA deben decir "precio final". Usar "precio de referencia".
            - Ley 172-13: NUNCA incluir datos personales del usuario en la respuesta JSON.
            - Ley 155-17: Nunca recomendar transacciones anónimas ni fuera de la plataforma.

            Sin texto adicional. Solo JSON válido. Sin markdown.
            """;
    }

    // ── Reuse static JsonSerializerOptions (avoid per-request allocation) ──
    private static readonly JsonSerializerOptions _snakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    private static string BuildUserMessage(RecoAgentRequest request)
    {
        var message = new
        {
            perfil = request.Perfil,
            candidatos = request.Candidatos,
            instrucciones_adicionales = request.InstruccionesAdicionales
        };

        return JsonSerializer.Serialize(message, _snakeCaseOptions);
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

    /// <summary>
    /// Enforces excluded brands from user profile by removing any recommendation
    /// whose brand is in the user's exclusion list.
    /// </summary>
    private void EnforceExcludedBrands(
        RecoAgentResponse response,
        List<VehicleCandidate> candidates,
        List<string>? excludedBrands)
    {
        if (excludedBrands == null || excludedBrands.Count == 0)
            return;

        var candidateBrands = candidates
            .ToDictionary(c => c.Id, c => c.Marca, StringComparer.OrdinalIgnoreCase);

        var excluded = new HashSet<string>(excludedBrands, StringComparer.OrdinalIgnoreCase);
        var before = response.Recomendaciones.Count;

        response.Recomendaciones = response.Recomendaciones
            .Where(r => !candidateBrands.TryGetValue(r.VehiculoId, out var brand) ||
                        !excluded.Contains(brand))
            .ToList();

        var removed = before - response.Recomendaciones.Count;
        if (removed > 0)
        {
            _logger.LogInformation(
                "RecoAgent: Removed {Count} recommendation(s) from excluded brands: {Brands}",
                removed, string.Join(", ", excludedBrands));
        }
    }

    /// <summary>
    /// Removes duplicate vehiculo_id entries from recommendations.
    /// Keeps the first occurrence (highest position/priority).
    /// </summary>
    private static void DeduplicateRecommendations(RecoAgentResponse response)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        response.Recomendaciones = response.Recomendaciones
            .Where(r => seen.Add(r.VehiculoId))
            .ToList();
    }

    /// <summary>
    /// Generates a vehicle-specific fallback explanation using candidate data,
    /// resulting in more natural and informative Dominican Spanish explanations.
    /// </summary>
    private static string GenerateVehicleSpecificFallback(VehicleCandidate candidate, int index)
    {
        var templates = new Func<VehicleCandidate, string>[]
        {
            c => $"Este {c.Marca} {c.Modelo} tiene buen score en OKLA — dale un ojo 👀",
            c => c.OklaScore >= 80
                ? $"{c.Marca} {c.Modelo} con OKLA Score de {c.OklaScore} — calidad verificada ✅"
                : $"Opción interesante en {c.Marca} — revísalo pa' que compares",
            c => c.DealerVerificado
                ? $"De dealer verificado en OKLA — este {c.Marca} {c.Modelo} vale la pena revisarlo"
                : $"{c.Marca} {c.Modelo} con buen precio pa' lo que ofrece 💰",
            c => !string.IsNullOrEmpty(c.Ubicacion)
                ? $"Este {c.Marca} {c.Modelo} está en {c.Ubicacion} — puede quedarte cerca 📍"
                : $"Popular en el marketplace — {c.Marca} {c.Modelo} con buenas fotos",
            c => c.FotosCount >= 8
                ? $"Con {c.FotosCount} fotos y descripción completa — se ve transparente"
                : $"Buena opción pa' ampliar tu comparación de {c.Tipo ?? "vehículos"}",
            c => $"De los {c.Marca} más buscados esta semana en RD 🔥",
        };

        return templates[index % templates.Length](candidate);
    }

    /// <summary>
    /// Removes recommendations whose vehiculo_id does not exist in the provided candidates.
    /// This is the primary anti-hallucination guardrail — AI must ONLY reference provided IDs.
    /// </summary>
    private void ValidateAndFilterCandidateIds(
        RecoAgentResponse response,
        List<VehicleCandidate> candidates)
    {
        if (candidates.Count == 0)
            return; // Cold start level 0/1: no candidates expected

        var validIds = new HashSet<string>(
            candidates.Select(c => c.Id),
            StringComparer.OrdinalIgnoreCase);

        var before = response.Recomendaciones.Count;

        response.Recomendaciones = response.Recomendaciones
            .Where(r => !string.IsNullOrEmpty(r.VehiculoId) && validIds.Contains(r.VehiculoId))
            .ToList();

        var removed = before - response.Recomendaciones.Count;
        if (removed > 0)
        {
            _logger.LogWarning(
                "RecoAgent anti-hallucination: removed {Count} recommendation(s) with IDs not in "
                + "candidate list. Confidence reduced from {OldConf:F2} to {NewConf:F2}",
                removed,
                response.ConfianzaRecomendaciones,
                response.ConfianzaRecomendaciones * 0.5f);

            response.ConfianzaRecomendaciones =
                (float)Math.Round(response.ConfianzaRecomendaciones * 0.5f, 2);
        }
    }
}