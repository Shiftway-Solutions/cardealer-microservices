using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TwoFactorAuth?> GetTwoFactorAuthAsync(string userId);
    Task AddOrUpdateTwoFactorAuthAsync(TwoFactorAuth twoFactorAuth);
    Task RemoveTwoFactorAuthAsync(string userId);
    Task<ApplicationUser?> GetByExternalIdAsync(ExternalAuthProvider provider, string externalUserId);
    Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default);

    // Security methods for password management
    Task<bool> VerifyPasswordAsync(ApplicationUser user, string password);
    Task ChangePasswordAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken = default);
}
