using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
        Task<int> CountAsync();

        // Admin management methods (simplified)
        Task<IEnumerable<User>> GetAllWithFiltersAsync(
            string? search = null,
            string? type = null,
            string? status = null,
            bool? verified = null,
            int page = 1,
            int pageSize = 20);
        Task<int> CountWithFiltersAsync(
            string? search = null,
            string? type = null,
            string? status = null,
            bool? verified = null);
        Task<int> CountActiveAsync();
        Task<int> CountCreatedSinceAsync(DateTime since);
    }
}
