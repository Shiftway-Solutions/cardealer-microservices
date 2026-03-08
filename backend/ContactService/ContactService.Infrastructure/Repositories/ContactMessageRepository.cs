using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Infrastructure.Repositories;

public class ContactMessageRepository : IContactMessageRepository
{
    private readonly ApplicationDbContext _context;

    public ContactMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContactMessages
            .AsNoTracking()
            .Include(cm => cm.ContactRequest)
            .FirstOrDefaultAsync(cm => cm.Id == id, cancellationToken);
    }

    public async Task<List<ContactMessage>> GetByContactRequestIdAsync(Guid contactRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.ContactMessages
            .AsNoTracking()
            .Where(cm => cm.ContactRequestId == contactRequestId)
            .OrderBy(cm => cm.SentAt)
            .Take(500) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<ContactMessage> CreateAsync(ContactMessage message, CancellationToken cancellationToken = default)
    {
        _context.ContactMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task MarkAsReadAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.ContactMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUnreadCountForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Performance: Removed Include — EF generates proper JOIN for count without materializing entities
        return await _context.ContactMessages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(cm => !cm.IsRead)
            .Where(cm => (cm.IsFromBuyer && cm.ContactRequest!.SellerId == userId) ||
                        (!cm.IsFromBuyer && cm.ContactRequest!.BuyerId == userId))
            .CountAsync(cancellationToken);
    }
}