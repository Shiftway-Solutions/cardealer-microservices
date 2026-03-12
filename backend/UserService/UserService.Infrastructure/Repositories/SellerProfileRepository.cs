using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de perfiles de vendedores
/// </summary>
public class SellerProfileRepository : ISellerProfileRepository
{
    private readonly ApplicationDbContext _context;

    public SellerProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    #region CRUD Básico

    public async Task<SellerProfile?> GetByIdAsync(Guid id)
    {
        return await _context.SellerProfiles
            .Include(sp => sp.Badges.Where(b => b.IsActive))
            .Include(sp => sp.ContactPreferences)
            .FirstOrDefaultAsync(sp => sp.Id == id && !sp.IsDeleted);
    }

    public async Task<SellerProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _context.SellerProfiles
            .Include(sp => sp.Badges.Where(b => b.IsActive))
            .Include(sp => sp.ContactPreferences)
            .FirstOrDefaultAsync(sp => sp.UserId == userId && !sp.IsDeleted);
    }

    public async Task<SellerProfile> CreateAsync(SellerProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;
        _context.SellerProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    /// <summary>
    /// Alias for CreateAsync - adds a new seller profile
    /// </summary>
    public async Task<SellerProfile> AddAsync(SellerProfile profile)
    {
        return await CreateAsync(profile);
    }

    public async Task<SellerProfile> UpdateAsync(SellerProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        _context.SellerProfiles.Update(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var profile = await _context.SellerProfiles.FindAsync(id);
        if (profile == null) return false;

        profile.IsDeleted = true;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Check if a seller profile exists by ID
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.SellerProfiles.AnyAsync(sp => sp.Id == id && !sp.IsDeleted);
    }

    #endregion

    #region Consultas

    public async Task<List<SellerProfile>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.SellerProfiles
            .Where(sp => !sp.IsDeleted && sp.IsActive)
            .OrderByDescending(sp => sp.TotalSales)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(sp => sp.Badges.Where(b => b.IsActive))
            .ToListAsync();
    }

    public async Task<List<SellerProfile>> GetByVerificationStatusAsync(SellerVerificationStatus status, int page = 1, int pageSize = 20)
    {
        return await _context.SellerProfiles
            .Where(sp => !sp.IsDeleted && sp.VerificationStatus == status)
            .OrderByDescending(sp => sp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(sp => sp.Badges.Where(b => b.IsActive))
            .ToListAsync();
    }

    public async Task<List<SellerProfile>> SearchAsync(string? searchTerm, SellerType? type, string? city, int page = 1, int pageSize = 20)
    {
        var query = _context.SellerProfiles.Where(sp => !sp.IsDeleted && sp.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(sp =>
                (sp.DisplayName != null && sp.DisplayName.ToLower().Contains(term)) ||
                sp.FullName.ToLower().Contains(term) ||
                (sp.BusinessName != null && sp.BusinessName.ToLower().Contains(term)));
        }

        if (type.HasValue)
        {
            query = query.Where(sp => sp.SellerType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(sp => sp.City.ToLower() == city.ToLower());
        }

        return await query
            .OrderByDescending(sp => sp.AverageRating)
            .ThenByDescending(sp => sp.TotalSales)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(sp => sp.Badges.Where(b => b.IsActive))
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm = null, SellerType? type = null, string? city = null)
    {
        var query = _context.SellerProfiles.Where(sp => !sp.IsDeleted && sp.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(sp =>
                (sp.DisplayName != null && sp.DisplayName.ToLower().Contains(term)) ||
                sp.FullName.ToLower().Contains(term) ||
                (sp.BusinessName != null && sp.BusinessName.ToLower().Contains(term)));
        }

        if (type.HasValue)
        {
            query = query.Where(sp => sp.SellerType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(sp => sp.City.ToLower() == city.ToLower());
        }

        return await query.CountAsync();
    }

    #endregion

    #region Contact Preferences

    public async Task<ContactPreferences?> GetContactPreferencesAsync(Guid sellerProfileId)
    {
        return await _context.Set<ContactPreferences>()
            .FirstOrDefaultAsync(cp => cp.SellerProfileId == sellerProfileId);
    }

    public async Task<ContactPreferences> CreateContactPreferencesAsync(ContactPreferences preferences)
    {
        preferences.CreatedAt = DateTime.UtcNow;
        _context.Set<ContactPreferences>().Add(preferences);
        await _context.SaveChangesAsync();
        return preferences;
    }

    public async Task<ContactPreferences> UpdateContactPreferencesAsync(ContactPreferences preferences)
    {
        preferences.UpdatedAt = DateTime.UtcNow;
        _context.Set<ContactPreferences>().Update(preferences);
        await _context.SaveChangesAsync();
        return preferences;
    }

    #endregion

    #region Badges

    public async Task<List<SellerBadgeAssignment>> GetBadgesAsync(Guid sellerProfileId)
    {
        return await _context.Set<SellerBadgeAssignment>()
            .Where(b => b.SellerProfileId == sellerProfileId && b.IsActive)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<SellerBadgeAssignment> AssignBadgeAsync(SellerBadgeAssignment badge)
    {
        // Verificar si ya tiene el badge
        var existing = await _context.Set<SellerBadgeAssignment>()
            .FirstOrDefaultAsync(b => b.SellerProfileId == badge.SellerProfileId &&
                                      b.Badge == badge.Badge &&
                                      b.IsActive);

        if (existing != null)
        {
            // Actualizar fecha de expiración si es necesario
            if (badge.ExpiresAt.HasValue)
            {
                existing.ExpiresAt = badge.ExpiresAt;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        badge.EarnedAt = DateTime.UtcNow;
        _context.Set<SellerBadgeAssignment>().Add(badge);
        await _context.SaveChangesAsync();
        return badge;
    }

    public async Task<bool> RemoveBadgeAsync(Guid sellerProfileId, SellerBadge badge)
    {
        var existing = await _context.Set<SellerBadgeAssignment>()
            .FirstOrDefaultAsync(b => b.SellerProfileId == sellerProfileId &&
                                      b.Badge == badge &&
                                      b.IsActive);

        if (existing == null) return false;

        existing.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasBadgeAsync(Guid sellerProfileId, SellerBadge badge)
    {
        return await _context.Set<SellerBadgeAssignment>()
            .AnyAsync(b => b.SellerProfileId == sellerProfileId &&
                          b.Badge == badge &&
                          b.IsActive &&
                          (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow));
    }

    #endregion

    #region Estadísticas

    public async Task UpdateStatsAsync(Guid sellerProfileId, int totalListings, int activeListings, int totalSales,
        decimal averageRating, int totalReviews, int responseTimeMinutes, int responseRate)
    {
        var profile = await _context.SellerProfiles.FindAsync(sellerProfileId);
        if (profile == null) return;

        profile.TotalListings = totalListings;
        profile.ActiveListings = activeListings;
        profile.TotalSales = totalSales;
        profile.AverageRating = averageRating;
        profile.TotalReviews = totalReviews;
        profile.ResponseTimeMinutes = responseTimeMinutes;
        profile.ResponseRate = responseRate;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateMonthlyStatsAsync(Guid sellerProfileId, int viewsThisMonth, int leadsThisMonth)
    {
        var profile = await _context.SellerProfiles.FindAsync(sellerProfileId);
        if (profile == null) return;

        profile.ViewsThisMonth = viewsThisMonth;
        profile.LeadsThisMonth = leadsThisMonth;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<List<SellerProfile>> GetTopSellersAsync(int count = 10, string? city = null)
    {
        var query = _context.SellerProfiles
            .Where(sp => !sp.IsDeleted && sp.IsActive && sp.VerificationStatus == SellerVerificationStatus.Verified);

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(sp => sp.City.ToLower() == city.ToLower());
        }

        return await query
            .OrderByDescending(sp => sp.TotalSales)
            .ThenByDescending(sp => sp.AverageRating)
            .Take(count)
            .Include(sp => sp.Badges.Where(b => b.IsActive))
            .ToListAsync();
    }

    #endregion

    #region Verificación

    public async Task<bool> VerifyAsync(Guid sellerProfileId, Guid verifiedByUserId, string? notes)
    {
        var profile = await _context.SellerProfiles.FindAsync(sellerProfileId);
        if (profile == null) return false;

        profile.VerificationStatus = SellerVerificationStatus.Verified;
        profile.VerifiedAt = DateTime.UtcNow;
        profile.VerifiedByUserId = verifiedByUserId;
        profile.VerificationNotes = notes;
        profile.VerificationExpiresAt = DateTime.UtcNow.AddYears(1);
        profile.MaxActiveListings = 10; // Incrementar límite
        profile.CanSellHighValue = true;
        profile.UpdatedAt = DateTime.UtcNow;

        // Asignar badge de verificado
        await AssignBadgeAsync(new SellerBadgeAssignment
        {
            SellerProfileId = sellerProfileId,
            Badge = SellerBadge.Verified,
            Reason = "Verificación de identidad completada"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(Guid sellerProfileId, Guid rejectedByUserId, string reason)
    {
        var profile = await _context.SellerProfiles.FindAsync(sellerProfileId);
        if (profile == null) return false;

        profile.VerificationStatus = SellerVerificationStatus.Rejected;
        profile.VerifiedByUserId = rejectedByUserId;
        profile.RejectionReason = reason;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SellerProfile>> GetPendingVerificationsAsync(int page = 1, int pageSize = 20)
    {
        return await _context.SellerProfiles
            .Where(sp => !sp.IsDeleted &&
                        (sp.VerificationStatus == SellerVerificationStatus.PendingReview ||
                         sp.VerificationStatus == SellerVerificationStatus.InReview))
            .OrderBy(sp => sp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(sp => sp.IdentityDocuments)
            .ToListAsync();
    }

    #endregion

    #region Actividad

    public async Task UpdateLastActiveAsync(Guid sellerProfileId)
    {
        var profile = await _context.SellerProfiles.FindAsync(sellerProfileId);
        if (profile == null) return;

        profile.LastActiveAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsActiveAsync(Guid sellerProfileId)
    {
        var profile = await _context.SellerProfiles.FindAsync(sellerProfileId);
        if (profile == null) return false;

        return profile.IsActive &&
               !profile.IsDeleted &&
               profile.LastActiveAt.HasValue &&
               profile.LastActiveAt.Value > DateTime.UtcNow.AddDays(-30);
    }

    #endregion
}
