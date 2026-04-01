using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SupportAgent.Application.DTOs;
using SupportAgent.Application.Services;
using SupportAgent.Domain.Entities;
using SupportAgent.Domain.Interfaces;

namespace SupportAgent.Application.Features.Chat.Commands;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SupportChatResponse>
{
    private readonly IClaudeSupportService _claudeService;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ISupportAgentConfigRepository _configRepository;
    private readonly IFaqResponseCache _faqCache;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IClaudeSupportService claudeService,
        IChatSessionRepository sessionRepository,
        ISupportAgentConfigRepository configRepository,
        IFaqResponseCache faqCache,
        ILogger<SendMessageCommandHandler> logger)
    {
        _claudeService = claudeService;
        _sessionRepository = sessionRepository;
        _configRepository = configRepository;
        _faqCache = faqCache;
        _logger = logger;
    }

    public async Task<SupportChatResponse> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var config = await _configRepository.GetActiveConfigAsync(ct);

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER 1: Prompt Injection Detection (pre-LLM)
        // ══════════════════════════════════════════════════════════════
        var injectionResult = PromptInjectionDetector.Detect(request.Message);
        if (injectionResult.ShouldBlock)
        {
            _logger.LogWarning(
                "Prompt injection BLOCKED. Threat={Threat}, Patterns={Patterns}, IP={IP}",
                injectionResult.ThreatLevel,
                string.Join(", ", injectionResult.DetectedPatterns),
                request.IpAddress);

            return new SupportChatResponse
            {
                SessionId = request.SessionId ?? Guid.NewGuid().ToString("N"),
                Response = "Lo siento, no puedo procesar ese mensaje. Si necesitas ayuda, reformula tu pregunta sobre cómo usar OKLA o el proceso de compra de vehículos. 😊",
                DetectedModule = "blocked",
                Timestamp = DateTime.UtcNow
            };
        }

        // Sanitize medium-threat messages (remove injection tokens but allow through)
        var processedMessage = request.Message;
        if (injectionResult.IsInjectionDetected)
        {
            processedMessage = PromptInjectionDetector.Sanitize(request.Message);
            _logger.LogWarning(
                "Prompt injection SANITIZED. Threat={Threat}, Patterns={Patterns}",
                injectionResult.ThreatLevel,
                string.Join(", ", injectionResult.DetectedPatterns));
        }

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER 2: PII Detection & Sanitization (pre-LLM)
        // ══════════════════════════════════════════════════════════════
        var piiResult = PiiDetector.Sanitize(processedMessage);
        if (piiResult.DetectionInfo.WasSanitized)
        {
            processedMessage = piiResult.SanitizedMessage;
            _logger.LogInformation(
                "PII sanitized from support message. Types={Types}, Session={SessionId}",
                string.Join(", ", piiResult.DetectionInfo.DetectedTypes),
                request.SessionId);
        }

        // If credit card detected, recommend human agent transfer
        if (piiResult.DetectionInfo.RequiresAgentTransfer)
        {
            _logger.LogWarning("Credit card detected in support chat — recommending agent transfer");
            return new SupportChatResponse
            {
                SessionId = request.SessionId ?? Guid.NewGuid().ToString("N"),
                Response = "🔒 Por tu seguridad, he redactado la información de tu tarjeta. " +
                           "Nunca compartas datos de tarjeta de crédito en el chat. " +
                           "Para asuntos de pagos, contacta directamente a soporte@okla.com.do o " +
                           "llama al equipo de facturación. ¿Puedo ayudarte con algo más?",
                DetectedModule = "pii_redirect",
                Timestamp = DateTime.UtcNow
            };
        }

        // Get or create session
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");
        var session = await _sessionRepository.GetBySessionIdAsync(sessionId, ct);

        if (session == null)
        {
            session = new ChatSession
            {
                SessionId = sessionId,
                UserId = request.UserId,
                IpAddress = request.IpAddress
            };
            session = await _sessionRepository.CreateAsync(session, ct);
            _logger.LogInformation("New support chat session created: {SessionId}", sessionId);
        }

        // Check session timeout (30 min default)
        if (session.LastActivityAt.AddMinutes(config.SessionTimeoutMinutes) < DateTime.UtcNow)
        {
            session.IsActive = false;
            await _sessionRepository.UpdateAsync(session, ct);

            // Create a fresh session
            session = new ChatSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                UserId = request.UserId,
                IpAddress = request.IpAddress
            };
            session = await _sessionRepository.CreateAsync(session, ct);
            sessionId = session.SessionId;
            _logger.LogInformation("Session timed out, new session created: {SessionId}", sessionId);
        }

        // Load conversation history for context
        var recentMessages = await _sessionRepository.GetMessagesAsync(
            session.Id, config.MaxConversationHistory, ct);

        var conversationHistory = recentMessages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationMessage(m.Role, m.Content))
            .ToList();

        // Detect module from user message
        var detectedModule = DetectModule(processedMessage);

        // Save user message (with PII already redacted)
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = "user",
            Content = processedMessage,
            DetectedModule = detectedModule
        };
        await _sessionRepository.AddMessageAsync(userMessage, ct);

        // ══════════════════════════════════════════════════════════════
        // PERFORMANCE: FAQ Response Cache (check before Claude API call)
        // First-message queries without conversation history are FAQ candidates.
        // Cache hit resolves in <5 ms vs 4-8 s for a Claude API round-trip.
        // ══════════════════════════════════════════════════════════════
        string? cachedFaqResponse = null;
        var isFaqCandidate = conversationHistory.Count == 0; // Only cache first-turn questions
        if (isFaqCandidate && _faqCache.TryGet(processedMessage, out var cached))
        {
            cachedFaqResponse = cached;
            _logger.LogInformation(
                "FAQ cache HIT. Session={SessionId}, Module={Module}, Latency={Latency}ms",
                sessionId, detectedModule, sw.ElapsedMilliseconds);
        }

        // ══════════════════════════════════════════════════════════════
        // LOCAL FAQ BANK: Deterministic answers for common questions
        // Resolved in <1 ms without requiring the Claude API.
        // ══════════════════════════════════════════════════════════════
        if (cachedFaqResponse == null && isFaqCandidate)
        {
            var localFaq = LocalFaqMatcher.TryMatch(processedMessage);
            if (localFaq != null)
            {
                cachedFaqResponse = localFaq;
                // Seed the runtime cache so follow-up repetitions are instant
                if (!piiResult.DetectionInfo.WasSanitized)
                    _faqCache.Set(processedMessage, localFaq);

                _logger.LogInformation(
                    "LocalFAQ match. Session={SessionId}, Module={Module}, Latency={Latency}ms",
                    sessionId, detectedModule, sw.ElapsedMilliseconds);
            }
        }

        string responseText;
        int inputTokens = 0, outputTokens = 0;

        if (cachedFaqResponse != null)
        {
            responseText = cachedFaqResponse;
        }
        else
        {
            try
            {
                // Call Claude API with the sanitized message and enhanced prompt
                var claudeResponse = await _claudeService.SendMessageAsync(
                    processedMessage,
                    conversationHistory,
                    SupportAgentPrompts.SystemPromptV2,
                    config.Temperature,
                    config.MaxTokens,
                    ct);

                responseText = claudeResponse.Response;
                inputTokens = claudeResponse.InputTokens;
                outputTokens = claudeResponse.OutputTokens;

                // Store in FAQ cache for future hits (only first-turn, non-PII messages)
                if (isFaqCandidate && !piiResult.DetectionInfo.WasSanitized)
                {
                    _faqCache.Set(processedMessage, responseText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Claude API unavailable for support chat. Session={SessionId}, Module={Module}",
                    sessionId, detectedModule);

                responseText =
                    "🛠️ El asistente de soporte no está disponible en este momento. " +
                    "Por favor, intenta de nuevo más tarde o contáctanos directamente:\n\n" +
                    "📧 **soporte@okla.com.do**\n\n" +
                    "Disculpa los inconvenientes.";
            }
        }

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER 3: PII Echo-Back Prevention (post-LLM)
        // ══════════════════════════════════════════════════════════════
        var sanitizedResponse = PiiDetector.SanitizeResponse(responseText);
        if (sanitizedResponse != responseText)
        {
            _logger.LogWarning("PII echo-back detected and sanitized in SupportAgent response");
        }

        // ══════════════════════════════════════════════════════════════
        // SAFETY LAYER 4: Output Grounding Validation (post-LLM)
        // ══════════════════════════════════════════════════════════════
        var groundingResult = SupportOutputValidator.Validate(sanitizedResponse);
        if (!groundingResult.IsGrounded)
        {
            sanitizedResponse = groundingResult.SanitizedResponse;
            _logger.LogWarning(
                "Output grounding violations detected. Violations={Violations}, Session={SessionId}",
                string.Join(", ", groundingResult.Violations),
                sessionId);
        }

        sw.Stop();

        // Save assistant message (sanitized version)
        var assistantMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = "assistant",
            Content = sanitizedResponse,
            DetectedModule = detectedModule,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            LatencyMs = (int)sw.ElapsedMilliseconds
        };
        await _sessionRepository.AddMessageAsync(assistantMessage, ct);

        // Update session
        session.LastActivityAt = DateTime.UtcNow;
        session.MessageCount += 2;
        session.LastModule = detectedModule;
        await _sessionRepository.UpdateAsync(session, ct);

        _logger.LogInformation(
            "SupportAgent response generated. Session={SessionId}, Module={Module}, Tokens={In}/{Out}, Latency={Latency}ms, Grounded={IsGrounded}, CacheHit={CacheHit}",
            sessionId, detectedModule, inputTokens, outputTokens,
            sw.ElapsedMilliseconds, groundingResult.IsGrounded, cachedFaqResponse != null);

        return new SupportChatResponse
        {
            SessionId = sessionId,
            Response = sanitizedResponse,
            DetectedModule = detectedModule,
            SuggestedActions = GetSuggestedActions(detectedModule),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns 2-3 contextual follow-up chips based on the detected conversation module.
    /// Keeps the conversation flowing without the user having to think of next questions.
    /// </summary>
    private static List<string> GetSuggestedActions(string detectedModule) => detectedModule switch
    {
        "soporte_tecnico" =>
        [
            "¿Cómo verifico mi KYC?",
            "¿Cuánto cuesta publicar?",
            "¿Cómo recupero mi contraseña?",
        ],
        "orientacion_comprador" =>
        [
            "¿Qué documentos debo pedir?",
            "¿Cómo evito estafas?",
            "¿Cómo verifico un vendedor?",
        ],
        "mixto" =>
        [
            "¿Qué documentos necesito?",
            "¿Cómo me registro como vendedor?",
        ],
        "conversacional" =>
        [
            "¿Cómo me registro?",
            "¿Cuánto cuesta publicar?",
            "¿Es seguro comprar en OKLA?",
        ],
        _ => []
    };

    /// <summary>
    /// Simple keyword-based module detection (pre-Claude classification).
    /// Claude also classifies internally via the system prompt, but this gives us
    /// metadata for logging and analytics.
    /// </summary>
    private static string DetectModule(string message)
    {
        var lower = message.ToLowerInvariant();

        // Buyer protection keywords
        var buyerKeywords = new[]
        {
            "estafa", "fraude", "confiable", "fiar", "seguro comprar", "documentos",
            "título", "titulo", "traspaso", "notario", "chasis", "vin", "mecánico",
            "mecanico", "hipoteca", "financiamiento", "dgii", "intrant", "marbete",
            "proconsumidor", "contrato", "compraventa", "negociar", "precio justo",
            "deposito antes", "depósito antes", "pide dinero", "pedir dinero",
            "enviar dinero", "envía dinero", "verificar vendedor", "comprar carro",
            "comprar vehiculo", "comprar vehículo", "ley 241", "ley 358",
            "reseñas vendedor", "badge verificación", "vendedor me pide",
            "contacto a un vendedor", "preguntar al vendedor", "qué le debo preguntar",
            "que le debo preguntar", "inspección", "inspeccion"
        };

        // Support technical keywords
        var supportKeywords = new[]
        {
            "registro", "registrar", "login", "contraseña", "password", "2fa",
            "verificación dos pasos", "kyc", "verificacion", "publicar", "publicación",
            "dealer", "concesionario", "plan", "precio plan", "suscripción", "suscripcion",
            "pago", "factura", "boost", "favoritos", "alertas", "mensajes", "mensajería",
            "cuenta", "perfil", "seguridad", "sesiones", "oauth", "google", "apple",
            "inventario", "empleados", "sucursales", "csv", "importar", "pwa", "app",
            "notificaciones", "busqueda", "búsqueda", "comparar", "seller"
        };

        var hasBuyerMatch = buyerKeywords.Any(k => lower.Contains(k));
        var hasSupportMatch = supportKeywords.Any(k => lower.Contains(k));

        if (hasBuyerMatch && hasSupportMatch)
            return "mixto";
        if (hasBuyerMatch)
            return "orientacion_comprador";
        if (hasSupportMatch)
            return "soporte_tecnico";

        // Greetings
        var greetings = new[] { "hola", "buenos días", "buenas tardes", "buenas noches", "qué tal", "hey", "saludos" };
        if (greetings.Any(g => lower.Contains(g)))
            return "conversacional";

        return "soporte_tecnico"; // Default to support
    }
}
