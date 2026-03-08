using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using ContactService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Infrastructure.Repositories;

public class ContactRequestRepository : IContactRequestRepository
{
    private readonly ApplicationDbContext _context;

    public ContactRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContactRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContactRequests
            .IgnoreQueryFilters()
            .Include(cr => cr.Messages)
            .FirstOrDefaultAsync(cr => cr.Id == id, cancellationToken);
    }

    public async Task<List<ContactRequest>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await _context.ContactRequests
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(cr => cr.Messages)
            .Where(cr => cr.BuyerId == buyerId)
            .OrderByDescending(cr => cr.CreatedAt)
            .Take(200) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ContactRequest>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.ContactRequests
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(cr => cr.Messages)
            .Where(cr => cr.SellerId == sellerId)
            .OrderByDescending(cr => cr.CreatedAt)
            .Take(200) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ContactRequest>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _context.ContactRequests
            .AsNoTracking()
            .Include(cr => cr.Messages)
            .Where(cr => cr.VehicleId == vehicleId)
            .OrderByDescending(cr => cr.CreatedAt)
            .Take(200) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<ContactRequest> CreateAsync(ContactRequest contactRequest, CancellationToken cancellationToken = default)
    {
        _context.ContactRequests.Add(contactRequest);
        await _context.SaveChangesAsync(cancellationToken);
        return contactRequest;
    }

    public async Task<ContactRequest> UpdateAsync(ContactRequest contactRequest, CancellationToken cancellationToken = default)
    {
        _context.ContactRequests.Update(contactRequest);
        await _context.SaveChangesAsync(cancellationToken);
        return contactRequest;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contactRequest = await _context.ContactRequests.FindAsync(new object[] { id }, cancellationToken);
        if (contactRequest != null)
        {
            _context.ContactRequests.Remove(contactRequest);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUnreadCountForSellerAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.ContactRequests
            .AsNoTracking()
            .Where(cr => cr.SellerId == sellerId && cr.Status != "Closed")
            .CountAsync(cr => cr.Messages.Any(m => !m.IsRead && m.IsFromBuyer), cancellationToken);
    }
}