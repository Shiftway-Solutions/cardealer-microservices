using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RecoAgent.Application.DTOs;

/// <summary>
/// Request DTO for the recommendation endpoint.
/// Contains the user profile and vehicle candidates for recommendation generation.
/// </summary>
public class RecoAgentRequest
{
    /// <summary>
    /// User profile with behavioral signals, preferences, and history.
    /// </summary>
    [Required]
    public UserProfile Perfil { get; set; } = new();

    /// <summary>
    /// Pre-filtered vehicle candidates (top 50 from ElasticSearch).
    /// </summary>
    public List<VehicleCandidate> Candidatos { get; set; } = [];

    /// <summary>
    /// Additional instructions for the agent (e.g., mode, section, reference listing).
    /// </summary>
    public string? InstruccionesAdicionales { get; set; }

    /// <summary>
    /// Session ID for tracking.
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// User profile object with all behavioral signals for recommendation generation.
/// </summary>
public class UserProfile
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("cold_start_level")]
    public int ColdStartLevel { get; set; }

    [JsonPropertyName("precio_perfil_min")]
    public decimal? PrecioPerfilMin { get; set; }

    [JsonPropertyName("precio_perfil_max")]
    public decimal? PrecioPerfilMax { get; set; }

    [JsonPropertyName("moneda_preferida")]
    public string MonedaPreferida { get; set; } = "USD";

    [JsonPropertyName("tipos_preferidos")]
    public List<string> TiposPreferidos { get; set; } = [];

    [JsonPropertyName("marcas_preferidas")]
    public List<string> MarcasPreferidas { get; set; } = [];

    [JsonPropertyName("marcas_excluidas")]
    public List<string> MarcasExcluidas { get; set; } = [];

    [JsonPropertyName("anio_min_preferido")]
    public int? AnioMinPreferido { get; set; }

    [JsonPropertyName("transmision_preferida")]
    public string? TransmisionPreferida { get; set; }

    [JsonPropertyName("etapa_compra")]
    public string EtapaCompra { get; set; } = "explorador";

    [JsonPropertyName("vistas_recientes")]
    public List<VehicleView> VistasRecientes { get; set; } = [];

    [JsonPropertyName("favoritos")]
    public List<string> Favoritos { get; set; } = [];

    [JsonPropertyName("historial_busquedas")]
    public List<string> HistorialBusquedas { get; set; } = [];

    [JsonPropertyName("similares_a_mi")]
    public List<string> SimilaresAMi { get; set; } = [];

    [JsonPropertyName("feedback_reco")]
    public List<RecoFeedback> FeedbackReco { get; set; } = [];

    [JsonPropertyName("ultima_actualizacion")]
    public DateTime? UltimaActualizacion { get; set; }
}

/// <summary>
/// Vehicle view record with engagement signals.
/// </summary>
public class VehicleView
{
    [JsonPropertyName("vehiculo_id")]
    public string VehiculoId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("tiempo_en_pagina_seg")]
    public int TiempoEnPaginaSeg { get; set; }

    [JsonPropertyName("vistas_repetidas")]
    public int VistasRepetidas { get; set; } = 1;
}

/// <summary>
/// Feedback on a previous recommendation (thumbs up/down).
/// </summary>
public class RecoFeedback
{
    [JsonPropertyName("vehiculo_id")]
    public string VehiculoId { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "neutral"; // thumbs_up | thumbs_down | dismiss

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Vehicle candidate pre-filtered by ElasticSearch for the agent.
/// </summary>
public class VehicleCandidate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("marca")]
    public string Marca { get; set; } = string.Empty;

    [JsonPropertyName("modelo")]
    public string Modelo { get; set; } = string.Empty;

    [JsonPropertyName("anio")]
    public int Anio { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("moneda")]
    public string Moneda { get; set; } = "USD";

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "sedan";

    [JsonPropertyName("transmision")]
    public string? Transmision { get; set; }

    [JsonPropertyName("combustible")]
    public string? Combustible { get; set; }

    [JsonPropertyName("kilometraje")]
    public int? Kilometraje { get; set; }

    [JsonPropertyName("okla_score")]
    public float OklaScore { get; set; }

    [JsonPropertyName("ad_active")]
    public bool AdActive { get; set; }

    [JsonPropertyName("dealer_verificado")]
    public bool DealerVerificado { get; set; }

    [JsonPropertyName("fotos_count")]
    public int FotosCount { get; set; }

    [JsonPropertyName("ubicacion")]
    public string? Ubicacion { get; set; }
}
