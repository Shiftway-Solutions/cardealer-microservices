using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for dealer employees
/// </summary>
public class DealerEmployeeRepository : IDealerEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public DealerEmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DealerEmployee?> GetByIdAsync(Guid id)
    {
        return await _context.DealerEmployees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<DealerEmployee>> GetByDealerIdAsync(Guid dealerId)
    {
        return await _context.DealerEmployees
            .Where(e => e.DealerId == dealerId)
            .Include(e => e.User)
            .OrderByDescending(e => e.InvitationDate)
            .ToListAsync();
    }

    public async Task<DealerEmployee?> GetByUserIdAndDealerIdAsync(Guid userId, Guid dealerId)
    {
        return await _context.DealerEmployees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.DealerId == dealerId);
    }

    public async Task<DealerEmployee> AddAsync(DealerEmployee employee)
    {
        await _context.DealerEmployees.AddAsync(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task UpdateAsync(DealerEmployee employee)
    {
        _context.DealerEmployees.Update(employee);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await _context.DealerEmployees.FindAsync(id);
        if (employee != null)
        {
            _context.DealerEmployees.Remove(employee);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CountByDealerIdAsync(Guid dealerId)
    {
        return await _context.DealerEmployees
            .Where(e => e.DealerId == dealerId)
            .CountAsync();
    }

    // Invitation operations
    public async Task<DealerEmployeeInvitation?> GetInvitationByIdAsync(Guid dealerId, Guid invitationId)
    {
        return await _context.DealerEmployeeInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.DealerId == dealerId);
    }

    public async Task<DealerEmployeeInvitation?> GetInvitationByTokenAsync(string token)
    {
        return await _context.DealerEmployeeInvitations
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<DealerEmployeeInvitation?> GetPendingInvitationByEmailAsync(Guid dealerId, string email)
    {
        return await _context.DealerEmployeeInvitations
            .FirstOrDefaultAsync(i => i.DealerId == dealerId
                && i.Email.ToLower() == email.ToLower()
                && i.Status == InvitationStatus.Pending);
    }

    public async Task<IEnumerable<DealerEmployeeInvitation>> GetPendingInvitationsAsync(Guid dealerId)
    {
        return await _context.DealerEmployeeInvitations
            .Where(i => i.DealerId == dealerId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.InvitationDate)
            .ToListAsync();
    }

    public async Task<DealerEmployeeInvitation> AddInvitationAsync(DealerEmployeeInvitation invitation)
    {
        await _context.DealerEmployeeInvitations.AddAsync(invitation);
        await _context.SaveChangesAsync();
        return invitation;
    }

    public async Task UpdateInvitationAsync(DealerEmployeeInvitation invitation)
    {
        _context.DealerEmployeeInvitations.Update(invitation);
        await _context.SaveChangesAsync();
    }
}
