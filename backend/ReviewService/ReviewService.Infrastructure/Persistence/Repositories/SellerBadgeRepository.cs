using Microsoft.EntityFrameworkCore;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;
using ReviewService.Infrastructure.Persistence;

namespace ReviewService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de badges de vendedor
/// </summary>
public class SellerBadgeRepository : Repository<SellerBadge, Guid>, ISellerBadgeRepository
{
    public SellerBadgeRepository(ReviewDbContext context) : base(context)
    {
    }

    public async Task<List<SellerBadge>> GetActiveBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerBadges
            .Where(x => x.SellerId == sellerId && x.IsActive)
            .OrderByDescending(x => x.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SellerBadge?> GetBySellerAndTypeAsync(Guid sellerId, BadgeType badgeType, CancellationToken cancellationToken = default)
    {
        return await _context.SellerBadges
            .FirstOrDefaultAsync(x => x.SellerId == sellerId && x.BadgeType == badgeType && x.IsActive, cancellationToken);
    }

    public async Task<SellerBadge> GrantBadgeAsync(Guid sellerId, BadgeType badgeType, string criteria, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
    {
        // Verificar si ya tiene el badge activo
        var existingBadge = await GetBySellerAndTypeAsync(sellerId, badgeType, cancellationToken);
        if (existingBadge != null)
        {
            // Actualizar badge existente
            existingBadge.QualifyingStats = criteria;
            existingBadge.ExpiresAt = expiresAt;
            existingBadge.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return existingBadge;
        }

        // Crear nuevo badge
        var newBadge = new SellerBadge
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            BadgeType = badgeType,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
            QualifyingStats = criteria,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.SellerBadges.AddAsync(newBadge, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return newBadge;
    }

    public async Task<bool> RevokeBadgeAsync(Guid sellerId, BadgeType badgeType, CancellationToken cancellationToken = default)
    {
        var badge = await GetBySellerAndTypeAsync(sellerId, badgeType, cancellationToken);
        if (badge != null)
        {
            badge.IsActive = false;
            badge.RevokedAt = DateTime.UtcNow;
            badge.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<List<SellerBadge>> GetExpiringBadgesAsync(int daysBeforeExpiry = 7, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysBeforeExpiry);

        return await _context.SellerBadges
            .Where(x => x.IsActive && x.ExpiresAt.HasValue && x.ExpiresAt <= expiryThreshold)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<BadgeType, int>> GetBadgeStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _context.SellerBadges
            .Where(x => x.IsActive)
            .GroupBy(x => x.BadgeType)
            .Select(g => new { BadgeType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<BadgeType, int>();

        // Ensure all badge types are represented
        foreach (var badgeType in Enum.GetValues<BadgeType>())
        {
            result[badgeType] = stats.FirstOrDefault(s => s.BadgeType == badgeType)?.Count ?? 0;
        }

        return result;
    }

    public async Task<bool> IsEligibleForBadgeAsync(Guid sellerId, BadgeType badgeType, CancellationToken cancellationToken = default)
    {
        // Esta implementación es básica. La lógica real está en BadgeCalculationService
        // Aquí solo verificamos si ya tiene el badge activo
        return !await _context.SellerBadges
            .AnyAsync(x => x.SellerId == sellerId && x.BadgeType == badgeType && x.IsActive, cancellationToken);
    }
}