using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Persistence;

public class EfPriceAlertRepository : IPriceAlertRepository
{
    private readonly ApplicationDbContext _context;

    public EfPriceAlertRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PriceAlert?> GetByIdAsync(Guid id)
        => await _context.PriceAlerts.FindAsync(id);

    public async Task<PriceAlert?> GetByIdAndUserAsync(Guid id, Guid userId)
        => await _context.PriceAlerts
            .FirstOrDefaultAsync(pa => pa.Id == id && pa.UserId == userId);

    public async Task<IEnumerable<PriceAlert>> GetByUserIdAsync(
        Guid userId, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        var query = _context.PriceAlerts.AsNoTracking().Where(pa => pa.UserId == userId);

        if (isActive.HasValue)
            query = query.Where(pa => pa.IsActive == isActive.Value);

        return await query
            .OrderByDescending(pa => pa.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, bool? isActive = null)
    {
        var query = _context.PriceAlerts.AsNoTracking().Where(pa => pa.UserId == userId);

        if (isActive.HasValue)
            query = query.Where(pa => pa.IsActive == isActive.Value);

        return await query.CountAsync();
    }

    public async Task<IEnumerable<PriceAlert>> GetActiveAlertsByVehicleIdAsync(Guid vehicleId)
        => await _context.PriceAlerts
            .AsNoTracking()
            .Where(pa => pa.VehicleId == vehicleId && pa.IsActive)
            .Take(500) // Safety limit
            .ToListAsync();

    public async Task AddAsync(PriceAlert priceAlert)
    {
        await _context.PriceAlerts.AddAsync(priceAlert);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PriceAlert priceAlert)
    {
        _context.PriceAlerts.Update(priceAlert);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.PriceAlerts.FindAsync(id);
        if (entity != null)
        {
            _context.PriceAlerts.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
        => await _context.PriceAlerts
            .AnyAsync(pa => pa.Id == id && pa.UserId == userId);
}
