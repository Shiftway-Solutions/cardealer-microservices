using CarDealer.Contracts.Abstractions;

namespace UserService.Domain.Events;

/// <summary>
/// Evento publicado cuando un dealer se registra (inicio de onboarding)
/// </summary>
public class DealerRegisteredEvent : EventBase
{
    public override string EventType => "dealer.registered";

    public Guid DealerId { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RNC { get; set; } = string.Empty;
    public string RequestedPlan { get; set; } = string.Empty;
    public bool IsEarlyBirdEligible { get; set; }

    public static DealerRegisteredEvent Create(
        Guid dealerId,
        Guid userId,
        string businessName,
        string email,
        string rnc,
        string requestedPlan,
        bool isEarlyBirdEligible)
    {
        return new DealerRegisteredEvent
        {
            DealerId = dealerId,
            UserId = userId,
            BusinessName = businessName,
            Email = email,
            RNC = rnc,
            RequestedPlan = requestedPlan,
            IsEarlyBirdEligible = isEarlyBirdEligible
        };
    }
}

/// <summary>
/// Evento publicado cuando un dealer verifica su email
/// </summary>
public class DealerEmailVerifiedEvent : EventBase
{
    public override string EventType => "dealer.email_verified";

    public Guid DealerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }

    public static DealerEmailVerifiedEvent Create(Guid dealerId, string email, DateTime verifiedAt)
    {
        return new DealerEmailVerifiedEvent
        {
            DealerId = dealerId,
            Email = email,
            VerifiedAt = verifiedAt
        };
    }
}

/// <summary>
/// Evento publicado cuando un dealer sube todos sus documentos
/// </summary>
public class DealerDocumentsSubmittedEvent : EventBase
{
    public override string EventType => "dealer.documents_submitted";

    public Guid DealerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public DateTime SubmittedAt { get; set; }

    public static DealerDocumentsSubmittedEvent Create(
        Guid dealerId,
        string businessName,
        int documentCount,
        DateTime submittedAt)
    {
        return new DealerDocumentsSubmittedEvent
        {
            DealerId = dealerId,
            BusinessName = businessName,
            DocumentCount = documentCount,
            SubmittedAt = submittedAt
        };
    }
}

/// <summary>
/// Evento publicado cuando un admin aprueba un dealer
/// </summary>
public class DealerApprovedEvent : EventBase
{
    public override string EventType => "dealer.approved";

    public Guid DealerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid ApprovedBy { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string RequestedPlan { get; set; } = string.Empty;

    public static DealerApprovedEvent Create(
        Guid dealerId,
        string businessName,
        string email,
        Guid approvedBy,
        DateTime approvedAt,
        string requestedPlan)
    {
        return new DealerApprovedEvent
        {
            DealerId = dealerId,
            BusinessName = businessName,
            Email = email,
            ApprovedBy = approvedBy,
            ApprovedAt = approvedAt,
            RequestedPlan = requestedPlan
        };
    }
}

/// <summary>
/// Evento publicado cuando un admin rechaza un dealer
/// </summary>
public class DealerRejectedEvent : EventBase
{
    public override string EventType => "dealer.rejected";

    public Guid DealerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid RejectedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }

    public static DealerRejectedEvent Create(
        Guid dealerId,
        string businessName,
        string email,
        Guid rejectedBy,
        string reason,
        DateTime rejectedAt)
    {
        return new DealerRejectedEvent
        {
            DealerId = dealerId,
            BusinessName = businessName,
            Email = email,
            RejectedBy = rejectedBy,
            Reason = reason,
            RejectedAt = rejectedAt
        };
    }
}

/// <summary>
/// Evento publicado cuando un dealer configura su pago con Azul
/// </summary>
public class DealerPaymentSetupEvent : EventBase
{
    public override string EventType => "dealer.payment_setup";

    public Guid DealerId { get; set; }
    public string AzulCustomerId { get; set; } = string.Empty;
    public string? AzulSubscriptionId { get; set; }
    public string RequestedPlan { get; set; } = string.Empty;
    public bool IsEarlyBirdEnrolled { get; set; }
    public DateTime SetupAt { get; set; }

    public static DealerPaymentSetupEvent Create(
        Guid dealerId,
        string azulCustomerId,
        string? azulSubscriptionId,
        string requestedPlan,
        bool isEarlyBirdEnrolled,
        DateTime setupAt)
    {
        return new DealerPaymentSetupEvent
        {
            DealerId = dealerId,
            AzulCustomerId = azulCustomerId,
            AzulSubscriptionId = azulSubscriptionId,
            RequestedPlan = requestedPlan,
            IsEarlyBirdEnrolled = isEarlyBirdEnrolled,
            SetupAt = setupAt
        };
    }
}

/// <summary>
/// Evento publicado cuando un dealer es activado completamente
/// </summary>
public class DealerActivatedEvent : EventBase
{
    public override string EventType => "dealer.activated";

    public Guid OnboardingId { get; set; }
    public Guid DealerId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsEarlyBird { get; set; }
    public DateTime ActivatedAt { get; set; }

    public static DealerActivatedEvent Create(
        Guid onboardingId,
        Guid dealerId,
        Guid subscriptionId,
        string businessName,
        string email,
        string plan,
        bool isEarlyBird,
        DateTime activatedAt)
    {
        return new DealerActivatedEvent
        {
            OnboardingId = onboardingId,
            DealerId = dealerId,
            SubscriptionId = subscriptionId,
            BusinessName = businessName,
            Email = email,
            Plan = plan,
            IsEarlyBird = isEarlyBird,
            ActivatedAt = activatedAt
        };
    }
}
