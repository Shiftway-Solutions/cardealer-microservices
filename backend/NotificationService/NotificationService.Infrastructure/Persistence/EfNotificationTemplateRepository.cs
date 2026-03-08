using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Infrastructure.Persistence;

public class EfNotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public EfNotificationTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.NotificationTemplates.FindAsync(id);
    }

    public async Task<NotificationTemplate?> GetByNameAsync(string name)
    {
        return await _context.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == name && t.IsActive);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByTypeAsync(NotificationType type)
    {
        return await _context.NotificationTemplates
            .AsNoTracking()
            .Where(t => t.Type == type && t.IsActive)
            .Take(200) // Safety limit
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationTemplate>> GetActiveTemplatesAsync()
    {
        return await _context.NotificationTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Take(500) // Safety limit
            .ToListAsync();
    }

    public async Task AddAsync(NotificationTemplate template)
    {
        await _context.NotificationTemplates.AddAsync(template);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(NotificationTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _context.NotificationTemplates.Update(template);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await GetByIdAsync(id);
        if (template != null)
        {
            _context.NotificationTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _context.NotificationTemplates
            .AsNoTracking()
            .AnyAsync(t => t.Name == name);
    }
}