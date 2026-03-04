using MediatR;
using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.DTOs;
using ChatbotService.Application.Services;

namespace ChatbotService.Application.Features.Sessions.Commands;

/// <summary>
/// Handler para iniciar una nueva sesión de chat.
/// Soporta los modos SingleVehicle, DealerInventory y General.
/// </summary>
public class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, StartSessionResponse>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatbotConfigurationRepository _configRepository;
    private readonly ILogger<StartSessionCommandHandler> _logger;

    public StartSessionCommandHandler(
        IChatSessionRepository sessionRepository,
        IChatbotConfigurationRepository configRepository,
        ILogger<StartSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _configRepository = configRepository;
        _logger = logger;
    }

    public async Task<StartSessionResponse> Handle(StartSessionCommand request, CancellationToken ct)
    {
        // Obtener configuración del chatbot.
        // Si hay un dealerId, busca config específica del dealer.
        // Si no se encuentra (dealer no configurado), cae en la config global/default.
        ChatbotConfiguration? config = null;
        if (request.DealerId.HasValue)
        {
            config = await _configRepository.GetByDealerIdAsync(request.DealerId.Value, ct)
                  ?? await _configRepository.GetDefaultAsync(ct);
        }
        else
        {
            config = await _configRepository.GetDefaultAsync(ct);
        }

        if (config == null)
        {
            throw new InvalidOperationException("No chatbot configuration found");
        }

        // Determinar el modo de chat
        var chatMode = ParseChatMode(request.ChatMode, request.VehicleId, request.DealerId);
        
        // Crear nueva sesión
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            ChatbotConfigurationId = config.Id,
            SessionToken = Guid.NewGuid().ToString("N"),
            UserId = request.UserId,
            UserName = request.UserName,
            UserEmail = request.UserEmail,
            UserPhone = request.UserPhone,
            SessionType = Enum.TryParse<SessionType>(request.SessionType, out var st) ? st : SessionType.WebChat,
            Channel = request.Channel ?? "web",
            ChannelUserId = request.ChannelUserId,
            Status = SessionStatus.Active,
            ChatMode = chatMode,
            VehicleId = request.VehicleId,
            DealerId = request.DealerId ?? config.DealerId,
            HandoffStatus = HandoffStatus.BotActive,
            MessageCount = 0,
            InteractionCount = 0,
            MaxInteractionsPerSession = config.MaxInteractionsPerSession,
            InteractionLimitReached = false,
            Language = request.Language ?? "es",
            UserAgent = request.UserAgent,
            IpAddress = request.IpAddress,
            DeviceType = request.DeviceType,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        await _sessionRepository.CreateAsync(session, ct);
        
        _logger.LogInformation(
            "Created chat session {SessionId} — Mode: {ChatMode}, Vehicle: {VehicleId}, Dealer: {DealerId}",
            session.Id, chatMode, request.VehicleId, request.DealerId);

        // Personalizar mensaje de bienvenida según el modo
        var welcomeMessage = chatMode switch
        {
            ChatMode.SingleVehicle => config.WelcomeMessage ?? 
                "¡Hola! 👋 Soy tu asistente virtual. ¿Qué te gustaría saber sobre este vehículo?",
            ChatMode.DealerInventory => config.WelcomeMessage ?? 
                "¡Hola! 👋 Soy tu asistente virtual. Tengo acceso a todo el inventario del dealer. " +
                "¿Buscas algo en particular? Puedo buscar, comparar y recomendarte vehículos.",
            _ => config.WelcomeMessage
        };

        return new StartSessionResponse
        {
            SessionId = session.Id,
            SessionToken = session.SessionToken,
            WelcomeMessage = welcomeMessage,
            // When using the default/global fallback config (DealerId == null but request has a real dealerId),
            // return an empty BotName so the frontend can show its per-dealer fallback:
            //   displayName = chat.botName || `Asistente de ${dealerName}`
            BotName = (request.DealerId.HasValue && config.DealerId == null)
                ? ""
                : (config.BotName ?? ""),
            BotAvatarUrl = config.BotAvatarUrl,
            MaxInteractionsPerSession = config.MaxInteractionsPerSession,
            RemainingInteractions = config.MaxInteractionsPerSession,
            ChatMode = chatMode.ToString()
        };
    }

    private static ChatMode ParseChatMode(string? mode, Guid? vehicleId, Guid? dealerId)
    {
        if (!string.IsNullOrEmpty(mode))
        {
            return mode.ToLowerInvariant() switch
            {
                "single_vehicle" => ChatMode.SingleVehicle,
                "singlevehicle" => ChatMode.SingleVehicle,
                "dealer_inventory" => ChatMode.DealerInventory,
                "dealerinventory" => ChatMode.DealerInventory,
                "general" => ChatMode.General,
                _ => ChatMode.General
            };
        }

        // Auto-detect basado en los parámetros
        if (vehicleId.HasValue) return ChatMode.SingleVehicle;
        if (dealerId.HasValue) return ChatMode.DealerInventory;
        return ChatMode.General;
    }
}

/// <summary>
/// Handler para enviar un mensaje al LLM.
/// REDISEÑADO: Usa Strategy Pattern para seleccionar el modo de contexto.
/// - SingleVehicle: contexto fijo de 1 vehículo
/// - DealerInventory: RAG con pgvector + function calling
/// - General: FAQ y soporte general
/// </summary>
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatbotResponse>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatbotConfigurationRepository _configRepository;
    private readonly IQuickResponseRepository _quickResponseRepository;
    private readonly IChatModeStrategyFactory _strategyFactory;
    private readonly ILlmService _llmService;
    private readonly ILlmResponseCacheService _cacheService;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IChatbotConfigurationRepository configRepository,
        IQuickResponseRepository quickResponseRepository,
        IChatModeStrategyFactory strategyFactory,
        ILlmService llmService,
        ILlmResponseCacheService cacheService,
        ILogger<SendMessageCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _configRepository = configRepository;
        _quickResponseRepository = quickResponseRepository;
        _strategyFactory = strategyFactory;
        _llmService = llmService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ChatbotResponse> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        // ── 1. Cargar sesión y configuración ──────────────────────────
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct)
            ?? throw new InvalidOperationException("Session not found");

        var config = await _configRepository.GetByIdAsync(session.ChatbotConfigurationId, ct)
            ?? await _configRepository.GetDefaultAsync(ct)
            ?? throw new InvalidOperationException("No chatbot configuration found");

        // ── 2. Verificar handoff: si un humano tiene control, no procesar con bot ──
        if (!session.IsBotActive)
        {
            // Guardar mensaje del usuario pero no responder con bot
            var humanModeMsg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Type = Enum.TryParse<MessageType>(request.MessageType, out var mt2) ? mt2 : MessageType.UserText,
                Content = request.Message,
                IsFromBot = false,
                ConsumedInteraction = false,
                CreatedAt = DateTime.UtcNow
            };
            await _messageRepository.CreateAsync(humanModeMsg, ct);
            session.MessageCount++;
            session.LastActivityAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session, ct);

            return new ChatbotResponse
            {
                MessageId = humanModeMsg.Id,
                Response = "",
                IsFallback = false,
                IsHumanMode = true,
                RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        // ── 3. Verificar límite de interacciones ──────────────────────
        if (session.InteractionLimitReached)
        {
            return new ChatbotResponse
            {
                MessageId = Guid.NewGuid(),
                Response = "Has alcanzado el límite de interacciones para esta sesión. Por favor, contacta con un agente.",
                IsFallback = true,
                RemainingInteractions = 0,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        // ── 4. SEGURIDAD: Prompt injection detection ──────────────────
        var injectionResult = PromptInjectionDetector.Detect(request.Message);
        if (injectionResult.ShouldBlock)
        {
            _logger.LogWarning("Prompt injection BLOCKED in session {SessionId}: {Patterns}",
                session.Id, string.Join(", ", injectionResult.DetectedPatterns));

            return new ChatbotResponse
            {
                MessageId = Guid.NewGuid(),
                Response = "Lo siento, no puedo procesar ese mensaje. ¿En qué más puedo ayudarte con nuestros vehículos? 🚗",
                IsFallback = true,
                RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        var sanitizedMessage = injectionResult.ThreatLevel >= ThreatLevel.Medium
            ? PromptInjectionDetector.Sanitize(request.Message)
            : request.Message;

        // ── 5. SEGURIDAD: PII detection ───────────────────────────────
        var piiResult = PiiDetector.Sanitize(sanitizedMessage);
        if (piiResult.DetectionInfo.RequiresAgentTransfer)
        {
            _logger.LogWarning("PII requiring agent transfer in session {SessionId}: {Types}",
                session.Id, string.Join(", ", piiResult.DetectionInfo.DetectedTypes));

            return new ChatbotResponse
            {
                MessageId = Guid.NewGuid(),
                Response = "Por tu seguridad, no compartas datos de tarjetas de crédito por el chat. " +
                    "Un agente humano puede ayudarte con pagos de forma segura. 🔒",
                IsFallback = false,
                IntentName = "pii_protection",
                ConfidenceScore = 1.0m,
                RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        var messageForLlm = piiResult.SanitizedMessage;

        // ── 5b. SEGURIDAD: Content moderation (pre-LLM) ──────────────
        var inputModeration = ContentModerationFilter.ModerateUserMessage(messageForLlm);
        if (!inputModeration.IsSafe)
        {
            _logger.LogWarning(
                "Content moderation BLOCKED user input. Category={Category}, Reason={Reason}, Session={SessionId}",
                inputModeration.Category, inputModeration.Reason, session.Id);

            return new ChatbotResponse
            {
                MessageId = Guid.NewGuid(),
                Response = inputModeration.SuggestedAction
                    ?? "No puedo procesar ese tipo de mensaje. ¿Hay algo relacionado con el vehículo en lo que pueda ayudarte? 😊",
                IsFallback = false,
                IntentName = "content_moderation",
                ConfidenceScore = 1.0m,
                RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount,
                ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        // ── 6. Quick Response match (bypass LLM, sin costo) ──────────
        var quickResponse = await _quickResponseRepository.FindMatchingAsync(
            config.Id, request.Message, ct);

        string botResponse;
        string? intentName = null;
        IntentCategory intentCategory = IntentCategory.Other;
        decimal confidenceScore = 0;
        bool isFallback = false;
        bool consumedInteraction = false;
        
        // DealerChatAgent intent scoring fields
        int intentScore = 1;
        string clasificacion = "curioso";
        string moduloActivo = "qa";
        bool handoffActivado = false;

        if (quickResponse != null)
        {
            botResponse = quickResponse.Response;
            intentName = quickResponse.Name;
            confidenceScore = 1.0m;
            _logger.LogInformation("Quick response matched: {Name}", quickResponse.Name);
        }
        else
        {
            // ── 7. STRATEGY PATTERN: Construir system prompt según el modo ──
            var strategy = _strategyFactory.GetStrategy(session.ChatMode);
            
            var systemPrompt = await strategy.BuildSystemPromptAsync(
                session, config, messageForLlm, ct);

            _logger.LogInformation(
                "Using {Mode} strategy for session {SessionId}, prompt length: {Length}",
                session.ChatMode, session.Id, systemPrompt.Length);

            // ── 7b. CACHE CHECK: Avoid redundant LLM calls ───────────
            var cachedResponse = await _cacheService.GetAsync(messageForLlm, systemPrompt, ct);
            if (cachedResponse != null)
            {
                botResponse = cachedResponse.Response;
                intentName = cachedResponse.Intent;
                confidenceScore = (decimal)cachedResponse.Confidence;
                isFallback = false;
                consumedInteraction = false; // Cache hits don't consume interactions

                _logger.LogInformation(
                    "Cache HIT for session {SessionId}, saved LLM call. Intent: {Intent}",
                    session.Id, intentName);
            }
            else
            {
            // ── 8. Llamar al LLM ─────────────────────────────────────
            var llmResult = await _llmService.GenerateResponseAsync(
                session.SessionToken,
                messageForLlm,
                session.Language ?? "es",
                systemPrompt,
                ct);

            // ── 9. Sanitizar respuesta del LLM (anti-PII echo-back) ──
            botResponse = PiiDetector.SanitizeResponse(
                llmResult.FulfillmentText ?? "Lo siento, no entendí tu mensaje.");
            intentName = llmResult.DetectedIntent;
            confidenceScore = (decimal)llmResult.ConfidenceScore;
            isFallback = llmResult.IsFallback;
            consumedInteraction = true;

            // ── 9b. CACHE SET: Store response for future hits ─────────
            _ = _cacheService.SetAsync(
                messageForLlm, botResponse, intentName,
                llmResult.ConfidenceScore, isFallback, systemPrompt, ct: ct);

            // ── 10. Grounding validation (anti-hallucination) ─────────
            var groundingResult = await strategy.ValidateResponseGroundingAsync(
                session, botResponse, ct);

            if (!groundingResult.IsGrounded && groundingResult.SanitizedResponse != null)
            {
                _logger.LogWarning("Response not grounded in session {SessionId}: {Claims}",
                    session.Id, string.Join(", ", groundingResult.UngroundedClaims));
                botResponse = groundingResult.SanitizedResponse;
            }

            // ── 10b. SEGURIDAD: Content moderation on bot output ──────
            var outputModeration = ContentModerationFilter.ModerateBotResponse(botResponse);
            if (!outputModeration.IsSafe)
            {
                _logger.LogWarning(
                    "Bot output moderated. Category={Category}, Reason={Reason}, Session={SessionId}",
                    outputModeration.Category, outputModeration.Reason, session.Id);
                botResponse = outputModeration.SuggestedAction
                    ?? "¿Hay algo más sobre el vehículo en lo que pueda ayudarte?";
            }

            // ── 10c. DealerChatAgent: capture intent scoring fields ───
            intentScore = llmResult.IntentScore;
            clasificacion = llmResult.Clasificacion;
            moduloActivo = llmResult.ModuloActivo;
            handoffActivado = llmResult.HandoffActivado;

            // ── 10d. Auto-trigger handoff if Claude activated it ──────
            if (llmResult.HandoffActivado && session.IsBotActive)
            {
                session.HandoffStatus = HandoffStatus.PendingHuman;
                session.HandoffReason = llmResult.RazonHandoff ?? "Solicitado por el usuario";
                _logger.LogInformation(
                    "DealerChatAgent triggered handoff in session {SessionId}: {Reason}",
                    session.Id, session.HandoffReason);
            }

            _logger.LogInformation(
                "DealerChatAgent response — Session: {SessionId}, IntentScore: {Score}, " +
                "Clasificacion: {Clasificacion}, Modulo: {Modulo}, Handoff: {Handoff}",
                session.Id, llmResult.IntentScore, llmResult.Clasificacion,
                llmResult.ModuloActivo, llmResult.HandoffActivado);
            } // end of cache-miss else block

            // ── 11. Incrementar contadores ────────────────────────────
            session.InteractionCount++;
            if (session.InteractionCount >= session.MaxInteractionsPerSession)
            {
                session.InteractionLimitReached = true;
            }
        }

        // ── 12. Persistir mensajes ────────────────────────────────────
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Type = Enum.TryParse<MessageType>(request.MessageType, out var mt) ? mt : MessageType.UserText,
            Content = request.Message,
            MediaUrl = request.MediaUrl,
            IsFromBot = false,
            ConsumedInteraction = false,
            CreatedAt = DateTime.UtcNow
        };

        var botMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Type = MessageType.BotText,
            Content = request.Message,
            BotResponse = botResponse,
            IntentName = intentName,
            IntentCategory = intentCategory,
            ConfidenceScore = confidenceScore,
            IsFromBot = true,
            ConsumedInteraction = consumedInteraction,
            InteractionCost = consumedInteraction ? 0.003m : 0m, // Claude Sonnet cost per interaction
            ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.CreateAsync(userMessage, ct);
        await _messageRepository.CreateAsync(botMessage, ct);

        session.MessageCount += 2;
        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, ct);

        return new ChatbotResponse
        {
            MessageId = botMessage.Id,
            Response = botResponse,
            IntentName = intentName,
            IntentCategory = intentCategory,
            ConfidenceScore = confidenceScore,
            IsFallback = isFallback,
            ChatMode = session.ChatMode.ToString(),
            IntentScore = intentScore,
            Clasificacion = clasificacion,
            ModuloActivo = moduloActivo,
            HandoffActivado = handoffActivado,
            RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount,
            ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }
}

/// <summary>
/// Handler para finalizar una sesión
/// </summary>
public class EndSessionCommandHandler : IRequestHandler<EndSessionCommand, bool>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ILogger<EndSessionCommandHandler> _logger;

    public EndSessionCommandHandler(
        IChatSessionRepository sessionRepository,
        ILogger<EndSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(EndSessionCommand request, CancellationToken ct)
    {
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);
        if (session == null) return false;

        session.Status = SessionStatus.Completed;
        session.EndedAt = DateTime.UtcNow;
        session.SessionDurationSeconds = (int)(DateTime.UtcNow - session.CreatedAt).TotalSeconds;

        await _sessionRepository.UpdateAsync(session, ct);
        _logger.LogInformation("Session {SessionId} ended (mode: {Mode})", session.Id, session.ChatMode);

        return true;
    }
}

/// <summary>
/// Handler para transferir a agente humano
/// </summary>
public class TransferToAgentCommandHandler : IRequestHandler<TransferToAgentCommand, TransferToAgentResult>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatLeadRepository _leadRepository;
    private readonly ILogger<TransferToAgentCommandHandler> _logger;

    public TransferToAgentCommandHandler(
        IChatSessionRepository sessionRepository,
        IChatLeadRepository leadRepository,
        ILogger<TransferToAgentCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _leadRepository = leadRepository;
        _logger = logger;
    }

    public async Task<TransferToAgentResult> Handle(TransferToAgentCommand request, CancellationToken ct)
    {
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);
        if (session == null)
        {
            return new TransferToAgentResult(false, null, "Session not found", null);
        }

        session.Status = SessionStatus.TransferredToAgent;
        session.HandoffStatus = HandoffStatus.PendingHuman;
        session.HandoffReason = request.Reason;
        await _sessionRepository.UpdateAsync(session, ct);

        // Crear lead si hay datos de contacto
        if (!string.IsNullOrEmpty(session.UserPhone) || !string.IsNullOrEmpty(session.UserEmail))
        {
            var lead = new ChatLead
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                FullName = session.UserName ?? "Unknown",
                Email = session.UserEmail,
                Phone = session.UserPhone,
                PreferredContactMethod = "phone",
                Status = LeadStatus.New,
                Temperature = LeadTemperature.Warm,
                Notes = request.Reason,
                CreatedAt = DateTime.UtcNow
            };
            await _leadRepository.CreateAsync(lead, ct);
        }

        _logger.LogInformation("Session {SessionId} transferred to agent (mode: {Mode})",
            session.Id, session.ChatMode);
        
        return new TransferToAgentResult(
            true, null,
            "Your request has been transferred to a human agent. Please wait.",
            5);
    }
}

/// <summary>
/// Handler para que un dealer tome control de una sesión (handoff bot→humano)
/// </summary>
public class TakeOverSessionCommandHandler : IRequestHandler<TakeOverSessionCommand, HandoffResult>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ILogger<TakeOverSessionCommandHandler> _logger;

    public TakeOverSessionCommandHandler(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        ILogger<TakeOverSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<HandoffResult> Handle(TakeOverSessionCommand request, CancellationToken ct)
    {
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);
        if (session == null)
        {
            return new HandoffResult(false, "Session not found", HandoffStatus.BotActive);
        }

        session.HandoffStatus = HandoffStatus.HumanActive;
        session.HandoffAgentId = request.AgentId;
        session.HandoffAgentName = request.AgentName;
        session.HandoffAt = DateTime.UtcNow;
        session.HandoffReason = request.Reason;
        session.Status = SessionStatus.HumanTakeover;

        await _sessionRepository.UpdateAsync(session, ct);

        // Agregar mensaje de sistema informando al usuario
        var systemMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Type = MessageType.SystemMessage,
            Content = $"Un asesor ({request.AgentName}) se ha unido a la conversación.",
            BotResponse = $"Un asesor ({request.AgentName}) se ha unido a la conversación. Ahora estás hablando con un humano. 👤",
            IsFromBot = true,
            ConsumedInteraction = false,
            CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.CreateAsync(systemMsg, ct);

        _logger.LogInformation(
            "Handoff bot→human: Session {SessionId}, Agent: {AgentName} ({AgentId})",
            session.Id, request.AgentName, request.AgentId);

        return new HandoffResult(true,
            $"Agente {request.AgentName} tomó control de la conversación",
            HandoffStatus.HumanActive);
    }
}

/// <summary>
/// Handler para devolver control al bot (handoff humano→bot)
/// </summary>
public class ReturnToBotCommandHandler : IRequestHandler<ReturnToBotCommand, HandoffResult>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ILogger<ReturnToBotCommandHandler> _logger;

    public ReturnToBotCommandHandler(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        ILogger<ReturnToBotCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<HandoffResult> Handle(ReturnToBotCommand request, CancellationToken ct)
    {
        var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);
        if (session == null)
        {
            return new HandoffResult(false, "Session not found", HandoffStatus.BotActive);
        }

        var previousAgent = session.HandoffAgentName;
        session.HandoffStatus = HandoffStatus.ReturnedToBot;
        session.HandoffAgentId = null;
        session.HandoffAgentName = null;
        session.Status = SessionStatus.Active;

        await _sessionRepository.UpdateAsync(session, ct);

        var systemMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Type = MessageType.SystemMessage,
            Content = "El asesor ha finalizado. El asistente virtual retoma la conversación.",
            BotResponse = "El asesor ha finalizado. Soy tu asistente virtual de nuevo. ¿En qué más puedo ayudarte? 🤖",
            IsFromBot = true,
            ConsumedInteraction = false,
            CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.CreateAsync(systemMsg, ct);

        _logger.LogInformation("Handoff human→bot: Session {SessionId}, previous agent: {Agent}",
            session.Id, previousAgent);

        return new HandoffResult(true,
            "Control devuelto al bot",
            HandoffStatus.ReturnedToBot);
    }
}
