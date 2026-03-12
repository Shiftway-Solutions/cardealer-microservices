using System;

namespace UserService.Domain.Entities;

/// <summary>
/// Estado del proceso de onboarding de dealer
/// </summary>
public enum DealerOnboardingStatus
{
    /// <summary>Registro inicial pendiente de verificación de email</summary>
    Pending = 0,

    /// <summary>Email verificado, pendiente de documentos</summary>
    EmailVerified = 1,

    /// <summary>Documentos subidos, pendiente de revisión</summary>
    DocumentsSubmitted = 2,

    /// <summary>En revisión por administrador</summary>
    UnderReview = 3,

    /// <summary>Aprobado, pendiente de configuración de pago</summary>
    Approved = 4,

    /// <summary>Pago configurado, pendiente de activación</summary>
    PaymentSetup = 5,

    /// <summary>Completamente activo</summary>
    Active = 6,

    /// <summary>Solicitud rechazada</summary>
    Rejected = 7,

    /// <summary>Cuenta suspendida</summary>
    Suspended = 8
}

/// <summary>
/// Tipo de dealer/concesionario para onboarding
/// </summary>
public enum DealerOnboardingType
{
    /// <summary>Dealer independiente</summary>
    Independent = 0,

    /// <summary>Cadena de concesionarios</summary>
    Chain = 1,

    /// <summary>Múltiples sucursales</summary>
    MultipleStore = 2,

    /// <summary>Franquicia</summary>
    Franchise = 3
}

/// <summary>
/// Plan solicitado durante el onboarding
/// </summary>
public enum DealerOnboardingPlan
{
    None = 0,

    /// <summary>$49/mes - 15 vehículos</summary>
    Starter = 1,

    /// <summary>$129/mes - 50 vehículos</summary>
    Pro = 2,

    /// <summary>$299/mes - Ilimitado</summary>
    Enterprise = 3
}

/// <summary>
/// Entidad para el proceso de onboarding de dealers
/// Gestiona todo el flujo desde registro hasta activación
/// </summary>
public class DealerOnboarding
{
    public Guid Id { get; set; }

    /// <summary>ID del usuario asociado (creado en AuthService)</summary>
    public Guid UserId { get; set; }

    /// <summary>Estado actual del proceso de onboarding</summary>
    public DealerOnboardingStatus Status { get; set; } = DealerOnboardingStatus.Pending;

    // ========================================
    // INFORMACIÓN DEL NEGOCIO
    // ========================================

    /// <summary>Nombre comercial del negocio</summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Razón social/nombre legal</summary>
    public string BusinessLegalName { get; set; } = string.Empty;

    /// <summary>Registro Nacional de Contribuyentes (RD)</summary>
    public string RNC { get; set; } = string.Empty;

    /// <summary>Tipo de dealer</summary>
    public DealerOnboardingType Type { get; set; } = DealerOnboardingType.Independent;

    /// <summary>Descripción del negocio</summary>
    public string? Description { get; set; }

    // ========================================
    // CONTACTO
    // ========================================

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? MobilePhone { get; set; }
    public string? Website { get; set; }

    // ========================================
    // UBICACIÓN
    // ========================================

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // ========================================
    // REPRESENTANTE LEGAL
    // ========================================

    /// <summary>Nombre completo del representante legal</summary>
    public string LegalRepName { get; set; } = string.Empty;

    /// <summary>Cédula del representante legal (formato: 000-0000000-0)</summary>
    public string LegalRepCedula { get; set; } = string.Empty;

    /// <summary>Cargo/posición del representante legal</summary>
    public string LegalRepPosition { get; set; } = string.Empty;

    // ========================================
    // AZUL (BANCO POPULAR) - PAGOS
    // ========================================

    /// <summary>ID del cliente en Azul</summary>
    public string? AzulCustomerId { get; set; }

    /// <summary>ID de la suscripción en Azul</summary>
    public string? AzulSubscriptionId { get; set; }

    /// <summary>Token de tarjeta guardada (para pagos recurrentes)</summary>
    public string? AzulCardToken { get; set; }

    // ========================================
    // PLAN Y EARLY BIRD
    // ========================================

    /// <summary>Plan solicitado por el dealer</summary>
    public DealerOnboardingPlan RequestedPlan { get; set; } = DealerOnboardingPlan.Starter;

    /// <summary>¿Elegible para Early Bird? (registro antes de 31/01/2026)</summary>
    public bool IsEarlyBirdEligible { get; set; }

    /// <summary>¿Se inscribió en Early Bird?</summary>
    public bool IsEarlyBirdEnrolled { get; set; }

    // ========================================
    // DOCUMENTOS (IDs de MediaService)
    // ========================================

    /// <summary>ID del documento RNC en MediaService</summary>
    public Guid? RncDocumentId { get; set; }

    /// <summary>ID de la licencia comercial en MediaService</summary>
    public Guid? BusinessLicenseDocumentId { get; set; }

    /// <summary>ID de la cédula del representante en MediaService</summary>
    public Guid? LegalRepCedulaDocumentId { get; set; }

    /// <summary>ID del contrato social en MediaService (opcional)</summary>
    public Guid? SocialContractDocumentId { get; set; }

    /// <summary>ID del poder legal en MediaService (opcional)</summary>
    public Guid? LegalPowerDocumentId { get; set; }

    /// <summary>ID del comprobante de dirección en MediaService</summary>
    public Guid? AddressProofDocumentId { get; set; }

    // ========================================
    // TIMESTAMPS
    // ========================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? DocumentsSubmittedAt { get; set; }
    public DateTime? UnderReviewAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaymentSetupAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ========================================
    // APROBACIÓN/RECHAZO
    // ========================================

    /// <summary>ID del admin que aprobó</summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>Notas del admin al aprobar</summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>ID del admin que rechazó</summary>
    public Guid? RejectedBy { get; set; }

    /// <summary>Razón del rechazo</summary>
    public string? RejectionReason { get; set; }

    // ========================================
    // TOKEN DE VERIFICACIÓN
    // ========================================

    /// <summary>Token para verificar email</summary>
    public string? EmailVerificationToken { get; set; }

    /// <summary>Fecha de expiración del token (24h)</summary>
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // ========================================
    // MÉTODOS HELPER
    // ========================================

    /// <summary>Verifica si el onboarding está completado</summary>
    public bool IsCompleted => Status == DealerOnboardingStatus.Active;

    /// <summary>Verifica si el onboarding fue rechazado</summary>
    public bool IsRejected => Status == DealerOnboardingStatus.Rejected;

    /// <summary>Verifica si puede subir documentos</summary>
    public bool CanUploadDocuments => Status == DealerOnboardingStatus.EmailVerified;

    /// <summary>Verifica si está pendiente de revisión</summary>
    public bool IsPendingReview => Status == DealerOnboardingStatus.DocumentsSubmitted ||
                                    Status == DealerOnboardingStatus.UnderReview;

    /// <summary>Verifica si puede configurar pago</summary>
    public bool CanSetupPayment => Status == DealerOnboardingStatus.Approved;

    /// <summary>Verifica si todos los documentos obligatorios están subidos</summary>
    public bool HasAllRequiredDocuments =>
        RncDocumentId.HasValue &&
        BusinessLicenseDocumentId.HasValue &&
        LegalRepCedulaDocumentId.HasValue &&
        AddressProofDocumentId.HasValue;

    /// <summary>Calcula si es elegible para Early Bird (antes de 31/01/2026)</summary>
    public static bool CalculateEarlyBirdEligibility(DateTime registrationDate)
    {
        var deadline = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        return registrationDate <= deadline;
    }

    /// <summary>Avanza al siguiente estado del onboarding</summary>
    public void AdvanceToNextStatus()
    {
        Status = Status switch
        {
            DealerOnboardingStatus.Pending => DealerOnboardingStatus.EmailVerified,
            DealerOnboardingStatus.EmailVerified => DealerOnboardingStatus.DocumentsSubmitted,
            DealerOnboardingStatus.DocumentsSubmitted => DealerOnboardingStatus.UnderReview,
            DealerOnboardingStatus.UnderReview => DealerOnboardingStatus.Approved,
            DealerOnboardingStatus.Approved => DealerOnboardingStatus.PaymentSetup,
            DealerOnboardingStatus.PaymentSetup => DealerOnboardingStatus.Active,
            _ => Status
        };
        UpdatedAt = DateTime.UtcNow;
    }
}
