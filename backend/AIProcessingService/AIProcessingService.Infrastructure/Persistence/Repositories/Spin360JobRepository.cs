using Microsoft.EntityFrameworkCore;
using AIProcessingService.Domain.Entities;
using AIProcessingService.Domain.Interfaces;

namespace AIProcessingService.Infrastructure.Persistence.Repositories;

public class Spin360JobRepository : ISpin360JobRepository
{
    private readonly AIProcessingDbContext _context;

    public Spin360JobRepository(AIProcessingDbContext context)
    {
        _context = context;
    }

    public async Task<Spin360Job?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Spin360Jobs
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task<Spin360Job> CreateAsync(Spin360Job job, CancellationToken ct = default)
    {
        _context.Spin360Jobs.Add(job);
        await _context.SaveChangesAsync(ct);
        return job;
    }

    public async Task UpdateAsync(Spin360Job job, CancellationToken ct = default)
    {
        _context.Spin360Jobs.Update(job);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var job = await GetByIdAsync(id, ct);
        if (job != null)
        {
            _context.Spin360Jobs.Remove(job);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<Spin360Job?> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default)
    {
        return await _context.Spin360Jobs
            .Where(j => j.VehicleId == vehicleId)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Spin360Job>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Spin360Jobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Spin360Job>> GetByStatusAsync(Spin360Status status, int limit = 50, CancellationToken ct = default)
    {
        return await _context.Spin360Jobs
            .Where(j => j.Status == status)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<Spin360Job>> GetPendingJobsAsync(int limit = 20, CancellationToken ct = default)
    {
        return await _context.Spin360Jobs
            .Where(j => j.Status == Spin360Status.Pending)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetActiveJobsCountAsync(CancellationToken ct = default)
    {
        return await _context.Spin360Jobs
            .CountAsync(j => j.Status != Spin360Status.Completed &&
                           j.Status != Spin360Status.Failed, ct);
    }
}
