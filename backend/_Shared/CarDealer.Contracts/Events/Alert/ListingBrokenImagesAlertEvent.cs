using CarDealer.Contracts.Abstractions;

namespace CarDealer.Contracts.Events.Alert;

// ═══════════════════════════════════════════════════════════════════════════════
// LISTING BROKEN IMAGES ALERT EVENT
//
// Published by MediaService when the ImageUrlHealthScanJob detects that an
// active listing has more than 50% of its images broken (403/404/410/500/timeout).
//
// Consumed by NotificationService to:
// 1. Send WhatsApp to the dealer: "Algunas fotos de tu [Marca Modelo Año] no
//    se están viendo. Entra a tu panel OKLA para resubirlas."
// 2. Persist in-app notification for the dealer
// 3. Mark the listing with "Fotos en actualización" banner for buyers
//
// Throttle: Max 1 notification per listing per 24 hours.
// ═══════════════════════════════════════════════════════════════════════════════

public class ListingBrokenImagesAlertEvent : EventBase
{
    public override string EventType => "alert.listing.broken_images";

    /// <summary>Dealer tenant ID (multi-tenant FK).</summary>
    public Guid DealerId { get; set; }

    /// <summary>The vehicle listing ID that has broken images.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Vehicle description for the notification message.</summary>
    public string VehicleTitle { get; set; } = string.Empty;

    /// <summary>Make of the vehicle (e.g., "Toyota").</summary>
    public string Make { get; set; } = string.Empty;

    /// <summary>Model of the vehicle (e.g., "Corolla").</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Year of the vehicle.</summary>
    public int Year { get; set; }

    /// <summary>Seller user ID (for resolving WhatsApp number).</summary>
    public Guid SellerId { get; set; }

    /// <summary>Dealer WhatsApp phone number if available.</summary>
    public string? DealerWhatsApp { get; set; }

    /// <summary>Total number of images in the listing.</summary>
    public int TotalImages { get; set; }

    /// <summary>Number of broken images detected.</summary>
    public int BrokenImages { get; set; }

    /// <summary>Percentage of images that are broken (0-100).</summary>
    public double BrokenPercentage { get; set; }

    /// <summary>HTTP status codes that caused the failures.</summary>
    public List<int> BrokenStatusCodes { get; set; } = new();

    /// <summary>Listing slug for deep-linking into the dealer panel.</summary>
    public string? ListingSlug { get; set; }
}
