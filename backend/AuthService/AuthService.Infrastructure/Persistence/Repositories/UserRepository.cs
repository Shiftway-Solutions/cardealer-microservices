using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthService.Shared.Exceptions;
using AuthService.Domain.Enums;


namespace AuthService.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserRepository(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Include TwoFactorAuth to properly evaluate IsTwoFactorEnabled
        return await _context.Users
            .Include(u => u.TwoFactorAuth)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Include TwoFactorAuth to properly evaluate IsTwoFactorEnabled
        return await _context.Users
            .Include(u => u.TwoFactorAuth)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);
    }

    public async Task<ApplicationUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userManager.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new DomainException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new DomainException($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public async Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new DomainException($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    // Métodos adicionales específicos de Identity
    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
    {
        return await _userManager.ResetPasswordAsync(user, token, newPassword);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<IdentityResult> SetLockoutEnabledAsync(ApplicationUser user, bool enabled)
    {
        return await _userManager.SetLockoutEnabledAsync(user, enabled);
    }

    public async Task<IdentityResult> ResetAccessFailedCountAsync(ApplicationUser user)
    {
        return await _userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<IdentityResult> AccessFailedAsync(ApplicationUser user)
    {
        return await _userManager.AccessFailedAsync(user);
    }


    public async Task<TwoFactorAuth?> GetTwoFactorAuthAsync(string userId)
    {
        return await _context.TwoFactorAuths
            .AsNoTracking()
            .FirstOrDefaultAsync(tfa => tfa.UserId == userId);
    }

    public async Task AddOrUpdateTwoFactorAuthAsync(TwoFactorAuth twoFactorAuth)
    {
        var existing = await _context.TwoFactorAuths
            .FirstOrDefaultAsync(tfa => tfa.UserId == twoFactorAuth.UserId);

        if (existing != null)
        {
            // Actualizar existente
            _context.Entry(existing).CurrentValues.SetValues(twoFactorAuth);
            existing.GetType().GetProperty("UpdatedAt")?.SetValue(existing, DateTime.UtcNow);
            _context.TwoFactorAuths.Update(existing);
        }
        else
        {
            // Agregar nuevo
            await _context.TwoFactorAuths.AddAsync(twoFactorAuth);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveTwoFactorAuthAsync(string userId)
    {
        var twoFactorAuth = await GetTwoFactorAuthAsync(userId);
        if (twoFactorAuth != null)
        {
            _context.TwoFactorAuths.Remove(twoFactorAuth);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ApplicationUser?> GetByExternalIdAsync(ExternalAuthProvider provider, string externalUserId)
    {
        return await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalAuthProvider == provider && u.ExternalUserId == externalUserId);
    }

    public async Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Performance: Use AsNoTracking and limit results to prevent loading entire table
        return await _userManager.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(1000) // Safety limit — use paginated queries for production
            .ToListAsync(cancellationToken);
    }

    // Implementación de métodos de seguridad para IUserRepository
    public async Task<bool> VerifyPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task ChangePasswordAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken = default)
    {
        // Generar token de reset para cambiar sin contraseña actual
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            throw new DomainException($"Failed to change password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        user.MarkAsUpdated();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
