using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecommendationService.Domain.Entities;
using RecommendationService.Domain.Interfaces;

namespace RecommendationService.Infrastructure.Persistence.Repositories;

public class VehicleInteractionRepository : IVehicleInteractionRepository
{
    private readonly RecommendationDbContext _context;

    public VehicleInteractionRepository(RecommendationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleInteraction?> GetByIdAsync(Guid id)
    {
        return await _context.VehicleInteractions.FindAsync(id);
    }

    public async Task<List<VehicleInteraction>> GetByUserIdAsync(Guid userId, int limit = 100)
    {
        return await _context.VehicleInteractions
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<VehicleInteraction>> GetRecentByUserIdAsync(Guid userId, int days = 30, int limit = 50)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _context.VehicleInteractions
            .Where(i => i.UserId == userId && i.CreatedAt >= cutoffDate)
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<VehicleInteraction>> GetByVehicleIdAsync(Guid vehicleId)
    {
        return await _context.VehicleInteractions
            .Where(i => i.VehicleId == vehicleId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<VehicleInteraction> CreateAsync(VehicleInteraction interaction)
    {
        _context.VehicleInteractions.Add(interaction);
        await _context.SaveChangesAsync();
        return interaction;
    }

    public async Task<int> GetUserInteractionCountAsync(Guid userId, InteractionType type)
    {
        return await _context.VehicleInteractions
            .CountAsync(i => i.UserId == userId && i.Type == type);
    }
}
