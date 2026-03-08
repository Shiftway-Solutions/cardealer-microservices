namespace ContactService.Domain.Enums;

/// <summary>
/// Preferred contact method for the buyer.
/// </summary>
public enum ContactMethod
{
    /// <summary>Contact via email.</summary>
    Email = 0,

    /// <summary>Contact via phone call.</summary>
    Phone = 1,

    /// <summary>Contact via WhatsApp.</summary>
    WhatsApp = 2,

    /// <summary>Any method is fine.</summary>
    Any = 3
}
