using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces.Repositories;

public interface ISavedSearchRepository
{
    Task<SavedSearch?> GetByIdAsync(Guid id);
    Task<SavedSearch?> GetByIdAndUserAsync(Guid id, Guid userId);
    Task<IEnumerable<SavedSearch>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<int> GetCountByUserIdAsync(Guid userId);
    Task<IEnumerable<SavedSearch>> GetActiveSearchesAsync();
    Task AddAsync(SavedSearch savedSearch);
    Task UpdateAsync(SavedSearch savedSearch);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id, Guid userId);
}
