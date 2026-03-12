using Microsoft.EntityFrameworkCore;
using VehicleIntelligenceService.Domain.Entities;
using VehicleIntelligenceService.Domain.Interfaces;

namespace VehicleIntelligenceService.Infrastructure.Persistence.Repositories;

public class DemandPredictionRepository : IDemandPredictionRepository
{
    private readonly VehicleIntelligenceDbContext _context;

    public DemandPredictionRepository(VehicleIntelligenceDbContext context)
    {
        _context = context;
    }

    public async Task<DemandPrediction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DemandPredictions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<DemandPrediction?> GetLatestByMakeModelYearAsync(
        string make,
        string model,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.DemandPredictions
            .Where(x => x.Make == make && x.Model == model && x.Year == year)
            .OrderByDescending(x => x.PredictionDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DemandPrediction>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.DemandPredictions
            .OrderByDescending(x => x.PredictionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<DemandPrediction> CreateAsync(DemandPrediction prediction, CancellationToken cancellationToken = default)
    {
        prediction.CreatedAt = DateTime.UtcNow;
        prediction.UpdatedAt = DateTime.UtcNow;

        _context.DemandPredictions.Add(prediction);
        await _context.SaveChangesAsync(cancellationToken);
        return prediction;
    }

    public async Task<DemandPrediction> UpdateAsync(DemandPrediction prediction, CancellationToken cancellationToken = default)
    {
        prediction.UpdatedAt = DateTime.UtcNow;

        _context.DemandPredictions.Update(prediction);
        await _context.SaveChangesAsync(cancellationToken);
        return prediction;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var prediction = await GetByIdAsync(id, cancellationToken);
        if (prediction == null)
            return false;

        _context.DemandPredictions.Remove(prediction);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
