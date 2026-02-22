using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Infrastructure.Persistence;

namespace KYCService.Infrastructure.Repositories;

/// <summary>
/// Repositorio para borradores de perfil KYC (autosave del wizard frontend).
/// Un usuario solo puede tener un borrador activo a la vez (índice único en user_id).
/// </summary>
public class KYCProfileDraftRepository : IKYCProfileDraftRepository
{
    private readonly KYCDbContext _context;
    private readonly ILogger<KYCProfileDraftRepository> _logger;

    public KYCProfileDraftRepository(KYCDbContext context, ILogger<KYCProfileDraftRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<KYCProfileDraft?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfileDrafts
            .AsNoTracking()
            .Where(d => d.UserId == userId && !d.IsSubmitted && d.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<KYCProfileDraft> UpsertAsync(KYCProfileDraft draft, CancellationToken cancellationToken = default)
    {
        var existing = await _context.KYCProfileDrafts
            .Where(d => d.UserId == draft.UserId && !d.IsSubmitted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.CurrentStep = draft.CurrentStep;
            existing.FormData = draft.FormData;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.ExpiresAt = DateTime.UtcNow.AddDays(30);

            _context.KYCProfileDrafts.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Updated KYC draft for user {UserId}, step {Step}", draft.UserId, draft.CurrentStep);
            return existing;
        }

        draft.Id = Guid.NewGuid();
        draft.CreatedAt = DateTime.UtcNow;
        draft.UpdatedAt = DateTime.UtcNow;
        draft.ExpiresAt = DateTime.UtcNow.AddDays(30);

        _context.KYCProfileDrafts.Add(draft);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new KYC draft for user {UserId}", draft.UserId);
        return draft;
    }

    public async Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await _context.KYCProfileDrafts
            .Where(d => d.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (count > 0)
            _logger.LogInformation("Deleted KYC draft for user {UserId}", userId);

        return count > 0;
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.KYCProfileDrafts
            .Where(d => d.ExpiresAt <= DateTime.UtcNow || d.IsSubmitted)
            .ExecuteDeleteAsync(cancellationToken);

        if (count > 0)
            _logger.LogInformation("Cleaned up {Count} expired/submitted KYC drafts", count);

        return count;
    }
}
