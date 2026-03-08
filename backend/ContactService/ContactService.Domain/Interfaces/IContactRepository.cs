using ContactService.Domain.Entities;

namespace ContactService.Domain.Interfaces;

public interface IContactRequestRepository
{
    Task<ContactRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ContactRequest>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);
    Task<List<ContactRequest>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<List<ContactRequest>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<ContactRequest> CreateAsync(ContactRequest contactRequest, CancellationToken cancellationToken = default);
    Task<ContactRequest> UpdateAsync(ContactRequest contactRequest, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountForSellerAsync(Guid sellerId, CancellationToken cancellationToken = default);
}

public interface IContactMessageRepository
{
    Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ContactMessage>> GetByContactRequestIdAsync(Guid contactRequestId, CancellationToken cancellationToken = default);
    Task<ContactMessage> CreateAsync(ContactMessage message, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}