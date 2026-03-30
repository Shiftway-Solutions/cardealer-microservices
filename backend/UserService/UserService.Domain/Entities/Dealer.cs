using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

/// <summary>
/// Estado de verificación del dealer
/// </summary>
public enum DealerVerificationStatus
{
    Pending = 0,
    UnderReview = 1,
    Verified = 2,
    Rejected = 3,
    Suspended = 4
}

/// <summary>
/// Tipo de dealer/concesionario
/// </summary>
public enum DealerType
{
    Independent = 0,        // Lote independiente
    Franchise = 1,          // Concesionario oficial de marca
    MultiLocation = 2,      // Cadena con múltiples ubicaciones
    OnlineOnly = 3,         // Solo ventas online
    Wholesale = 4           // Venta mayorista
}

/// <summary>
/// Entidad Dealer - Representa un concesionario o vendedor profesional
/// </summary>
public class Dealer
{
    public Guid Id { get; set; }

    // ========================================
    // INFORMACIÓN BÁSICA
    // ========================================
    public string BusinessName { get; set; } = string.Empty;
    public string? TradeName { get; set; } // Nombre comercial si es diferente
    public string? Slug { get; set; } // URL-friendly identifier: "auto-plaza-santo-domingo"
    public string? Description { get; set; }
    public DealerType DealerType { get; set; } = DealerType.Independent;

    // ========================================
    // CONTACTO
    // ========================================
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }

    // ========================================
    // UBICACIÓN
    // ========================================
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "DO"; // República Dominicana por defecto
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // ========================================
    // BRANDING
    // ========================================
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? PrimaryColor { get; set; } // Hex color

    // ========================================
    // DOCUMENTACIÓN LEGAL
    // ========================================
    public string? BusinessRegistrationNumber { get; set; } // RNC (República Dominicana)
    public string? TaxId { get; set; }
    public string? DealerLicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public string? BusinessLicenseDocumentUrl { get; set; }

    // ========================================
    // VERIFICACIÓN
    // ========================================
    public DealerVerificationStatus VerificationStatus { get; set; } = DealerVerificationStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? VerificationNotes { get; set; }
    public string? RejectionReason { get; set; }

    // ════════════════════════════════════════════
    // BADGE "DEALER VERIFICADO OKLA" — 4 criterios
    // Ref: okla.do/verificacion
    // ════════════════════════════════════════════
    /// <summary>Criterio 1: RNC validado contra DGII (API o manual)</summary>
    public bool IsRncVerified { get; set; } = false;
    /// <summary>Timestamp de verificación del RNC en DGII</summary>
    public DateTime? RncVerifiedAt { get; set; }
    /// <summary>Estado del contribuyente según DGII (Activo, Suspendido, etc.)</summary>
    public string? DgiiTaxpayerStatus { get; set; }

    /// <summary>Criterio 2: WhatsApp verificado por OTP</summary>
    public bool IsWhatsAppVerified { get; set; } = false;
    /// <summary>Timestamp de verificación del WhatsApp</summary>
    public DateTime? WhatsAppVerifiedAt { get; set; }

    /// <summary>Criterio 3: ≥10 listings con fotos procesadas por ModerationAgent</summary>
    public int ModeratedListingsCount { get; set; } = 0;
    /// <summary>Última vez que se recalculó el conteo de listings moderados</summary>
    public DateTime? ModeratedListingsUpdatedAt { get; set; }

    /// <summary>Criterio 4: Sin reportes de fraude en los últimos 90 días</summary>
    public bool HasRecentFraudReports { get; set; } = false;
    /// <summary>Última vez que se verificó el historial de fraude</summary>
    public DateTime? FraudCheckAt { get; set; }

    /// <summary>
    /// Computed: true when ALL 4 criteria are met.
    /// This drives the "Dealer Verificado OKLA" badge display.
    /// </summary>
    public bool IsOklaVerified =>
        IsRncVerified &&
        IsWhatsAppVerified &&
        ModeratedListingsCount >= 10 &&
        !HasRecentFraudReports;

    // ========================================
    // MÉTRICAS
    // ========================================
    public int TotalListings { get; set; } = 0;
    public int ActiveListings { get; set; } = 0;
    public int TotalSales { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0;
    public int TotalReviews { get; set; } = 0;
    public int ResponseTimeMinutes { get; set; } = 0; // Promedio de tiempo de respuesta

    // ========================================
    // CONFIGURACIÓN
    // ========================================
    public bool IsActive { get; set; } = true;
    public bool AcceptsFinancing { get; set; } = false;
    public bool AcceptsTradeIn { get; set; } = false;
    public bool OffersWarranty { get; set; } = false;
    public bool HomeDelivery { get; set; } = false;
    public string? BusinessHours { get; set; } // JSON con horarios
    public string? SocialMediaLinks { get; set; } // JSON con links
    public string? NotificationSettingsJson { get; set; } // JSON con preferencias de notificaciones
    public string? SecuritySettingsJson { get; set; } // JSON con preferencias operativas de seguridad

    // ========================================
    // SUSCRIPCIÓN
    // ========================================
    public Guid? SubscriptionId { get; set; }
    public int MaxListings { get; set; } = 10; // Límite según plan
    public bool IsFeatured { get; set; } = false;
    public DateTime? FeaturedUntil { get; set; }

    // ========================================
    // AUDITORÍA
    // ========================================
    public Guid OwnerUserId { get; set; } // Usuario propietario del dealer
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // ========================================
    // NAVEGACIÓN
    // ========================================
    public ICollection<DealerEmployee> Employees { get; set; } = new List<DealerEmployee>();
}
