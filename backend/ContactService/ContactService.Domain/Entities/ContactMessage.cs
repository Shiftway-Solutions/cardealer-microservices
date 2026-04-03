using System;
using System.ComponentModel.DataAnnotations;

namespace ContactService.Domain.Entities
{
    using CarDealer.Shared.MultiTenancy;
    public class ContactMessage : ITenantEntity
    {
        public Guid Id { get; set; }
        public Guid DealerId { get; set; } // Multi-tenant

        [Required]
        public Guid ContactRequestId { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsFromBuyer { get; set; }
        public bool IsRead { get; set; } = false;
        public string SenderName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ContactRequest? ContactRequest { get; set; }

        public ContactMessage()
        {
            Id = Guid.NewGuid();
        }

        public ContactMessage(Guid contactRequestId, Guid senderId, string message, bool isFromBuyer, string senderName = "") : this()
        {
            ContactRequestId = contactRequestId;
            SenderId = senderId;
            Message = message;
            IsFromBuyer = isFromBuyer;
            SenderName = senderName;
        }
    }
}
