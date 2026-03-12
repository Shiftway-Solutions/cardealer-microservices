using System;

namespace UserService.Domain.Entities.Privacy;

/// <summary>
/// Preferencias de comunicación del usuario (Derecho de Oposición)
/// </summary>
public class CommunicationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    // Email preferences
    public bool EmailActivityNotifications { get; set; } = true;      // Mensajes, respuestas
    public bool EmailListingUpdates { get; set; } = true;             // Vistas, contactos en anuncios
    public bool EmailNewsletter { get; set; } = false;                // Newsletter semanal
    public bool EmailPromotions { get; set; } = false;                // Ofertas y promociones
    public bool EmailPriceAlerts { get; set; } = true;                // Alertas de precio

    // SMS preferences
    public bool SmsVerificationCodes { get; set; } = true;            // Siempre true (obligatorio)
    public bool SmsPriceAlerts { get; set; } = false;                 // Alertas de precios
    public bool SmsPromotions { get; set; } = false;                  // Promociones

    // Push notifications
    public bool PushNewMessages { get; set; } = true;                 // Mensajes nuevos
    public bool PushPriceChanges { get; set; } = true;                // Cambios en favoritos
    public bool PushRecommendations { get; set; } = false;            // Recomendaciones IA

    // WhatsApp preferences (Ley 172-13 + Meta Business Policy — explicit opt-in required)
    public bool WhatsAppTransactional { get; set; } = true;           // Leads, confirmaciones (obligatorio)
    public bool WhatsAppMarketing { get; set; } = false;              // Promociones, ofertas (opt-in voluntario)
    public bool WhatsAppPriceAlerts { get; set; } = false;            // Alertas de precio por WhatsApp

    // Privacy preferences
    public bool AllowProfiling { get; set; } = true;                  // Recomendaciones personalizadas
    public bool AllowThirdPartySharing { get; set; } = false;         // Compartir con terceros
    public bool AllowAnalytics { get; set; } = true;                  // Cookies analytics
    public bool AllowRetargeting { get; set; } = false;               // Cookies retargeting

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public User? User { get; set; }
}
