using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces;

/// <summary>
/// Repositorio para gestión de perfiles de vendedores
/// </summary>
public interface ISellerProfileRepository
{
    // ========================================
    // CRUD BÁSICO
    // ========================================
    Task<SellerProfile?> GetByIdAsync(Guid id);
    Task<SellerProfile?> GetByUserIdAsync(Guid userId);
    Task<SellerProfile> CreateAsync(SellerProfile profile);
    Task<SellerProfile> AddAsync(SellerProfile profile); // Alias for CreateAsync
    Task<SellerProfile> UpdateAsync(SellerProfile profile);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);

    // ========================================
    // CONSULTAS
    // ========================================
    Task<List<SellerProfile>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<SellerProfile>> GetByVerificationStatusAsync(SellerVerificationStatus status, int page = 1, int pageSize = 20);
    Task<List<SellerProfile>> SearchAsync(string? searchTerm, SellerType? type, string? city, int page = 1, int pageSize = 20);
    Task<int> GetTotalCountAsync(string? searchTerm = null, SellerType? type = null, string? city = null);

    // ========================================
    // CONTACT PREFERENCES
    // ========================================
    Task<ContactPreferences?> GetContactPreferencesAsync(Guid sellerProfileId);
    Task<ContactPreferences> CreateContactPreferencesAsync(ContactPreferences preferences);
    Task<ContactPreferences> UpdateContactPreferencesAsync(ContactPreferences preferences);

    // ========================================
    // BADGES
    // ========================================
    Task<List<SellerBadgeAssignment>> GetBadgesAsync(Guid sellerProfileId);
    Task<SellerBadgeAssignment> AssignBadgeAsync(SellerBadgeAssignment badge);
    Task<bool> RemoveBadgeAsync(Guid sellerProfileId, SellerBadge badge);
    Task<bool> HasBadgeAsync(Guid sellerProfileId, SellerBadge badge);

    // ========================================
    // ESTADÍSTICAS
    // ========================================
    Task UpdateStatsAsync(Guid sellerProfileId, int totalListings, int activeListings, int totalSales,
        decimal averageRating, int totalReviews, int responseTimeMinutes, int responseRate);
    Task UpdateMonthlyStatsAsync(Guid sellerProfileId, int viewsThisMonth, int leadsThisMonth);
    Task<List<SellerProfile>> GetTopSellersAsync(int count = 10, string? city = null);

    // ========================================
    // VERIFICACIÓN
    // ========================================
    Task<bool> VerifyAsync(Guid sellerProfileId, Guid verifiedByUserId, string? notes);
    Task<bool> RejectAsync(Guid sellerProfileId, Guid rejectedByUserId, string reason);
    Task<List<SellerProfile>> GetPendingVerificationsAsync(int page = 1, int pageSize = 20);

    // ========================================
    // ACTIVIDAD
    // ========================================
    Task UpdateLastActiveAsync(Guid sellerProfileId);
    Task<bool> IsActiveAsync(Guid sellerProfileId);
}
