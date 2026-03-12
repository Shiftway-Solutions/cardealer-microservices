using CarDealer.Contracts.Abstractions;

namespace UserService.Domain.Events;

/// <summary>
/// Evento publicado cuando se crea un perfil de vendedor
/// </summary>
public class SellerProfileCreatedEvent : EventBase
{
    public override string EventType => "seller.profile.created";

    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SellerType { get; set; } = string.Empty;

    public static SellerProfileCreatedEvent Create(
        Guid sellerId,
        Guid userId,
        string fullName,
        string email,
        string city,
        string sellerType)
    {
        return new SellerProfileCreatedEvent
        {
            SellerId = sellerId,
            UserId = userId,
            FullName = fullName,
            Email = email,
            City = city,
            SellerType = sellerType
        };
    }
}

/// <summary>
/// Evento publicado cuando se actualiza un perfil de vendedor
/// </summary>
public class SellerProfileUpdatedEvent : EventBase
{
    public override string EventType => "seller.profile.updated";

    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public List<string> UpdatedFields { get; set; } = new();

    public static SellerProfileUpdatedEvent Create(
        Guid sellerId,
        Guid userId,
        List<string> updatedFields)
    {
        return new SellerProfileUpdatedEvent
        {
            SellerId = sellerId,
            UserId = userId,
            UpdatedFields = updatedFields
        };
    }
}

/// <summary>
/// Evento publicado cuando se actualizan las preferencias de contacto
/// </summary>
public class SellerPreferencesUpdatedEvent : EventBase
{
    public override string EventType => "seller.preferences.updated";

    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public string PreferredContactMethod { get; set; } = string.Empty;
    public bool AllowPhoneCalls { get; set; }
    public bool AllowWhatsApp { get; set; }

    public static SellerPreferencesUpdatedEvent Create(
        Guid sellerId,
        Guid userId,
        string preferredContactMethod,
        bool allowPhoneCalls,
        bool allowWhatsApp)
    {
        return new SellerPreferencesUpdatedEvent
        {
            SellerId = sellerId,
            UserId = userId,
            PreferredContactMethod = preferredContactMethod,
            AllowPhoneCalls = allowPhoneCalls,
            AllowWhatsApp = allowWhatsApp
        };
    }
}

/// <summary>
/// Evento publicado cuando un vendedor gana un badge
/// </summary>
public class SellerBadgeEarnedEvent : EventBase
{
    public override string EventType => "seller.badge.earned";

    public Guid SellerId { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }

    public static SellerBadgeEarnedEvent Create(
        Guid sellerId,
        string badge,
        string reason,
        DateTime? expiresAt = null)
    {
        return new SellerBadgeEarnedEvent
        {
            SellerId = sellerId,
            Badge = badge,
            Reason = reason,
            ExpiresAt = expiresAt
        };
    }
}

/// <summary>
/// Evento publicado cuando un vendedor pierde un badge
/// </summary>
public class SellerBadgeLostEvent : EventBase
{
    public override string EventType => "seller.badge.lost";

    public Guid SellerId { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public static SellerBadgeLostEvent Create(
        Guid sellerId,
        string badge,
        string reason)
    {
        return new SellerBadgeLostEvent
        {
            SellerId = sellerId,
            Badge = badge,
            Reason = reason
        };
    }
}

/// <summary>
/// Evento publicado cuando un vendedor es verificado
/// </summary>
public class SellerVerifiedEvent : EventBase
{
    public override string EventType => "seller.verified";

    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public Guid VerifiedByUserId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }

    public static SellerVerifiedEvent Create(
        Guid sellerId,
        Guid userId,
        Guid verifiedByUserId,
        string notes)
    {
        return new SellerVerifiedEvent
        {
            SellerId = sellerId,
            UserId = userId,
            VerifiedByUserId = verifiedByUserId,
            Notes = notes,
            VerifiedAt = DateTime.UtcNow
        };
    }
}
