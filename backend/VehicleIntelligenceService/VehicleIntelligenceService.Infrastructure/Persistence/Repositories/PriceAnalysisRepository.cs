using Microsoft.EntityFrameworkCore;
using VehicleIntelligenceService.Domain.Entities;
using VehicleIntelligenceService.Domain.Interfaces;

namespace VehicleIntelligenceService.Infrastructure.Persistence.Repositories;

public class PriceAnalysisRepository : IPriceAnalysisRepository
{
    private readonly VehicleIntelligenceDbContext _context;

    public PriceAnalysisRepository(VehicleIntelligenceDbContext context)
    {
        _context = context;
    }

    public async Task<PriceAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PriceAnalyses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PriceAnalysis?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceAnalyses
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.AnalysisDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PriceAnalysis>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceAnalyses
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.AnalysisDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceAnalysis> CreateAsync(PriceAnalysis analysis, CancellationToken cancellationToken = default)
    {
        analysis.CreatedAt = DateTime.UtcNow;
        analysis.UpdatedAt = DateTime.UtcNow;

        _context.PriceAnalyses.Add(analysis);
        await _context.SaveChangesAsync(cancellationToken);
        return analysis;
    }

    public async Task<PriceAnalysis> UpdateAsync(PriceAnalysis analysis, CancellationToken cancellationToken = default)
    {
        analysis.UpdatedAt = DateTime.UtcNow;

        _context.PriceAnalyses.Update(analysis);
        await _context.SaveChangesAsync(cancellationToken);
        return analysis;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var analysis = await GetByIdAsync(id, cancellationToken);
        if (analysis == null)
            return false;

        _context.PriceAnalyses.Remove(analysis);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
