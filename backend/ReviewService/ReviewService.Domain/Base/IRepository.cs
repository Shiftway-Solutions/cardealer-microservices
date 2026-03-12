namespace ReviewService.Domain.Base;

/// <summary>
/// Repositorio base genérico
/// </summary>
public interface IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
{
    Task<TEntity?> GetByIdAsync(TId id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TId id);
    Task<bool> ExistsAsync(TId id);
    Task SaveChangesAsync();
}