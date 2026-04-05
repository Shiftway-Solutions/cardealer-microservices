using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using ChatbotService.Application.DTOs;
using ChatbotService.Application.Features.Sessions.Commands;
using ChatbotService.Domain.Interfaces;
namespace ChatbotService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IPromptCacheStats _promptCacheStats;
    private readonly IChatbotConfigurationRepository _configRepository;

    public ChatController(
        IMediator mediator,
        ILogger<ChatController> logger,
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IPromptCacheStats promptCacheStats,
        IChatbotConfigurationRepository configRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _promptCacheStats = promptCacheStats;
        _configRepository = configRepository;
    }

    /// <summary>
    /// Start a new chat session
    /// </summary>
    [HttpPost("start")]
    [AllowAnonymous]
    [EnableRateLimiting("SessionStart")]
    [ProducesResponseType(typeof(StartSessionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new StartSessionCommand(
                request.UserId,
                request.UserName,
                request.UserEmail,
                request.UserPhone,
                request.SessionType.ToString(),
                request.Channel,
                request.ChannelUserId,
                request.UserAgent,
                request.IpAddress,
                request.DeviceType,
                request.Language ?? "es",
                request.DealerId,
                request.ChatMode,
                request.VehicleId,
                request.DealerName);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lightweight probe: returns isAiEnabled and botName for a dealer without starting a session.
    /// Used by vehicle detail page to conditionally show the AI chat button in the seller card.
    /// </summary>
    [HttpGet("dealer-status/{dealerId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetDealerChatStatus(Guid dealerId, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configRepository.GetByDealerIdAsync(dealerId, cancellationToken)
                      ?? await _configRepository.GetDefaultAsync(cancellationToken);

            if (config == null)
            {
                return Ok(new { isAiEnabled = false, botName = string.Empty });
            }

            // Same plan gate logic as StartSession: AI is enabled only when dealer has their own config
            bool isAiEnabled = config.EnableWebChat && config.DealerId.HasValue;

            return Ok(new
            {
                isAiEnabled,
                botName = isAiEnabled ? (config.BotName ?? string.Empty) : string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking dealer chat status for {DealerId}", dealerId);
            return Ok(new { isAiEnabled = false, botName = string.Empty });
        }
    }

    /// <summary>
    /// Send a message to the chatbot
    /// </summary>
    [HttpPost("message")]
    [AllowAnonymous]
    [EnableRateLimiting("ChatMessage")]
    [ProducesResponseType(typeof(ChatbotResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new SendMessageCommand(
                request.SessionToken,
                request.Message,
                request.Type.ToString(),
                request.MediaUrl);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation sending message");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Accept bot disclosure and privacy policy.
    /// Must be called after StartSession and before the first SendMessage.
    /// </summary>
    [HttpPost("accept-disclosure")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AcceptDisclosureResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AcceptDisclosure([FromBody] AcceptDisclosureRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new AcceptDisclosureCommand(request.SessionToken);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return NotFound(new { error = result.Message });
            }

            return Ok(new AcceptDisclosureResponse
            {
                Success = result.Success,
                Message = result.Message,
                ConsentAcceptedAt = result.ConsentAcceptedAt ?? DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting disclosure");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// End a chat session
    /// </summary>
    [HttpPost("end")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new EndSessionCommand(request.SessionToken, request.EndReason);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Session not found" });
            }

            return Ok(new { message = "Session ended successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Transfer session to a human agent
    /// </summary>
    [HttpPost("transfer")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TransferToAgent([FromBody] TransferToAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new TransferToAgentCommand(request.SessionToken, request.TransferReason, null);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return NotFound(new { error = "Session not found or transfer failed" });
            }

            return Ok(new { message = "Session transferred to agent", result.AgentName, result.EstimatedWaitTimeMinutes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring to agent");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get session by token
    /// </summary>
    [HttpGet("session")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ChatSessionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSessionByToken([FromQuery] string token, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionRepository.GetByTokenAsync(token, cancellationToken);
            if (session == null)
            {
                return NotFound(new { error = "Session not found" });
            }

            var dto = new ChatSessionDto
            {
                Id = session.Id,
                SessionToken = session.SessionToken,
                UserId = session.UserId,
                UserName = session.UserName,
                UserEmail = session.UserEmail,
                SessionType = session.SessionType,
                Channel = session.Channel,
                Status = session.Status,
                MessageCount = session.MessageCount,
                InteractionCount = session.InteractionCount,
                MaxInteractionsPerSession = session.MaxInteractionsPerSession,
                InteractionLimitReached = session.InteractionLimitReached,
                CreatedAt = session.CreatedAt,
                LastActivityAt = session.LastActivityAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get messages for a session
    /// </summary>
    [HttpGet("session/{sessionToken}/messages")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), 200)]
    public async Task<IActionResult> GetSessionMessages(string sessionToken, CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _messageRepository.GetBySessionTokenAsync(sessionToken, cancellationToken);

            var dtos = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SessionId = m.SessionId,
                Type = m.Type,
                Content = m.Content,
                MediaUrl = m.MediaUrl,
                IntentName = m.IntentName,
                ConfidenceScore = m.ConfidenceScore,
                IsFromBot = m.IsFromBot,
                ResponseTimeMs = m.ResponseTimeMs,
                ConsumedInteraction = m.ConsumedInteraction,
                CreatedAt = m.CreatedAt
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session messages");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get active sessions count
    /// </summary>
    [HttpGet("sessions/active/count")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> GetActiveSessionsCount(CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _sessionRepository.GetActiveSessionsAsync(cancellationToken);
            return Ok(new { count = sessions.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions count");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get this month's conversation count for a specific dealer.
    /// Used by the dealer ChatAgent dashboard to display usage vs plan limit.
    /// Does not require Admin role — any caller can query a dealer's count (non-sensitive data).
    /// </summary>
    [HttpGet("dealer-session-count/{dealerId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetDealerSessionCount(Guid dealerId, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configRepository.GetByDealerIdAsync(dealerId, cancellationToken);
            if (config == null)
            {
                // Dealer has no dedicated config (FREE plan) → no AI sessions
                return Ok(new { count = 0 });
            }

            var now = DateTime.UtcNow;
            var count = await _sessionRepository.GetMonthSessionCountAsync(config.Id, now.Year, now.Month, cancellationToken);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dealer session count for {DealerId}", dealerId);
            return Ok(new { count = 0 });
        }
    }

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    // ═══════════════════════════════════════════════════════════════
    // HANDOFF: Bot ↔ Human endpoints
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Dealer takes over a chat session (bot→human handoff)
    /// </summary>
    [HttpPost("handoff/takeover")]
    [Authorize(Roles = "Dealer,Admin")]
    [ProducesResponseType(typeof(HandoffResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TakeOverSession([FromBody] TakeOverRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.AgentId, out var agentGuid))
                return BadRequest(new { error = "AgentId must be a valid GUID" });

            var command = new TakeOverSessionCommand(
                request.SessionToken,
                agentGuid,
                request.AgentName,
                request.Reason);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
                return NotFound(new { error = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in handoff takeover");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Return control to bot (human→bot handoff)
    /// </summary>
    [HttpPost("handoff/return-to-bot")]
    [Authorize(Roles = "Dealer,Admin")]
    [ProducesResponseType(typeof(HandoffResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReturnToBot([FromBody] ReturnToBotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ReturnToBotCommand(request.SessionToken);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
                return NotFound(new { error = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning to bot");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// R17-PC: Anthropic Prompt Caching metrics.
    /// Returns accumulated token usage and estimated cost savings vs the >60% target.
    ///
    /// How it works:
    ///   - Static system prompt (personality + rules + JSON schema, ≥1,024 tokens) is cached
    ///     server-side by Anthropic on first request and reused across ALL dealers.
    ///   - cache_read_tokens = tokens served from cache at 10% of base price (90% savings).
    ///   - cache_write_tokens = tokens written to cache at 125% of base price (25% premium).
    ///   - estimated_savings_percent ≥ 60% → target met.
    /// </summary>
    [HttpGet("metrics/prompt-cache")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public IActionResult GetPromptCacheMetrics()
    {
        var report = _promptCacheStats.GetReport();

        if (report == null)
        {
            return NotFound(new { message = "PromptCache metrics not available." });
        }

        return Ok(new
        {
            totalLlmCalls = report.TotalLlmCalls,
            totalInputTokens = report.TotalInputTokens,
            cacheReadTokens = report.CacheReadTokens,
            cacheWriteTokens = report.CacheWriteTokens,
            cacheHitRatePercent = report.CacheHitRatePercent,
            estimatedSavingsPercent = report.EstimatedSavingsPercent,
            targetPercent = report.TargetPercent,
            targetMet = report.TargetMet,
            lastCallAt = report.LastCallAt,
            measuredAt = report.MeasuredAt,
            status = report.TargetMet
                ? "✅ Target met — Anthropic Prompt Caching is reducing costs by ≥60%"
                : report.TotalLlmCalls == 0
                    ? "⏳ No LLM calls recorded yet. Call /api/chat/send to accumulate stats."
                    : $"⚠️ Below target — {report.EstimatedSavingsPercent:F1}% savings, need ≥60%. Check CACHE_BREAK marker in system prompts."
        });
    }
}
