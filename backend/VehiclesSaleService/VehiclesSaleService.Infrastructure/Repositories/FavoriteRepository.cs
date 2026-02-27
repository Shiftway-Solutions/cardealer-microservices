using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Persistence;

namespace VehiclesSaleService.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly ApplicationDbContext _context;

    public FavoriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Favorite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Include(f => f.Vehicle)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Favorite?> GetByUserAndVehicleAsync(
        Guid userId, 
        Guid vehicleId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(
                f => f.UserId == userId && f.VehicleId == vehicleId, 
                cancellationToken);
    }

    public async Task<IEnumerable<Favorite>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Vehicle)
                .ThenInclude(v => v!.Images)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetFavoriteVehiclesByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Vehicle)
                .ThenInclude(v => v!.Images)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Vehicle!)
            .Where(v => v.Status == VehicleStatus.Active) // Solo vehículos activos
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .CountAsync(f => f.UserId == userId, cancellationToken);
    }

    public async Task<int> GetCountByVehicleIdAsync(
        Guid vehicleId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .CountAsync(f => f.VehicleId == vehicleId, cancellationToken);
    }

    public async Task<bool> IsFavoriteAsync(
        Guid userId, 
        Guid vehicleId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .AnyAsync(
                f => f.UserId == userId && f.VehicleId == vehicleId, 
                cancellationToken);
    }

    public async Task<Favorite> CreateAsync(
        Favorite favorite, 
        CancellationToken cancellationToken = default)
    {
        await _context.Favorites.AddAsync(favorite, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var favorite = await _context.Favorites.FindAsync(new object[] { id }, cancellationToken);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByUserAndVehicleAsync(
        Guid userId, 
        Guid vehicleId, 
        CancellationToken cancellationToken = default)
    {
        var favorite = await GetByUserAndVehicleAsync(userId, vehicleId, cancellationToken);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
