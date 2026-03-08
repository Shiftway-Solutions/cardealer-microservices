using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// Configuración del servicio de embeddings.
/// Puede apuntar al LlmServer self-hosted o a un proveedor externo
/// (HuggingFace Inference API, OpenAI, etc.).
/// </summary>
public class EmbeddingSettings
{
    /// <summary>URL del servidor de embeddings.</summary>
    /// <remarks>
    /// - Self-hosted: "http://llm-server:8000" (mismo servidor que el LLM)
    /// - HuggingFace Inference API: "https://api-inference.huggingface.co" (gratuito)
    /// - OpenAI: "https://api.openai.com"
    /// </remarks>
    public string ServerUrl { get; set; } = "http://llm-server:8000";
    
    /// <summary>Modelo de embeddings a usar</summary>
    public string Model { get; set; } = "all-MiniLM-L6-v2";
    
    /// <summary>Dimensiones del vector de embedding</summary>
    public int Dimensions { get; set; } = 384;
    
    /// <summary>Timeout en segundos para generar embeddings</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// API Key para autenticación con proveedores externos (HuggingFace, OpenAI).
    /// Si está vacío, no se envía header Authorization (modo self-hosted).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Ruta del endpoint de embeddings. Default: /v1/embeddings (OpenAI-compatible).
    /// Para HuggingFace Inference API gratuito usar:
    /// "/models/sentence-transformers/all-MiniLM-L6-v2"
    /// </summary>
    public string EmbeddingsPath { get; set; } = "/v1/embeddings";

    /// <summary>
    /// Proveedor de embeddings. Determina el formato de request/response.
    /// "openai" = formato OpenAI (compatible con llm-server, OpenAI, y HF Inference Endpoints).
    /// "huggingface" = formato HuggingFace Inference API gratuito (request/response diferente).
    /// </summary>
    public string Provider { get; set; } = "openai";
}

/// <summary>
/// Servicio que genera embeddings de texto para búsqueda vectorial (RAG).
/// Soporta múltiples proveedores:
///   - Self-hosted llm-server (llama.cpp + sentence-transformers)
///   - HuggingFace Inference API (gratuito)
///   - OpenAI Embeddings API
///   - Cualquier API compatible con formato OpenAI /v1/embeddings
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        IOptions<EmbeddingSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<EmbeddingService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var results = await GenerateEmbeddingsBatchAsync(new List<string> { text }, ct);
        var embedding = results.FirstOrDefault() ?? Array.Empty<float>();

        // Validate: reject zero-vectors (they match everything in cosine similarity)
        if (embedding.Length > 0 && embedding.All(v => v == 0f))
        {
            _logger.LogWarning("Embedding service returned zero-vector for text: {TextPreview}",
                text.Length > 80 ? text[..80] + "..." : text);
            return Array.Empty<float>();
        }

        // Validate dimension matches expected
        if (embedding.Length > 0 && embedding.Length != _settings.Dimensions)
        {
            _logger.LogError("Embedding dimension mismatch: expected {Expected}, got {Actual}",
                _settings.Dimensions, embedding.Length);
            return Array.Empty<float>();
        }

        return embedding;
    }

    public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(
        List<string> texts, CancellationToken ct = default)
    {
        if (!texts.Any())
            return new List<float[]>();

        // Batch size limit to prevent OOM with large inventories
        const int maxBatchSize = 64;

        if (texts.Count > maxBatchSize)
        {
            _logger.LogInformation("Splitting {Count} texts into batches of {BatchSize}",
                texts.Count, maxBatchSize);

            var allResults = new List<float[]>();
            for (int i = 0; i < texts.Count; i += maxBatchSize)
            {
                var batch = texts.Skip(i).Take(maxBatchSize).ToList();
                var batchResults = await GenerateSingleBatchAsync(batch, ct);
                allResults.AddRange(batchResults);
            }
            return allResults;
        }

        return await GenerateSingleBatchAsync(texts, ct);
    }

    private async Task<List<float[]>> GenerateSingleBatchAsync(
        List<string> texts, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var client = _httpClientFactory.CreateClient("EmbeddingServer");

            if (_settings.Provider.Equals("huggingface", StringComparison.OrdinalIgnoreCase))
            {
                // ── HuggingFace Inference API (gratuito) ────────────────
                // Request: { "inputs": ["text1", "text2"] }
                // Response: [[0.1, 0.2, ...], [0.3, 0.4, ...]]
                return await GenerateEmbeddingsHuggingFaceAsync(client, texts, linkedCts.Token);
            }

            // ── OpenAI-compatible format (llm-server, OpenAI, HF Endpoints) ──
            var request = new
            {
                input = texts,
                model = _settings.Model
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.EmbeddingsPath);
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            }
            httpRequest.Content = JsonContent.Create(request);

            var response = await client.SendAsync(httpRequest, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                cancellationToken: linkedCts.Token);

            if (result?.Data == null)
            {
                _logger.LogWarning("Embedding response has no data — returning empty arrays (will skip RAG)");
                return texts.Select(_ => Array.Empty<float>()).ToList();
            }

            return result.Data
                .OrderBy(d => d.Index)
                .Select(d => d.Embedding)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings for {Count} texts", texts.Count);
            // Return empty arrays — callers must check length > 0 before using
            return texts.Select(_ => Array.Empty<float>()).ToList();
        }
    }

    private class EmbeddingResponse
    {
        public List<EmbeddingData>? Data { get; set; }
        public string? Model { get; set; }
    }

    private class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Genera embeddings usando el formato de HuggingFace Inference API (gratuito).
    /// Request:  { "inputs": ["text1", "text2"] }
    /// Response: [[0.1, 0.2, ...], [0.3, 0.4, ...]]
    /// </summary>
    private async Task<List<float[]>> GenerateEmbeddingsHuggingFaceAsync(
        HttpClient client, List<string> texts, CancellationToken ct)
    {
        var hfRequest = new { inputs = texts };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.EmbeddingsPath);
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
        httpRequest.Content = JsonContent.Create(hfRequest);

        var response = await client.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        // HF returns a flat array of arrays: [[0.1, 0.2, ...], [0.3, 0.4, ...]]
        var embeddings = await response.Content.ReadFromJsonAsync<List<float[]>>(cancellationToken: ct);

        if (embeddings == null || embeddings.Count != texts.Count)
        {
            _logger.LogWarning("HuggingFace embedding response mismatch: expected {Expected}, got {Actual}",
                texts.Count, embeddings?.Count ?? 0);
            return texts.Select(_ => new float[_settings.Dimensions]).ToList();
        }

        return embeddings;
    }
}

/// <summary>
/// Servicio de búsqueda vectorial usando pgvector para RAG.
/// Implementa búsqueda híbrida: semántica (cosine similarity) + filtros SQL.
/// Includes minimum relevance filtering and latency measurement.
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVehicleEmbeddingRepository _embeddingRepository;
    private readonly ILogger<VectorSearchService> _logger;

    /// <summary>
    /// Minimum cosine similarity score to include a result.
    /// Below this threshold, results are considered irrelevant noise.
    /// Cosine similarity range: 0 (completely different) to 1 (identical).
    /// 0.25 = very loose relevance, 0.5 = moderate, 0.7 = highly relevant.
    /// </summary>
    private const float MinRelevanceScore = 0.30f;

    /// <summary>
    /// Maximum acceptable latency for search operations in milliseconds.
    /// Used for SLA monitoring and alerting.
    /// </summary>
    private const int LatencySlaMs = 500;

    public VectorSearchService(
        IEmbeddingService embeddingService,
        IVehicleEmbeddingRepository embeddingRepository,
        ILogger<VectorSearchService> logger)
    {
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _logger = logger;
    }

    public async Task<List<VehicleSearchResult>> SearchAsync(
        Guid dealerId,
        string query,
        VehicleSearchFilters? filters = null,
        int topK = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<VehicleSearchResult>();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 1. Generar embedding del query del usuario
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        
        var embeddingMs = stopwatch.ElapsedMilliseconds;

        if (queryEmbedding.Length == 0)
        {
            _logger.LogWarning("Failed to generate query embedding, falling back to text search");
            return new List<VehicleSearchResult>();
        }

        // 2. Búsqueda híbrida: vector similarity + SQL filters
        var results = await _embeddingRepository.HybridSearchAsync(
            dealerId, queryEmbedding, filters, topK, ct);

        var searchMs = stopwatch.ElapsedMilliseconds;
        stopwatch.Stop();

        // 3. Filter out irrelevant results (below minimum similarity threshold)
        var relevantResults = results
            .Where(r => r.SimilarityScore >= MinRelevanceScore)
            .ToList();

        var filteredOut = results.Count - relevantResults.Count;

        // 4. Log latency and results for SLA monitoring
        var totalMs = searchMs;
        var latencyLevel = totalMs > LatencySlaMs ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(latencyLevel,
            "Vector search for dealer {DealerId}: query='{Query}', " +
            "results={Count}/{Total} (filtered {FilteredOut} below {MinScore} threshold), " +
            "latency={TotalMs}ms (embedding={EmbeddingMs}ms, search={SearchMs}ms), " +
            "SLA={SlaStatus}, filters={HasFilters}",
            dealerId, query.Length > 50 ? query[..50] + "..." : query,
            relevantResults.Count, results.Count, filteredOut, MinRelevanceScore,
            totalMs, embeddingMs, searchMs - embeddingMs,
            totalMs <= LatencySlaMs ? "OK" : $"BREACH ({totalMs}ms > {LatencySlaMs}ms)",
            filters != null);

        return relevantResults;
    }

    public async Task UpsertVehicleEmbeddingAsync(
        ChatbotVehicle vehicle, Guid dealerId, CancellationToken ct = default)
    {
        // Construir texto para embedding
        var text = BuildVehicleEmbeddingText(vehicle);
        
        // Generar embedding
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text, ct);
        
        if (embedding.Length == 0)
        {
            _logger.LogWarning("Failed to generate embedding for vehicle {VehicleId}", vehicle.VehicleId);
            return;
        }

        var vehicleEmbedding = new VehicleEmbedding
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.VehicleId,
            DealerId = dealerId,
            Content = text,
            Embedding = embedding,
            Metadata = new VehicleEmbeddingMetadata
            {
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Price = vehicle.Price,
                FuelType = vehicle.FuelType,
                Transmission = vehicle.Transmission,
                BodyType = vehicle.BodyType,
                Mileage = vehicle.Mileage,
                ExteriorColor = vehicle.ExteriorColor ?? vehicle.Color,
                IsAvailable = vehicle.IsAvailable
            },
            UpdatedAt = DateTime.UtcNow
        };

        await _embeddingRepository.UpsertAsync(vehicleEmbedding, ct);
        
        _logger.LogDebug("Upserted embedding for vehicle {VehicleId} ({Year} {Make} {Model})",
            vehicle.VehicleId, vehicle.Year, vehicle.Make, vehicle.Model);
    }

    public async Task DeleteVehicleEmbeddingAsync(Guid vehicleId, CancellationToken ct = default)
    {
        await _embeddingRepository.DeleteByVehicleIdAsync(vehicleId, ct);
        _logger.LogDebug("Deleted embedding for vehicle {VehicleId}", vehicleId);
    }

    public async Task<int> RebuildDealerEmbeddingsAsync(
        Guid dealerId, IEnumerable<ChatbotVehicle> vehicles, CancellationToken ct = default)
    {
        var vehicleList = vehicles.ToList();
        if (!vehicleList.Any()) return 0;

        // Generar textos para todos los vehículos
        var texts = vehicleList.Select(BuildVehicleEmbeddingText).ToList();
        
        // Generar embeddings en batch
        var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(texts, ct);

        // Crear entidades de embedding
        var vehicleEmbeddings = vehicleList.Select((vehicle, i) => new VehicleEmbedding
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.VehicleId,
            DealerId = dealerId,
            Content = texts[i],
            Embedding = embeddings[i],
            Metadata = new VehicleEmbeddingMetadata
            {
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Price = vehicle.Price,
                FuelType = vehicle.FuelType,
                Transmission = vehicle.Transmission,
                BodyType = vehicle.BodyType,
                Mileage = vehicle.Mileage,
                ExteriorColor = vehicle.ExteriorColor ?? vehicle.Color,
                IsAvailable = vehicle.IsAvailable
            },
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        // Bulk upsert
        await _embeddingRepository.BulkUpsertAsync(dealerId, vehicleEmbeddings, ct);

        _logger.LogInformation("Rebuilt {Count} embeddings for dealer {DealerId}",
            vehicleEmbeddings.Count, dealerId);

        return vehicleEmbeddings.Count;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        return await _embeddingService.GenerateEmbeddingAsync(text, ct);
    }

    /// <summary>
    /// Construye el texto de un vehículo para generar su embedding.
    /// Formato optimizado para búsqueda semántica en español dominicano.
    /// Includes all searchable fields + condition for better semantic matching.
    /// </summary>
    private static string BuildVehicleEmbeddingText(ChatbotVehicle vehicle)
    {
        var parts = new List<string>
        {
            $"{vehicle.Year} {vehicle.Make} {vehicle.Model}",
            vehicle.Trim ?? "",
            $"Precio RD${vehicle.Price:N0}",
            vehicle.Mileage.HasValue ? $"{vehicle.Mileage:N0} kilómetros" : "",
            $"Combustible: {vehicle.FuelType ?? "N/A"}",
            $"Transmisión: {vehicle.Transmission ?? "N/A"}",
            $"Tipo: {vehicle.BodyType ?? "N/A"}",
            $"Color: {vehicle.ExteriorColor ?? vehicle.Color ?? "N/A"}",
            !string.IsNullOrEmpty(vehicle.Description) ? vehicle.Description : "",
            vehicle.IsOnSale ? "EN OFERTA ESPECIAL" : ""
        };

        return string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
