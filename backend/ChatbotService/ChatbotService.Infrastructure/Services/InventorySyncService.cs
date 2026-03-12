using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;
using System.Net.Http.Json;

namespace ChatbotService.Infrastructure.Services;

public class InventorySyncSettings
{
    public string VehiclesApiUrl { get; set; } = "http://vehiclessaleservice:8080/api/vehicles";
    public int BatchSize { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 60;
}

public class InventorySyncService : IInventorySyncService
{
    private readonly InventorySyncSettings _settings;
    private readonly IChatbotVehicleRepository _vehicleRepository;
    private readonly IVectorSearchService _vectorSearch;
    private readonly ILogger<InventorySyncService> _logger;
    private readonly HttpClient _httpClient;

    public InventorySyncService(
        IOptions<InventorySyncSettings> settings,
        IChatbotVehicleRepository vehicleRepository,
        IVectorSearchService vectorSearch,
        IHttpClientFactory httpClientFactory,
        ILogger<InventorySyncService> logger)
    {
        _settings = settings.Value;
        _vehicleRepository = vehicleRepository;
        _vectorSearch = vectorSearch;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("VehiclesApi");
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<InventorySyncResult> SyncVehiclesAsync(Guid configurationId, CancellationToken ct = default)
    {
        var result = new InventorySyncResult { ChatbotConfigurationId = configurationId };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting inventory sync for configuration {ConfigId}", configurationId);

            // ── PAGINATED FETCH: Handles dealers with >1000 vehicles ──
            var allVehicles = new List<VehicleApiDto>();
            int page = 1;
            const int pageSize = 200;
            bool hasMore = true;

            while (hasMore)
            {
                var response = await _httpClient.GetAsync(
                    $"{_settings.VehiclesApiUrl}?page={page}&pageSize={pageSize}&status=Active", ct);
                if (!response.IsSuccessStatusCode)
                {
                    result.Success = false;
                    result.ErrorMessage = $"API returned {response.StatusCode} on page {page}";
                    return result;
                }

                var vehiclesData = await response.Content.ReadFromJsonAsync<VehiclesApiResponse>(ct);
                if (vehiclesData?.Items == null || vehiclesData.Items.Count == 0)
                {
                    hasMore = false;
                }
                else
                {
                    allVehicles.AddRange(vehiclesData.Items);
                    hasMore = vehiclesData.Items.Count == pageSize; // More pages if full page returned
                    page++;
                }

                // Safety limit to prevent infinite loops
                if (page > 100)
                {
                    _logger.LogWarning("InventorySync hit page limit (100) for config {ConfigId}", configurationId);
                    break;
                }
            }

            if (allVehicles.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No vehicles data received";
                return result;
            }

            _logger.LogInformation("Fetched {Count} vehicles across {Pages} pages", allVehicles.Count, page - 1);

            var existingVehicles = (await _vehicleRepository.GetByConfigurationIdAsync(configurationId, ct)).ToDictionary(v => v.VehicleId);
            var processedIds = new HashSet<Guid>();
            var changedVehicles = new List<ChatbotVehicle>(); // Track vehicles needing embedding refresh

            foreach (var vehicle in allVehicles)
            {
                processedIds.Add(vehicle.Id);
                var chatbotVehicle = MapToChatbotVehicle(vehicle, configurationId);

                if (existingVehicles.TryGetValue(vehicle.Id, out var existing))
                {
                    chatbotVehicle.Id = existing.Id;
                    chatbotVehicle.ViewCount = existing.ViewCount;
                    chatbotVehicle.InquiryCount = existing.InquiryCount;
                    await _vehicleRepository.UpdateAsync(chatbotVehicle, ct);
                    result.VehiclesUpdated++;

                    // Track vehicles with price/description changes for embedding refresh
                    if (existing.Price != chatbotVehicle.Price ||
                        existing.Description != chatbotVehicle.Description ||
                        existing.Make != chatbotVehicle.Make ||
                        existing.Model != chatbotVehicle.Model)
                    {
                        changedVehicles.Add(chatbotVehicle);
                    }
                }
                else
                {
                    await _vehicleRepository.CreateAsync(chatbotVehicle, ct);
                    result.NewVehiclesAdded++;
                    changedVehicles.Add(chatbotVehicle); // New vehicles always need embeddings
                }

                result.TotalVehiclesProcessed++;

                if (!result.VehiclesByMake.ContainsKey(vehicle.Make))
                    result.VehiclesByMake[vehicle.Make] = 0;
                result.VehiclesByMake[vehicle.Make]++;

                if (!string.IsNullOrEmpty(vehicle.BodyType))
                {
                    if (!result.VehiclesByBodyType.ContainsKey(vehicle.BodyType))
                        result.VehiclesByBodyType[vehicle.BodyType] = 0;
                    result.VehiclesByBodyType[vehicle.BodyType]++;
                }
            }

            // Mark unavailable vehicles and delete their stale embeddings
            foreach (var existing in existingVehicles.Values)
            {
                if (!processedIds.Contains(existing.VehicleId))
                {
                    existing.IsAvailable = false;
                    existing.LastSyncedAt = DateTime.UtcNow;
                    await _vehicleRepository.UpdateAsync(existing, ct);
                    result.VehiclesRemoved++;

                    // P0 FIX: Delete embedding for sold/removed vehicles to prevent
                    // stale data from appearing in RAG search results
                    try
                    {
                        await _vectorSearch.DeleteVehicleEmbeddingAsync(existing.VehicleId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete embedding for removed vehicle {VehicleId}", existing.VehicleId);
                    }
                }
            }

            // P0 FIX: Refresh embeddings for new and changed vehicles.
            // This ensures RAG search returns results with current prices and descriptions.
            if (changedVehicles.Count > 0)
            {
                try
                {
                    // Determine dealer ID from the first vehicle's config or existing data
                    var dealerId = existingVehicles.Values.FirstOrDefault()?.ChatbotConfigurationId ?? configurationId;

                    var embeddingsRefreshed = 0;
                    foreach (var vehicle in changedVehicles)
                    {
                        try
                        {
                            await _vectorSearch.UpsertVehicleEmbeddingAsync(vehicle, dealerId, ct);
                            embeddingsRefreshed++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to upsert embedding for vehicle {VehicleId}", vehicle.VehicleId);
                        }
                    }

                    _logger.LogInformation(
                        "Embedding refresh: {Refreshed}/{Total} embeddings updated, {Removed} stale embeddings deleted",
                        embeddingsRefreshed, changedVehicles.Count, result.VehiclesRemoved);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Embedding refresh failed during inventory sync");
                    // Non-fatal: inventory sync succeeded even if embedding refresh fails
                }
            }

            sw.Stop();
            result.Success = true;
            result.Duration = sw.Elapsed;

            _logger.LogInformation("Inventory sync completed: {Processed} processed, {Added} added, {Updated} updated, {Removed} removed in {Duration}ms",
                result.TotalVehiclesProcessed, result.NewVehiclesAdded, result.VehiclesUpdated, result.VehiclesRemoved, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = sw.Elapsed;
            _logger.LogError(ex, "Inventory sync failed for configuration {ConfigId}", configurationId);
        }

        return result;
    }

    public async Task<int> GetActiveVehicleCountAsync(Guid configurationId, CancellationToken ct = default)
    {
        var vehicles = await _vehicleRepository.GetByConfigurationIdAsync(configurationId, ct);
        return vehicles.Count();
    }

    public async Task<IEnumerable<ChatbotVehicle>> SearchVehiclesAsync(Guid configurationId, string query, int limit = 5, CancellationToken ct = default)
        => await _vehicleRepository.SearchAsync(configurationId, query, limit, ct);

    public async Task<IEnumerable<ChatbotVehicle>> GetFeaturedVehiclesAsync(Guid configurationId, int limit = 5, CancellationToken ct = default)
        => await _vehicleRepository.GetFeaturedAsync(configurationId, limit, ct);

    public async Task<IEnumerable<ChatbotVehicle>> GetVehiclesByPriceRangeAsync(Guid configurationId, decimal minPrice, decimal maxPrice, int limit = 10, CancellationToken ct = default)
        => await _vehicleRepository.GetByPriceRangeAsync(configurationId, minPrice, maxPrice, limit, ct);

    private static ChatbotVehicle MapToChatbotVehicle(VehicleApiDto dto, Guid configurationId) => new()
    {
        Id = Guid.NewGuid(),
        ChatbotConfigurationId = configurationId,
        VehicleId = dto.Id,
        Make = dto.Make,
        Model = dto.Model,
        Year = dto.Year,
        Price = dto.Price,
        Mileage = dto.Mileage,
        FuelType = dto.FuelType,
        Transmission = dto.Transmission,
        BodyType = dto.BodyType,
        ExteriorColor = dto.ExteriorColor,
        InteriorColor = dto.InteriorColor,
        Description = dto.Description,
        ImageUrl = dto.Images?.FirstOrDefault(),
        VehicleUrl = $"/vehicles/{dto.Slug ?? dto.Id.ToString()}",
        IsFeatured = dto.IsFeatured,
        IsAvailable = true,
        LastSyncedAt = DateTime.UtcNow
    };
}

internal class VehiclesApiResponse
{
    public List<VehicleApiDto>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

internal class VehicleApiDto
{
    public Guid Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? BodyType { get; set; }
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }
    public string? Description { get; set; }
    public List<string>? Images { get; set; }
    public string? Slug { get; set; }
    public bool IsFeatured { get; set; }
}
