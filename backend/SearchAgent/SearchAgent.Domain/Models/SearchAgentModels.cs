using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchAgent.Domain.Models;

/// <summary>
/// Defensive JSON converter that handles cases where Claude returns an array instead of a
/// single string for filter fields (e.g., combustible: ["hibrido", "electrico"]).
/// Takes the first element of any array, preserves plain strings, maps null → null.
/// </summary>
public sealed class StringOrArrayConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            JsonTokenType.StartArray => ReadFirstArrayElement(ref reader),
            _ => reader.GetString()
        };
    }

    private static string? ReadFirstArrayElement(ref Utf8JsonReader reader)
    {
        string? first = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (first == null && reader.TokenType == JsonTokenType.String)
                first = reader.GetString();
            // Skip any remaining elements
        }
        return first;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}

/// <summary>
/// Complete response from SearchAgent following the v2.0 schema.
/// Contains exact filters, relaxed filters, sponsored config, and metadata.
/// </summary>
public class SearchAgentResponse
{
    [JsonPropertyName("filtros_exactos")]
    public SearchFilters? FiltrosExactos { get; set; }

    [JsonPropertyName("filtros_relajados")]
    public SearchFilters? FiltrosRelajados { get; set; }

    [JsonPropertyName("resultado_minimo_garantizado")]
    public int ResultadoMinimoGarantizado { get; set; } = 8;

    [JsonPropertyName("nivel_filtros_activo")]
    public int NivelFiltrosActivo { get; set; } = 1;

    [JsonPropertyName("patrocinados_config")]
    public SponsoredConfig? PatrocinadosConfig { get; set; }

    [JsonPropertyName("ordenar_por")]
    public string OrdenarPor { get; set; } = "relevancia";

    [JsonPropertyName("dealer_verificado")]
    public bool? DealerVerificado { get; set; }

    [JsonPropertyName("confianza")]
    public float Confianza { get; set; }

    [JsonPropertyName("query_reformulada")]
    public string? QueryReformulada { get; set; }

    [JsonPropertyName("advertencias")]
    public List<string> Advertencias { get; set; } = [];

    [JsonPropertyName("mensaje_relajamiento")]
    public string? MensajeRelajamiento { get; set; }

    [JsonPropertyName("mensaje_usuario")]
    public string? MensajeUsuario { get; set; }
}

public class SearchFilters
{
    [JsonPropertyName("marca")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Marca { get; set; }

    [JsonPropertyName("modelo")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Modelo { get; set; }

    [JsonPropertyName("anio_desde")]
    public int? AnioDeSde { get; set; }

    [JsonPropertyName("anio_hasta")]
    public int? AnioHasta { get; set; }

    [JsonPropertyName("precio_min")]
    public decimal? PrecioMin { get; set; }

    [JsonPropertyName("precio_max")]
    public decimal? PrecioMax { get; set; }

    [JsonPropertyName("moneda")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Moneda { get; set; }

    [JsonPropertyName("tipo_vehiculo")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? TipoVehiculo { get; set; }

    [JsonPropertyName("transmision")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Transmision { get; set; }

    [JsonPropertyName("combustible")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Combustible { get; set; }

    [JsonPropertyName("condicion")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Condicion { get; set; }

    [JsonPropertyName("kilometraje_max")]
    public int? KilometrajeMax { get; set; }

    [JsonPropertyName("provincia")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Provincia { get; set; }

    [JsonPropertyName("ciudad")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Ciudad { get; set; }

    [JsonPropertyName("color")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Color { get; set; }

    [JsonPropertyName("traccion")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Traccion { get; set; }
}

public class SponsoredConfig
{
    [JsonPropertyName("umbral_afinidad")]
    public float UmbralAfinidad { get; set; } = 0.45f;

    [JsonPropertyName("tipo_vehiculo_afinidad")]
    public List<string> TipoVehiculoAfinidad { get; set; } = [];

    [JsonPropertyName("precio_rango_afinidad")]
    public PriceRange? PrecioRangoAfinidad { get; set; }

    [JsonPropertyName("marcas_afinidad")]
    public List<string> MarcasAfinidad { get; set; } = [];

    [JsonPropertyName("anio_rango_afinidad")]
    public YearRange? AnioRangoAfinidad { get; set; }

    [JsonPropertyName("max_porcentaje_resultados")]
    public float MaxPorcentajeResultados { get; set; } = 0.25f;

    [JsonPropertyName("posiciones_fijas")]
    public List<int> PosicionesFijas { get; set; } = [1, 5, 10];

    [JsonPropertyName("etiqueta")]
    public string Etiqueta { get; set; } = "Patrocinado";
}

public class PriceRange
{
    [JsonPropertyName("min")]
    public decimal Min { get; set; }

    [JsonPropertyName("max")]
    public decimal Max { get; set; }

    [JsonPropertyName("moneda")]
    public string Moneda { get; set; } = "USD";
}

public class YearRange
{
    [JsonPropertyName("desde")]
    public int Desde { get; set; }

    [JsonPropertyName("hasta")]
    public int Hasta { get; set; }
}
