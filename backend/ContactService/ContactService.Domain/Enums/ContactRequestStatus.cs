namespace ContactService.Domain.Enums;

/// <summary>
/// Status of a contact request / inquiry between buyer and seller.
/// </summary>
public enum ContactRequestStatus
{
    /// <summary>Contact request created, awaiting seller response.</summary>
    Open = 0,

    /// <summary>Seller has replied to the contact request.</summary>
    Responded = 1,

    /// <summary>Messages have been read by the recipient.</summary>
    Read = 2,

    /// <summary>Contact request archived by either party.</summary>
    Archived = 3,

    /// <summary>Contact request closed (resolved or no further action).</summary>
    Closed = 4
}
