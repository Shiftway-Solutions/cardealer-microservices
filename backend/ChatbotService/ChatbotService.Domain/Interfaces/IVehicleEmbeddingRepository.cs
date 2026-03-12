using ChatbotService.Domain.Models;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Repositorio para embeddings de vehículos (pgvector).
/// Implementa búsqueda híbrida: similitud coseno + filtros SQL.
/// </summary>
public interface IVehicleEmbeddingRepository
{
    /// <summary>
    /// Búsqueda híbrida: cosine similarity + filtros SQL
    /// </summary>
    Task<List<VehicleSearchResult>> HybridSearchAsync(
        Guid dealerId,
        float[] queryEmbedding,
        VehicleSearchFilters? filters = null,
        int topK = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Inserta o actualiza un embedding de vehículo
    /// </summary>
    Task UpsertAsync(VehicleEmbedding embedding, CancellationToken ct = default);

    /// <summary>
    /// Inserta o actualiza embeddings en bulk para un dealer
    /// </summary>
    Task BulkUpsertAsync(Guid dealerId, List<VehicleEmbedding> embeddings, CancellationToken ct = default);

    /// <summary>
    /// Elimina embedding por vehicleId
    /// </summary>
    Task DeleteByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default);

    /// <summary>
    /// Elimina todos los embeddings de un dealer
    /// </summary>
    Task DeleteByDealerIdAsync(Guid dealerId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el conteo de embeddings para un dealer
    /// </summary>
    Task<int> GetCountByDealerIdAsync(Guid dealerId, CancellationToken ct = default);
}
