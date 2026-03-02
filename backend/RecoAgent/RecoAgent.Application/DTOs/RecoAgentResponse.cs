using System.Text.Json.Serialization;

namespace RecoAgent.Application.DTOs;

/// <summary>
/// Full response from the recommendation agent (mirrors Claude JSON output).
/// </summary>
public class RecoAgentResponse
{
    [JsonPropertyName("recomendaciones")]
    public List<RecommendationItem> Recomendaciones { get; set; } = [];

    [JsonPropertyName("patrocinados_config")]
    public SponsoredConfig? PatrocinadosConfig { get; set; }

    [JsonPropertyName("diversificacion_aplicada")]
    public DiversificationApplied? DiversificacionAplicada { get; set; }

    [JsonPropertyName("etapa_compra_detectada")]
    public string EtapaCompraDetectada { get; set; } = "explorador";

    [JsonPropertyName("cold_start_nivel")]
    public int ColdStartNivel { get; set; }

    [JsonPropertyName("confianza_recomendaciones")]
    public float ConfianzaRecomendaciones { get; set; }

    [JsonPropertyName("proxima_actualizacion")]
    public DateTime? ProximaActualizacion { get; set; }
}

/// <summary>
/// Individual vehicle recommendation with scoring and explanation.
/// </summary>
public class RecommendationItem
{
    [JsonPropertyName("vehiculo_id")]
    public string VehiculoId { get; set; } = string.Empty;

    [JsonPropertyName("posicion")]
    public int Posicion { get; set; }

    [JsonPropertyName("razon_recomendacion")]
    public string RazonRecomendacion { get; set; } = string.Empty;

    [JsonPropertyName("tipo_recomendacion")]
    public string TipoRecomendacion { get; set; } = "perfil"; // perfil | similar | descubrimiento | popular | patrocinado

    [JsonPropertyName("score_afinidad_perfil")]
    public float ScoreAfinidadPerfil { get; set; }

    [JsonPropertyName("es_patrocinado")]
    public bool EsPatrocinado { get; set; }
}

/// <summary>
/// Sponsored placement configuration metadata.
/// </summary>
public class SponsoredConfig
{
    [JsonPropertyName("posiciones_patrocinados")]
    public List<int> PosicionesPatrocinados { get; set; } = [2, 6, 11];

    [JsonPropertyName("label")]
    public string Label { get; set; } = "Destacado";

    [JsonPropertyName("threshold_score")]
    public float ThresholdScore { get; set; } = 0.50f;

    [JsonPropertyName("total_insertados")]
    public int TotalInsertados { get; set; }
}

/// <summary>
/// Diversification info applied to the recommendations.
/// </summary>
public class DiversificationApplied
{
    [JsonPropertyName("marcas_distintas")]
    public int MarcasDistintas { get; set; }

    [JsonPropertyName("max_misma_marca")]
    public int MaxMismaMarca { get; set; }

    [JsonPropertyName("max_misma_marca_porcentaje")]
    public float MaxMismaMarcaPorcentaje { get; set; }

    [JsonPropertyName("tipos_incluidos")]
    public List<string> TiposIncluidos { get; set; } = [];
}
