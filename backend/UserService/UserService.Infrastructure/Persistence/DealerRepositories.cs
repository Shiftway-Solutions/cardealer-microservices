using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Persistence
{
    public class DealerRepository : IDealerRepository
    {
        private readonly ApplicationDbContext _context;

        public DealerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dealer?> GetByIdAsync(Guid id)
        {
            return await _context.Dealers
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Dealer?> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _context.Dealers
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.OwnerUserId == ownerId);
        }

        public async Task<Dealer?> GetBySlugAsync(string slug)
        {
            return await _context.Dealers
                .AsNoTracking()
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Slug == slug && d.IsActive);
        }

        public async Task<IEnumerable<Dealer>> GetForSitemapAsync()
        {
            return await _context.Dealers
                .AsNoTracking()
                .Where(d => d.IsActive && d.Slug != null)
                .Select(d => new Dealer
                {
                    Id = d.Id,
                    Slug = d.Slug,
                    BusinessName = d.BusinessName,
                    UpdatedAt = d.UpdatedAt,
                    CreatedAt = d.CreatedAt
                })
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Dealer>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Dealers
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Dealer>> SearchAsync(
            string? searchTerm,
            string? city,
            string? state,
            DealerType? dealerType,
            bool? isVerified,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Dealers.AsNoTracking().Where(d => d.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(d =>
                    d.BusinessName.Contains(searchTerm) ||
                    (d.Description != null && d.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(d => d.City == city);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(d => d.State == state);
            }

            if (dealerType.HasValue)
            {
                query = query.Where(d => d.DealerType == dealerType.Value);
            }

            if (isVerified.HasValue)
            {
                query = query.Where(d => isVerified.Value
                    ? d.VerificationStatus == DealerVerificationStatus.Verified
                    : d.VerificationStatus != DealerVerificationStatus.Verified);
            }

            return await query
                .OrderByDescending(d => d.AverageRating)
                .ThenByDescending(d => d.TotalReviews)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> SearchCountAsync(
            string? searchTerm,
            string? city,
            string? state,
            DealerType? dealerType,
            bool? isVerified)
        {
            var query = _context.Dealers.AsNoTracking().Where(d => d.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(d =>
                    d.BusinessName.Contains(searchTerm) ||
                    (d.Description != null && d.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(d => d.City == city);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(d => d.State == state);
            }

            if (dealerType.HasValue)
            {
                query = query.Where(d => d.DealerType == dealerType.Value);
            }

            if (isVerified.HasValue)
            {
                query = query.Where(d => isVerified.Value
                    ? d.VerificationStatus == DealerVerificationStatus.Verified
                    : d.VerificationStatus != DealerVerificationStatus.Verified);
            }

            return await query.CountAsync();
        }

        public async Task<Dealer> AddAsync(Dealer dealer)
        {
            await _context.Dealers.AddAsync(dealer);
            await _context.SaveChangesAsync();
            return dealer;
        }

        public async Task UpdateAsync(Dealer dealer)
        {
            dealer.UpdatedAt = DateTime.UtcNow;
            _context.Dealers.Update(dealer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var dealer = await GetByIdAsync(id);
            if (dealer != null)
            {
                dealer.IsActive = false;
                dealer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Dealers.AnyAsync(d => d.Id == id);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Dealers.CountAsync(d => d.IsActive);
        }

        public async Task<int> CountByTypeAsync(DealerType dealerType)
        {
            return await _context.Dealers.CountAsync(d => d.IsActive && d.DealerType == dealerType);
        }
    }

    // NOTE: SellerProfileRepository has been moved to Repositories/SellerProfileRepository.cs (more complete version)
    // The old implementation below is commented out to avoid duplicate class definition

    /*
    public class SellerProfileRepository_DEPRECATED : ISellerProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public SellerProfileRepository_DEPRECATED(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SellerProfile?> GetByIdAsync(Guid id)
        {
            return await _context.SellerProfiles
                .Include(s => s.IdentityDocuments)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SellerProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.SellerProfiles
                .Include(s => s.IdentityDocuments)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<IEnumerable<SellerProfile>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.SellerProfiles
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<SellerProfile>> SearchAsync(
            string? searchTerm,
            string? city,
            string? state,
            bool? isVerified,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.SellerProfiles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s =>
                    s.FullName.Contains(searchTerm) ||
                    (s.Bio != null && s.Bio.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => s.City == city);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(s => s.State == state);
            }

            if (isVerified.HasValue)
            {
                query = query.Where(s => isVerified.Value 
                    ? s.VerificationStatus == SellerVerificationStatus.Verified 
                    : s.VerificationStatus != SellerVerificationStatus.Verified);
            }

            return await query
                .OrderByDescending(s => s.AverageRating)
                .ThenByDescending(s => s.TotalReviews)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<SellerProfile> AddAsync(SellerProfile profile)
        {
            await _context.SellerProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task UpdateAsync(SellerProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            _context.SellerProfiles.Update(profile);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var profile = await GetByIdAsync(id);
            if (profile != null)
            {
                _context.SellerProfiles.Remove(profile);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.SellerProfiles.AnyAsync(s => s.Id == id);
        }

        public async Task<int> CountAsync()
        {
            return await _context.SellerProfiles.CountAsync();
        }
    }
    */

    public class IdentityDocumentRepository : IIdentityDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public IdentityDocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IdentityDocument?> GetByIdAsync(Guid id)
        {
            return await _context.IdentityDocuments.FindAsync(id);
        }

        public async Task<IEnumerable<IdentityDocument>> GetBySellerProfileIdAsync(Guid sellerProfileId)
        {
            return await _context.IdentityDocuments
                .Where(d => d.SellerProfileId == sellerProfileId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<IdentityDocument>> GetPendingVerificationAsync(int page = 1, int pageSize = 10)
        {
            return await _context.IdentityDocuments
                .Where(d => d.Status == DocumentVerificationStatus.Pending)
                .OrderBy(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IdentityDocument> AddAsync(IdentityDocument document)
        {
            await _context.IdentityDocuments.AddAsync(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task UpdateAsync(IdentityDocument document)
        {
            _context.IdentityDocuments.Update(document);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var document = await GetByIdAsync(id);
            if (document != null)
            {
                _context.IdentityDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountPendingAsync()
        {
            return await _context.IdentityDocuments.CountAsync(d => d.Status == DocumentVerificationStatus.Pending);
        }
    }

    // DealerEmployeeRepository implementation is in Repositories/DealerEmployeeRepository.cs
}
