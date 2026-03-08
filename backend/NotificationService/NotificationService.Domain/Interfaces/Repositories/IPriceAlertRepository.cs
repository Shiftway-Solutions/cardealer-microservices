using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces.Repositories;

public interface IPriceAlertRepository
{
    Task<PriceAlert?> GetByIdAsync(Guid id);
    Task<PriceAlert?> GetByIdAndUserAsync(Guid id, Guid userId);
    Task<IEnumerable<PriceAlert>> GetByUserIdAsync(Guid userId, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<int> GetCountByUserIdAsync(Guid userId, bool? isActive = null);
    Task<IEnumerable<PriceAlert>> GetActiveAlertsByVehicleIdAsync(Guid vehicleId);
    Task AddAsync(PriceAlert priceAlert);
    Task UpdateAsync(PriceAlert priceAlert);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id, Guid userId);
}
