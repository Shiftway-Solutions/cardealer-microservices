using ChatbotService.Domain.Enums;

namespace ChatbotService.Domain.Models;

// ══════════════════════════════════════════════════════════════
// FUNCTION CALLING — Definiciones para que el LLM ejecute acciones
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Definición de una función que el LLM puede invocar
/// </summary>
public class FunctionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, FunctionParameter> Parameters { get; set; } = new();
    public List<string> Required { get; set; } = new();
}

public class FunctionParameter
{
    public string Type { get; set; } = "string"; // string, number, boolean, array
    public string Description { get; set; } = string.Empty;
    public List<string>? Enum { get; set; }
}

/// <summary>
/// Function call solicitada por el LLM
/// </summary>
public class FunctionCall
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Resultado de ejecutar una function call
/// </summary>
public class FunctionCallResult
{
    public bool Success { get; set; }
    public string ResultText { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

// ══════════════════════════════════════════════════════════════
// RAG — Búsqueda vectorial y resultados
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Filtros estructurados para búsqueda híbrida de vehículos
/// </summary>
public class VehicleSearchFilters
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? BodyType { get; set; }
    public string? Condition { get; set; }
    public string? Color { get; set; }
    public int? MaxMileage { get; set; }
}

/// <summary>
/// Resultado de búsqueda vectorial de un vehículo
/// </summary>
public class VehicleSearchResult
{
    public Guid VehicleId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string? Trim { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public int? Mileage { get; set; }
    public string? ExteriorColor { get; set; }
    public string? Condition { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsOnSale { get; set; }
    public decimal? OriginalPrice { get; set; }

    /// <summary>Similitud coseno (0-1, mayor = más relevante)</summary>
    public float SimilarityScore { get; set; }

    /// <summary>Texto formateado para inyectar en el prompt del LLM</summary>
    public string ToPromptText()
    {
        var saleTag = IsOnSale && OriginalPrice.HasValue ? $" 🏷️OFERTA (antes RD${OriginalPrice:N0})" : "";
        var mileageText = Mileage.HasValue ? $"{Mileage.Value:N0}km" : "N/A";
        return $"- {Year} {Make} {Model} {Trim ?? ""} | RD${Price:N0}{saleTag} | " +
               $"{FuelType ?? "N/A"} | {Transmission ?? "N/A"} | {mileageText} | " +
               $"{ExteriorColor ?? "N/A"} | {Condition ?? "N/A"} | ID:{VehicleId}";
    }
}

/// <summary>
/// Resultado de validación de grounding (anti-hallucination)
/// </summary>
public class GroundingValidationResult
{
    public bool IsGrounded { get; set; } = true;
    public List<string> UngroundedClaims { get; set; } = new();
    public string? SanitizedResponse { get; set; }
    public string? WarningMessage { get; set; }
}

// ══════════════════════════════════════════════════════════════
// WHATSAPP — Modelos para mensajería entrante/saliente
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Mensaje entrante de WhatsApp (webhook de Meta Cloud API)
/// </summary>
public class WhatsAppInboundMessage
{
    /// <summary>Número de teléfono del remitente (e.g., "18091234567")</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Nombre del perfil de WhatsApp del remitente</summary>
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>ID del mensaje en WhatsApp</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Tipo de mensaje: text, image, document, location, etc.</summary>
    public string MessageType { get; set; } = "text";

    /// <summary>Contenido del mensaje (cuerpo de texto)</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>URL del media (para tipo image/document/video)</summary>
    public string? MediaUrl { get; set; }

    /// <summary>Timestamp del mensaje</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Resultado del procesamiento de un mensaje WhatsApp
/// </summary>
public class WhatsAppProcessingResult
{
    public bool Success { get; set; }
    public string? ResponseText { get; set; }
    public string? SessionToken { get; set; }
    public Guid? SessionId { get; set; }
    public bool IsNewSession { get; set; }
    public bool HandoffTriggered { get; set; }
    public string? ErrorMessage { get; set; }
}

// ══════════════════════════════════════════════════════════════
// EMBEDDING — Modelo para pgvector
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Embedding de vehículo almacenado en pgvector
/// </summary>
public class VehicleEmbedding
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DealerId { get; set; }

    /// <summary>Texto original del que se generó el embedding</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Vector de embedding (384 dims para all-MiniLM-L6-v2)</summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>Metadata para filtros estructurados</summary>
    public VehicleEmbeddingMetadata Metadata { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Metadata indexable de un vehículo para filtros SQL en búsqueda híbrida
/// </summary>
public class VehicleEmbeddingMetadata
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? BodyType { get; set; }
    public string? Condition { get; set; }
    public int? Mileage { get; set; }
    public string? ExteriorColor { get; set; }
    public bool IsAvailable { get; set; } = true;
}
