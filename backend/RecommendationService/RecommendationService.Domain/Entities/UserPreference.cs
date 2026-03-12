using System;
using System.Collections.Generic;

namespace RecommendationService.Domain.Entities;

/// <summary>
/// Preferencias inferidas del usuario basadas en su comportamiento
/// </summary>
public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Preferencias de marca
    public List<string> PreferredMakes { get; set; } = new();
    public List<string> PreferredModels { get; set; } = new();

    // Preferencias de tipo
    public List<string> PreferredBodyTypes { get; set; } = new();
    public List<string> PreferredFuelTypes { get; set; } = new();
    public List<string> PreferredTransmissions { get; set; } = new();

    // Rangos preferidos
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinMileage { get; set; }
    public int? MaxMileage { get; set; }

    // Colores preferidos
    public List<string> PreferredColors { get; set; } = new();

    // Features preferidas
    public List<string> PreferredFeatures { get; set; } = new();

    // Confianza en las preferencias (0.0 - 1.0)
    public double Confidence { get; set; } = 0.0;

    // Contadores para calcular preferencias
    public int TotalVehiclesViewed { get; set; }
    public int TotalSearches { get; set; }
    public int TotalFavorites { get; set; }
    public int TotalContacts { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserPreference() { }

    public UserPreference(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void CalculateConfidence()
    {
        // Más interacciones = mayor confianza
        var totalInteractions = TotalVehiclesViewed + (TotalSearches * 2) + (TotalFavorites * 3) + (TotalContacts * 5);
        Confidence = Math.Min(1.0, totalInteractions / 100.0);
    }
}
