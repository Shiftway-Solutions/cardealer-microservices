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

    public async Task MarkConversationMessagesAsReadAsync(Guid contactRequestId, Guid readerId, bool readerIsBuyer, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: bypass multi-tenant filter so messages with DealerId=Guid.Empty
        // (created pre-tenancy or from cross-tenant buyer) are still found and updated.
        var unreadMessages = await _context.ContactMessages
            .IgnoreQueryFilters()
            .Where(m => m.ContactRequestId == contactRequestId
                     && m.IsFromBuyer != readerIsBuyer  // messages from the other party
                     && !m.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadMessages.Count > 0)
        {
            foreach (var msg in unreadMessages)
                msg.IsRead = true;

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

    public async Task<int> AnonymizeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var messages = await _context.ContactMessages
            .Include(cm => cm.ContactRequest)
            .Where(cm => cm.ContactRequest!.BuyerId == userId && cm.IsFromBuyer)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Message = "[MENSAJE SUPRIMIDO — Ley 172-13]";
        }

        if (messages.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }
}