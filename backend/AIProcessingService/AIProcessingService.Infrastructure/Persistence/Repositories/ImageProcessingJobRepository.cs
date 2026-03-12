using Microsoft.EntityFrameworkCore;
using AIProcessingService.Domain.Entities;
using AIProcessingService.Domain.Interfaces;

namespace AIProcessingService.Infrastructure.Persistence.Repositories;

public class ImageProcessingJobRepository : IImageProcessingJobRepository
{
    private readonly AIProcessingDbContext _context;

    public ImageProcessingJobRepository(AIProcessingDbContext context)
    {
        _context = context;
    }

    public async Task<ImageProcessingJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task<ImageProcessingJob> CreateAsync(ImageProcessingJob job, CancellationToken ct = default)
    {
        _context.ImageProcessingJobs.Add(job);
        await _context.SaveChangesAsync(ct);
        return job;
    }

    public async Task UpdateAsync(ImageProcessingJob job, CancellationToken ct = default)
    {
        _context.ImageProcessingJobs.Update(job);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var job = await GetByIdAsync(id, ct);
        if (job != null)
        {
            _context.ImageProcessingJobs.Remove(job);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<List<ImageProcessingJob>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .Where(j => j.VehicleId == vehicleId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<ImageProcessingJob>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<ImageProcessingJob>> GetByStatusAsync(JobStatus status, int limit = 100, CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .Where(j => j.Status == status)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<ImageProcessingJob>> GetPendingJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .Where(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<ImageProcessingJob>> GetFailedJobsForRetryAsync(int limit = 20, CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .Where(j => j.Status == JobStatus.Failed && j.RetryCount < j.MaxRetries)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetQueueLengthAsync(CancellationToken ct = default)
    {
        return await _context.ImageProcessingJobs
            .CountAsync(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Queued, ct);
    }

    public async Task<Dictionary<JobStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.ImageProcessingJobs
            .Where(j => j.CreatedAt >= today)
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }

    public async Task<double> GetAverageProcessingTimeAsync(ProcessingType type, int hours = 24, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var avgTime = await _context.ImageProcessingJobs
            .Where(j => j.Type == type &&
                       j.Status == JobStatus.Completed &&
                       j.CompletedAt >= since)
            .AverageAsync(j => (double?)j.ProcessingTimeMs, ct);

        return avgTime ?? 0;
    }
}
