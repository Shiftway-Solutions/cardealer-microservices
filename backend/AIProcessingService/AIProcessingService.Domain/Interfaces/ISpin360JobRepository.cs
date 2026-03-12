using AIProcessingService.Domain.Entities;

namespace AIProcessingService.Domain.Interfaces;

public interface ISpin360JobRepository
{
    // CRUD
    Task<Spin360Job?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Spin360Job> CreateAsync(Spin360Job job, CancellationToken ct = default);
    Task UpdateAsync(Spin360Job job, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Queries
    Task<Spin360Job?> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default);
    Task<List<Spin360Job>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Spin360Job>> GetByStatusAsync(Spin360Status status, int limit = 50, CancellationToken ct = default);
    Task<List<Spin360Job>> GetPendingJobsAsync(int limit = 20, CancellationToken ct = default);

    // Stats
    Task<int> GetActiveJobsCountAsync(CancellationToken ct = default);
}
