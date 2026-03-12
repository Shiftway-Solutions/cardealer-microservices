using AIProcessingService.Domain.Entities;

namespace AIProcessingService.Domain.Interfaces;

public interface IImageProcessingJobRepository
{
    // CRUD
    Task<ImageProcessingJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ImageProcessingJob> CreateAsync(ImageProcessingJob job, CancellationToken ct = default);
    Task UpdateAsync(ImageProcessingJob job, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Queries
    Task<List<ImageProcessingJob>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default);
    Task<List<ImageProcessingJob>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<ImageProcessingJob>> GetByStatusAsync(JobStatus status, int limit = 100, CancellationToken ct = default);
    Task<List<ImageProcessingJob>> GetPendingJobsAsync(int limit = 50, CancellationToken ct = default);
    Task<List<ImageProcessingJob>> GetFailedJobsForRetryAsync(int limit = 20, CancellationToken ct = default);

    // Stats
    Task<int> GetQueueLengthAsync(CancellationToken ct = default);
    Task<Dictionary<JobStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);
    Task<double> GetAverageProcessingTimeAsync(ProcessingType type, int hours = 24, CancellationToken ct = default);
}
