using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities.Privacy;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Persistence;

/// <summary>
/// Implementación del repositorio de solicitudes de privacidad
/// </summary>
public class PrivacyRequestRepository : IPrivacyRequestRepository
{
    private readonly ApplicationDbContext _context;

    public PrivacyRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PrivacyRequest?> GetByIdAsync(Guid id)
    {
        return await _context.PrivacyRequests
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PrivacyRequest?> GetPendingDeletionRequestAsync(Guid userId)
    {
        return await _context.PrivacyRequests
            .FirstOrDefaultAsync(pr =>
                pr.UserId == userId &&
                pr.Type == PrivacyRequestType.Cancellation &&
                pr.Status == PrivacyRequestStatus.Pending);
    }

    public async Task<IEnumerable<PrivacyRequest>> GetByUserIdAsync(Guid userId)
    {
        return await _context.PrivacyRequests
            .Where(pr => pr.UserId == userId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<PrivacyRequest> AddAsync(PrivacyRequest request)
    {
        await _context.PrivacyRequests.AddAsync(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task UpdateAsync(PrivacyRequest request)
    {
        _context.PrivacyRequests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasPendingRequestAsync(Guid userId, PrivacyRequestType type)
    {
        return await _context.PrivacyRequests
            .AnyAsync(pr =>
                pr.UserId == userId &&
                pr.Type == type &&
                pr.Status == PrivacyRequestStatus.Pending);
    }

    public async Task<IEnumerable<PrivacyRequest>> GetExpiredGracePeriodRequestsAsync()
    {
        return await _context.PrivacyRequests
            .Where(pr =>
                pr.Type == PrivacyRequestType.Cancellation &&
                pr.Status == PrivacyRequestStatus.Processing &&
                pr.IsConfirmed &&
                pr.GracePeriodEndsAt <= DateTime.UtcNow)
            .Include(pr => pr.User)
            .ToListAsync();
    }

    public async Task<PrivacyRequest?> GetByConfirmationCodeAsync(Guid userId, string code)
    {
        return await _context.PrivacyRequests
            .FirstOrDefaultAsync(pr =>
                pr.UserId == userId &&
                pr.ConfirmationCode == code &&
                pr.Type == PrivacyRequestType.Cancellation &&
                pr.Status == PrivacyRequestStatus.Pending &&
                (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow));
    }

    // ── Ley 172-13 Art. 5: Derecho de acceso y portabilidad ──────────────

    public async Task<PrivacyRequest?> GetLatestExportRequestAsync(Guid userId)
    {
        return await _context.PrivacyRequests
            .Where(pr => pr.UserId == userId &&
                         (pr.Type == PrivacyRequestType.Portability || pr.Type == PrivacyRequestType.Access))
            .OrderByDescending(pr => pr.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<PrivacyRequest?> GetByDownloadTokenAsync(string downloadToken)
    {
        return await _context.PrivacyRequests
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr =>
                pr.DownloadToken == downloadToken &&
                pr.Status == PrivacyRequestStatus.Completed &&
                pr.DownloadTokenExpiresAt > DateTime.UtcNow);
    }

    public async Task<IEnumerable<PrivacyRequest>> GetPendingExportRequestsAsync()
    {
        return await _context.PrivacyRequests
            .Where(pr =>
                (pr.Type == PrivacyRequestType.Portability || pr.Type == PrivacyRequestType.Access) &&
                pr.Status == PrivacyRequestStatus.Pending)
            .OrderBy(pr => pr.CreatedAt)
            .Include(pr => pr.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<PrivacyRequest>> GetExpiredExportRequestsAsync()
    {
        return await _context.PrivacyRequests
            .Where(pr =>
                (pr.Type == PrivacyRequestType.Portability || pr.Type == PrivacyRequestType.Access) &&
                pr.Status == PrivacyRequestStatus.Completed &&
                pr.DownloadTokenExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
    }
}
