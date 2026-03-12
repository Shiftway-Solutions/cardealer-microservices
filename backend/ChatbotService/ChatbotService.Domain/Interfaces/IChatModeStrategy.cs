using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Models;

namespace ChatbotService.Domain.Interfaces;

/// <summary>
/// Estrategia de contexto para el chatbot.
/// Define cómo se construye el system prompt y el contexto de RAG
/// según el modo de operación (SingleVehicle vs DealerInventory).
/// </summary>
public interface IChatModeStrategy
{
    /// <summary>Modo que implementa esta estrategia</summary>
    ChatMode Mode { get; }

    /// <summary>
    /// Construye el system prompt completo para el LLM, incluyendo
    /// el contexto RAG apropiado para el modo.
    /// </summary>
    Task<string> BuildSystemPromptAsync(
        ChatSession session,
        ChatbotConfiguration config,
        string userMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene las function definitions disponibles para este modo.
    /// SingleVehicle: ninguna (contexto fijo).
    /// DealerInventory: search, compare, schedule, etc.
    /// </summary>
    Task<List<FunctionDefinition>> GetAvailableFunctionsAsync(
        ChatSession session,
        CancellationToken ct = default);

    /// <summary>
    /// Ejecuta un function call solicitado por el LLM.
    /// </summary>
    Task<FunctionCallResult> ExecuteFunctionAsync(
        ChatSession session,
        FunctionCall functionCall,
        CancellationToken ct = default);

    /// <summary>
    /// Valida que la respuesta del LLM esté "grounded" en datos reales.
    /// Previene hallucinations sobre vehículos que no existen.
    /// </summary>
    Task<GroundingValidationResult> ValidateResponseGroundingAsync(
        ChatSession session,
        string llmResponse,
        CancellationToken ct = default);
}

/// <summary>
/// Factory para resolver la estrategia correcta según el ChatMode
/// </summary>
public interface IChatModeStrategyFactory
{
    IChatModeStrategy GetStrategy(ChatMode mode);
}

/// <summary>
/// Servicio de búsqueda vectorial para RAG (pgvector)
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Busca vehículos por similitud semántica + filtros estructurados
    /// </summary>
    Task<List<VehicleSearchResult>> SearchAsync(
        Guid dealerId,
        string query,
        VehicleSearchFilters? filters = null,
        int topK = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Genera y almacena embedding para un vehículo
    /// </summary>
    Task UpsertVehicleEmbeddingAsync(
        ChatbotVehicle vehicle,
        Guid dealerId,
        CancellationToken ct = default);

    /// <summary>
    /// Elimina embedding de un vehículo
    /// </summary>
    Task DeleteVehicleEmbeddingAsync(Guid vehicleId, CancellationToken ct = default);

    /// <summary>
    /// Re-genera todos los embeddings para un dealer (bulk sync)
    /// </summary>
    Task<int> RebuildDealerEmbeddingsAsync(
        Guid dealerId,
        IEnumerable<ChatbotVehicle> vehicles,
        CancellationToken ct = default);

    /// <summary>
    /// Genera embedding para un texto (usa el modelo de embeddings local)
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}

/// <summary>
/// WhatsApp integration service — handles Meta Cloud API webhooks,
/// message sending, and session management for WhatsApp channel.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Verifica el webhook de Meta (challenge de verificación inicial)
    /// </summary>
    bool VerifyWebhook(string mode, string token, string challenge);

    /// <summary>
    /// Parsea un payload de webhook entrante de Meta
    /// </summary>
    WhatsAppInboundMessage? ParseInboundMessage(System.Text.Json.JsonElement payload);

    /// <summary>
    /// Envía un mensaje de texto al usuario por WhatsApp
    /// </summary>
    Task<bool> SendTextMessageAsync(string toPhone, string message, CancellationToken ct = default);

    /// <summary>
    /// Envía un mensaje con botones interactivos (quick replies)
    /// </summary>
    Task<bool> SendInteractiveMessageAsync(
        string toPhone,
        string headerText,
        string bodyText,
        List<(string Id, string Title)> buttons,
        CancellationToken ct = default);

    /// <summary>
    /// Marca un mensaje como leído
    /// </summary>
    Task MarkAsReadAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Valida si un número de teléfono es de RD (país soportado)
    /// </summary>
    bool IsAllowedCountry(string phoneNumber);

    /// <summary>
    /// Rate limiting por número de teléfono
    /// </summary>
    bool CheckRateLimit(string phoneNumber);
}

/// <summary>
/// Servicio de generación de embeddings (sentence-transformers)
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Genera embedding para un texto
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Genera embeddings en batch
    /// </summary>
    Task<List<float[]>> GenerateEmbeddingsBatchAsync(
        List<string> texts,
        CancellationToken ct = default);
}
