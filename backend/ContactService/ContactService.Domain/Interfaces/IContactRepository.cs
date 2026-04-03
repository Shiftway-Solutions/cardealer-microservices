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

    /// <summary>
    /// Counts total contact requests for a seller. Used to detect "first inquiry ever" for celebration notifications.
    /// </summary>
    Task<int> CountBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts contact requests for a seller within a specific date range.
    /// Used by the upsell trigger to detect dealers with 5+ inquiries in 30 days.
    /// </summary>
    Task<int> CountBySellerIdInPeriodAsync(Guid sellerId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymizes all contact requests where the user is the buyer (Ley 172-13 cascade deletion).
    /// Replaces PII fields (BuyerName, BuyerEmail, BuyerPhone, Name, Email, Phone) with "[SUPRIMIDO]".
    /// Returns the number of requests anonymized.
    /// </summary>
    Task<int> AnonymizeByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);
}

public interface IContactMessageRepository
{
    Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ContactMessage>> GetByContactRequestIdAsync(Guid contactRequestId, CancellationToken cancellationToken = default);
    Task<ContactMessage> CreateAsync(ContactMessage message, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-marks all messages in a conversation as read for the given reader.
    /// Marks messages sent by the OTHER party (not the reader) as IsRead = true.
    /// Used when a user opens a conversation to clear the unread badge.
    /// </summary>
    Task MarkConversationMessagesAsReadAsync(Guid contactRequestId, Guid readerId, bool readerIsBuyer, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymizes messages sent by a user (Ley 172-13 cascade deletion).
    /// Replaces Content with "[MENSAJE SUPRIMIDO — Ley 172-13]" for messages where UserId matches.
    /// </summary>
    Task<int> AnonymizeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}