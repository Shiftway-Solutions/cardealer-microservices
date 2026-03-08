using VehiclesSaleService.Domain.Entities;

namespace VehiclesSaleService.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task<Favorite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Favorite?> GetByUserAndVehicleAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Favorite>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> GetFavoriteVehiclesByUserIdAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCountByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<bool> IsFavoriteAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken = default);
    Task<Favorite> CreateAsync(Favorite favorite, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByUserAndVehicleAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken = default);
}
