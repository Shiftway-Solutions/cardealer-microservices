using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Persistence;

public class EfSavedSearchRepository : ISavedSearchRepository
{
    private readonly ApplicationDbContext _context;

    public EfSavedSearchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SavedSearch?> GetByIdAsync(Guid id)
        => await _context.SavedSearches.FindAsync(id);

    public async Task<SavedSearch?> GetByIdAndUserAsync(Guid id, Guid userId)
        => await _context.SavedSearches
            .FirstOrDefaultAsync(ss => ss.Id == id && ss.UserId == userId);

    public async Task<IEnumerable<SavedSearch>> GetByUserIdAsync(
        Guid userId, int page = 1, int pageSize = 20)
    {
        return await _context.SavedSearches
            .AsNoTracking()
            .Where(ss => ss.UserId == userId)
            .OrderByDescending(ss => ss.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId)
        => await _context.SavedSearches
            .CountAsync(ss => ss.UserId == userId);

    public async Task<IEnumerable<SavedSearch>> GetActiveSearchesAsync()
        => await _context.SavedSearches
            .AsNoTracking()
            .Where(ss => ss.IsActive && ss.NotifyOnNewResults)
            .Take(1000) // Safety limit for background worker
            .ToListAsync();

    public async Task AddAsync(SavedSearch savedSearch)
    {
        await _context.SavedSearches.AddAsync(savedSearch);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SavedSearch savedSearch)
    {
        _context.SavedSearches.Update(savedSearch);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.SavedSearches.FindAsync(id);
        if (entity != null)
        {
            _context.SavedSearches.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
        => await _context.SavedSearches
            .AnyAsync(ss => ss.Id == id && ss.UserId == userId);
}
