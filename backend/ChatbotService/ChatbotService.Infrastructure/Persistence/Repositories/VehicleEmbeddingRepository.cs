using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de embeddings usando pgvector (raw SQL).
/// No usa EF Core para esta tabla porque pgvector requiere 
/// operadores especiales (<=> cosine distance) no soportados por EF.
/// </summary>
public class VehicleEmbeddingRepository : IVehicleEmbeddingRepository
{
    private readonly string _connectionString;
    private readonly ILogger<VehicleEmbeddingRepository> _logger;

    public VehicleEmbeddingRepository(
        string connectionString,
        ILogger<VehicleEmbeddingRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Inicializa la tabla y extensión pgvector (idempotente)
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            CREATE EXTENSION IF NOT EXISTS vector;

            CREATE TABLE IF NOT EXISTS vehicle_embeddings (
                id UUID PRIMARY KEY,
                vehicle_id UUID NOT NULL,
                dealer_id UUID NOT NULL,
                content TEXT NOT NULL,
                embedding vector(384),
                metadata JSONB NOT NULL DEFAULT '{}',
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_ve_dealer_id 
                ON vehicle_embeddings(dealer_id);
            
            CREATE INDEX IF NOT EXISTS idx_ve_vehicle_id 
                ON vehicle_embeddings(vehicle_id);

            CREATE UNIQUE INDEX IF NOT EXISTS idx_ve_dealer_vehicle 
                ON vehicle_embeddings(dealer_id, vehicle_id);
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);

        // Intentar crear índice IVFFlat (requiere datos existentes para listas óptimas)
        try
        {
            var countSql = "SELECT COUNT(*) FROM vehicle_embeddings";
            await using var countCmd = new NpgsqlCommand(countSql, conn);
            var count = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0);
            
            if (count >= 100) // IVFFlat necesita al menos algunos registros
            {
                var lists = Math.Max(1, (int)Math.Sqrt(count));
                var ivfSql = $@"
                    DROP INDEX IF EXISTS idx_ve_embedding_ivfflat;
                    CREATE INDEX idx_ve_embedding_ivfflat 
                        ON vehicle_embeddings 
                        USING ivfflat (embedding vector_cosine_ops) 
                        WITH (lists = {lists});";
                
                await using var ivfCmd = new NpgsqlCommand(ivfSql, conn);
                await ivfCmd.ExecuteNonQueryAsync(ct);
                
                _logger.LogInformation("Created IVFFlat index with {Lists} lists on {Count} embeddings",
                    lists, count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create IVFFlat index (will use sequential scan)");
        }

        _logger.LogInformation("Vehicle embeddings table initialized");
    }

    public async Task<List<VehicleSearchResult>> HybridSearchAsync(
        Guid dealerId,
        float[] queryEmbedding,
        VehicleSearchFilters? filters = null,
        int topK = 5,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Construir query SQL con filtros dinámicos
        var conditions = new List<string> { "dealer_id = @dealerId" };
        var parameters = new List<NpgsqlParameter>
        {
            new("dealerId", dealerId),
            new("topK", topK)
        };

        // Agregar el embedding como parámetro
        var embeddingParam = new NpgsqlParameter("queryEmbedding", NpgsqlDbType.Unknown)
        {
            Value = $"[{string.Join(",", queryEmbedding)}]"
        };
        parameters.Add(embeddingParam);

        if (filters != null)
        {
            if (!string.IsNullOrEmpty(filters.Make))
            {
                conditions.Add("LOWER(metadata->>'Make') = LOWER(@make)");
                parameters.Add(new("make", filters.Make));
            }
            if (!string.IsNullOrEmpty(filters.Model))
            {
                // SECURITY: Escape LIKE wildcards (%, _, \) to prevent injection
                var escapedModel = filters.Model
                    .Replace("\\", "\\\\")
                    .Replace("%", "\\%")
                    .Replace("_", "\\_");
                conditions.Add("LOWER(metadata->>'Model') LIKE LOWER(@model) ESCAPE '\\'");
                parameters.Add(new("model", $"%{escapedModel}%"));
            }
            if (filters.YearMin.HasValue)
            {
                conditions.Add("(metadata->>'Year')::int >= @yearMin");
                parameters.Add(new("yearMin", filters.YearMin.Value));
            }
            if (filters.YearMax.HasValue)
            {
                conditions.Add("(metadata->>'Year')::int <= @yearMax");
                parameters.Add(new("yearMax", filters.YearMax.Value));
            }
            if (filters.PriceMin.HasValue)
            {
                conditions.Add("(metadata->>'Price')::decimal >= @priceMin");
                parameters.Add(new("priceMin", filters.PriceMin.Value));
            }
            if (filters.PriceMax.HasValue)
            {
                conditions.Add("(metadata->>'Price')::decimal <= @priceMax");
                parameters.Add(new("priceMax", filters.PriceMax.Value));
            }
            if (!string.IsNullOrEmpty(filters.FuelType))
            {
                conditions.Add("LOWER(metadata->>'FuelType') = LOWER(@fuelType)");
                parameters.Add(new("fuelType", filters.FuelType));
            }
            if (!string.IsNullOrEmpty(filters.Transmission))
            {
                conditions.Add("LOWER(metadata->>'Transmission') = LOWER(@transmission)");
                parameters.Add(new("transmission", filters.Transmission));
            }
            if (!string.IsNullOrEmpty(filters.BodyType))
            {
                conditions.Add("LOWER(metadata->>'BodyType') = LOWER(@bodyType)");
                parameters.Add(new("bodyType", filters.BodyType));
            }
            if (filters.MaxMileage.HasValue)
            {
                conditions.Add("(metadata->>'Mileage')::int <= @maxMileage");
                parameters.Add(new("maxMileage", filters.MaxMileage.Value));
            }
        }

        // Solo mostrar vehículos disponibles
        conditions.Add("(metadata->>'IsAvailable')::boolean = true");

        var whereClause = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT 
                vehicle_id,
                content,
                metadata,
                1 - (embedding <=> @queryEmbedding::vector) AS similarity
            FROM vehicle_embeddings
            WHERE {whereClause}
            ORDER BY embedding <=> @queryEmbedding::vector
            LIMIT @topK";

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
            cmd.Parameters.Add(param);

        var results = new List<VehicleSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            var vehicleId = reader.GetGuid(0);
            var metadataJson = reader.GetString(2);
            var similarity = reader.GetFloat(3);
            
            var metadata = JsonSerializer.Deserialize<VehicleEmbeddingMetadata>(metadataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (metadata != null)
            {
                results.Add(new VehicleSearchResult
                {
                    VehicleId = vehicleId,
                    Make = metadata.Make,
                    Model = metadata.Model,
                    Year = metadata.Year,
                    Price = metadata.Price,
                    FuelType = metadata.FuelType,
                    Transmission = metadata.Transmission,
                    Mileage = metadata.Mileage,
                    ExteriorColor = metadata.ExteriorColor,
                    Condition = metadata.Condition,
                    SimilarityScore = similarity
                });
            }
        }

        return results;
    }

    public async Task UpsertAsync(VehicleEmbedding embedding, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var metadataJson = JsonSerializer.Serialize(embedding.Metadata);
        var embeddingStr = $"[{string.Join(",", embedding.Embedding)}]";

        var sql = @"
            INSERT INTO vehicle_embeddings (id, vehicle_id, dealer_id, content, embedding, metadata, created_at, updated_at)
            VALUES (@id, @vehicleId, @dealerId, @content, @embedding::vector, @metadata::jsonb, @createdAt, @updatedAt)
            ON CONFLICT (dealer_id, vehicle_id)
            DO UPDATE SET 
                content = EXCLUDED.content,
                embedding = EXCLUDED.embedding,
                metadata = EXCLUDED.metadata,
                updated_at = EXCLUDED.updated_at";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", embedding.Id);
        cmd.Parameters.AddWithValue("vehicleId", embedding.VehicleId);
        cmd.Parameters.AddWithValue("dealerId", embedding.DealerId);
        cmd.Parameters.AddWithValue("content", embedding.Content);
        cmd.Parameters.Add(new NpgsqlParameter("embedding", NpgsqlDbType.Unknown) { Value = embeddingStr });
        cmd.Parameters.Add(new NpgsqlParameter("metadata", NpgsqlDbType.Jsonb) { Value = metadataJson });
        cmd.Parameters.AddWithValue("createdAt", embedding.CreatedAt);
        cmd.Parameters.AddWithValue("updatedAt", embedding.UpdatedAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task BulkUpsertAsync(
        Guid dealerId, List<VehicleEmbedding> embeddings, CancellationToken ct = default)
    {
        if (!embeddings.Any()) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Eliminar embeddings existentes del dealer y reinsertar
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        try
        {
            var deleteSql = "DELETE FROM vehicle_embeddings WHERE dealer_id = @dealerId";
            await using var deleteCmd = new NpgsqlCommand(deleteSql, conn, tx);
            deleteCmd.Parameters.AddWithValue("dealerId", dealerId);
            await deleteCmd.ExecuteNonQueryAsync(ct);

            foreach (var embedding in embeddings)
            {
                var metadataJson = JsonSerializer.Serialize(embedding.Metadata);
                var embeddingStr = $"[{string.Join(",", embedding.Embedding)}]";

                var insertSql = @"
                    INSERT INTO vehicle_embeddings (id, vehicle_id, dealer_id, content, embedding, metadata, created_at, updated_at)
                    VALUES (@id, @vehicleId, @dealerId, @content, @embedding::vector, @metadata::jsonb, @createdAt, @updatedAt)";

                await using var insertCmd = new NpgsqlCommand(insertSql, conn, tx);
                insertCmd.Parameters.AddWithValue("id", embedding.Id);
                insertCmd.Parameters.AddWithValue("vehicleId", embedding.VehicleId);
                insertCmd.Parameters.AddWithValue("dealerId", embedding.DealerId);
                insertCmd.Parameters.AddWithValue("content", embedding.Content);
                insertCmd.Parameters.Add(new NpgsqlParameter("embedding", NpgsqlDbType.Unknown) { Value = embeddingStr });
                insertCmd.Parameters.Add(new NpgsqlParameter("metadata", NpgsqlDbType.Jsonb) { Value = metadataJson });
                insertCmd.Parameters.AddWithValue("createdAt", embedding.CreatedAt);
                insertCmd.Parameters.AddWithValue("updatedAt", embedding.UpdatedAt);

                await insertCmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            _logger.LogInformation("Bulk upserted {Count} embeddings for dealer {DealerId}",
                embeddings.Count, dealerId);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = "DELETE FROM vehicle_embeddings WHERE vehicle_id = @vehicleId";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("vehicleId", vehicleId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteByDealerIdAsync(Guid dealerId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = "DELETE FROM vehicle_embeddings WHERE dealer_id = @dealerId";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("dealerId", dealerId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> GetCountByDealerIdAsync(Guid dealerId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = "SELECT COUNT(*) FROM vehicle_embeddings WHERE dealer_id = @dealerId";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("dealerId", dealerId);
        
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }
}
