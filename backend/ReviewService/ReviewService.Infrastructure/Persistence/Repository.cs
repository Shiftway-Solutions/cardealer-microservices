using Microsoft.EntityFrameworkCore;
using ReviewService.Domain.Base;

namespace ReviewService.Infrastructure.Persistence;

/// <summary>
/// Implementación base del repositorio genérico
/// </summary>
public class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
{
    protected readonly ReviewDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(ReviewDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual Task<TEntity> UpdateAsync(TEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(TId id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(TId id)
    {
        return await _dbSet.FindAsync(id) != null;
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}