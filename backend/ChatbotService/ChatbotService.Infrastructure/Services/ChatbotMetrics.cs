using System.Diagnostics;
using System.Diagnostics.Metrics;
using ChatbotService.Application.Interfaces;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// .NET 8 Metrics for ChatbotService observability.
/// Exposes counters and histograms compatible with OpenTelemetry / Prometheus.
/// Implements IChatbotSafetyMetrics for Application-layer injection.
/// 
/// Usage:
///   builder.Services.AddSingleton<ChatbotMetrics>();
///   builder.Services.AddSingleton<IChatbotSafetyMetrics>(sp => sp.GetRequiredService<ChatbotMetrics>());
///   // Metrics are auto-collected by OpenTelemetry or Prometheus exporters.
/// </summary>
public sealed class ChatbotMetrics : IChatbotSafetyMetrics
{
    public static readonly string MeterName = "ChatbotService";

    private readonly Counter<long> _messagesReceived;
    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _llmCallsTotal;
    private readonly Counter<long> _llmCallsFailed;
    private readonly Counter<long> _quickResponseHits;
    private readonly Counter<long> _sessionsStarted;
    private readonly Counter<long> _sessionsEnded;
    private readonly Counter<long> _sessionsTransferred;
    private readonly Counter<long> _rateLimitRejections;
    private readonly Counter<long> _validationFailures;
    private readonly Histogram<double> _llmResponseTime;
    private readonly Histogram<double> _messageProcessingTime;
    private readonly Counter<long> _tokensConsumed;
    private readonly Counter<long> _interactionLimitReached;
    private readonly Counter<long> _circuitBreakerTrips;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;

    // ── Safety & Quality Metrics (LLM-as-a-Judge audit) ──────────────
    private readonly Counter<long> _hallucinationDetected;
    private readonly Counter<long> _groundingViolations;
    private readonly Counter<long> _moderationBlocked;
    private readonly Counter<long> _injectionDetected;
    private readonly Counter<long> _piiDetected;
    private readonly Histogram<double> _responseQualityScore;

    public ChatbotMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, "1.0.0");

        _messagesReceived = meter.CreateCounter<long>(
            "chatbot.messages.received",
            unit: "{message}",
            description: "Total chat messages received from users");

        _messagesProcessed = meter.CreateCounter<long>(
            "chatbot.messages.processed",
            unit: "{message}",
            description: "Total chat messages successfully processed");

        _llmCallsTotal = meter.CreateCounter<long>(
            "chatbot.llm.calls.total",
            unit: "{call}",
            description: "Total LLM inference calls made");

        _llmCallsFailed = meter.CreateCounter<long>(
            "chatbot.llm.calls.failed",
            unit: "{call}",
            description: "Failed LLM inference calls");

        _quickResponseHits = meter.CreateCounter<long>(
            "chatbot.quickresponse.hits",
            unit: "{hit}",
            description: "Quick response matches (bypass LLM)");

        _sessionsStarted = meter.CreateCounter<long>(
            "chatbot.sessions.started",
            unit: "{session}",
            description: "Total chat sessions started");

        _sessionsEnded = meter.CreateCounter<long>(
            "chatbot.sessions.ended",
            unit: "{session}",
            description: "Total chat sessions ended");

        _sessionsTransferred = meter.CreateCounter<long>(
            "chatbot.sessions.transferred",
            unit: "{session}",
            description: "Total sessions transferred to human agent");

        _rateLimitRejections = meter.CreateCounter<long>(
            "chatbot.ratelimit.rejections",
            unit: "{rejection}",
            description: "Requests rejected by rate limiter");

        _validationFailures = meter.CreateCounter<long>(
            "chatbot.validation.failures",
            unit: "{failure}",
            description: "Security validation failures (SQL injection, XSS)");

        _llmResponseTime = meter.CreateHistogram<double>(
            "chatbot.llm.response.duration",
            unit: "ms",
            description: "LLM inference response time in milliseconds");

        _messageProcessingTime = meter.CreateHistogram<double>(
            "chatbot.message.processing.duration",
            unit: "ms",
            description: "Total message processing time including LLM call");

        _tokensConsumed = meter.CreateCounter<long>(
            "chatbot.llm.tokens.consumed",
            unit: "{token}",
            description: "Total LLM tokens consumed");

        _interactionLimitReached = meter.CreateCounter<long>(
            "chatbot.interactions.limit_reached",
            unit: "{event}",
            description: "Times interaction limit was reached per session");

        _circuitBreakerTrips = meter.CreateCounter<long>(
            "chatbot.circuitbreaker.trips",
            unit: "{trip}",
            description: "Circuit breaker trip events");

        _cacheHits = meter.CreateCounter<long>(
            "chatbot.cache.hits",
            unit: "{hit}",
            description: "LLM response cache hits (R17)");

        _cacheMisses = meter.CreateCounter<long>(
            "chatbot.cache.misses",
            unit: "{miss}",
            description: "LLM response cache misses (R17)");

        // ── Safety & Quality Metrics ─────────────────────────────────

        _hallucinationDetected = meter.CreateCounter<long>(
            "chatbot.hallucination.detected",
            unit: "{event}",
            description: "Hallucination events detected by grounding validators");

        _groundingViolations = meter.CreateCounter<long>(
            "chatbot.grounding.violations",
            unit: "{violation}",
            description: "Ungrounded claims found in LLM responses (price, vehicle, phrase)");

        _moderationBlocked = meter.CreateCounter<long>(
            "chatbot.moderation.blocked",
            unit: "{event}",
            description: "Messages blocked or sanitized by content moderation filter");

        _injectionDetected = meter.CreateCounter<long>(
            "chatbot.injection.detected",
            unit: "{event}",
            description: "Prompt injection attempts detected (blocked + sanitized)");

        _piiDetected = meter.CreateCounter<long>(
            "chatbot.pii.detected",
            unit: "{event}",
            description: "PII detection events (cédula, credit card, phone, etc.)");

        _responseQualityScore = meter.CreateHistogram<double>(
            "chatbot.response.quality.score",
            unit: "{score}",
            description: "Response quality score (0.0-1.0) from grounding + moderation checks");
    }

    // ── Public Recording Methods ─────────────────────────────────────────────

    public void RecordMessageReceived(string channel = "web")
        => _messagesReceived.Add(1, new KeyValuePair<string, object?>("channel", channel));

    public void RecordMessageProcessed(string channel = "web", bool usedLlm = true)
        => _messagesProcessed.Add(1,
            new KeyValuePair<string, object?>("channel", channel),
            new KeyValuePair<string, object?>("used_llm", usedLlm));

    public void RecordLlmCall(bool success, double responseTimeMs, int tokensUsed = 0)
    {
        _llmCallsTotal.Add(1);
        _llmResponseTime.Record(responseTimeMs);

        if (!success)
            _llmCallsFailed.Add(1);

        if (tokensUsed > 0)
            _tokensConsumed.Add(tokensUsed);
    }

    public void RecordQuickResponseHit()
        => _quickResponseHits.Add(1);

    public void RecordSessionStarted(string channel = "web")
        => _sessionsStarted.Add(1, new KeyValuePair<string, object?>("channel", channel));

    public void RecordSessionEnded()
        => _sessionsEnded.Add(1);

    public void RecordSessionTransferred()
        => _sessionsTransferred.Add(1);

    public void RecordRateLimitRejection(string endpoint)
        => _rateLimitRejections.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint));

    public void RecordValidationFailure(string type)
        => _validationFailures.Add(1, new KeyValuePair<string, object?>("type", type));

    public void RecordMessageProcessingTime(double durationMs)
        => _messageProcessingTime.Record(durationMs);

    public void RecordInteractionLimitReached()
        => _interactionLimitReached.Add(1);

    public void RecordCircuitBreakerTrip()
        => _circuitBreakerTrips.Add(1);

    // R17 (MLOps): Cache metrics
    public void RecordCacheHit()
        => _cacheHits.Add(1);

    public void RecordCacheMiss()
        => _cacheMisses.Add(1);

    // ── Safety & Quality Recording Methods ───────────────────────────

    /// <summary>
    /// Records a hallucination event with type classification.
    /// Types: price_mismatch, ungrounded_phrase, fabricated_vehicle, url_fabrication
    /// </summary>
    public void RecordHallucinationDetected(string type)
        => _hallucinationDetected.Add(1, new KeyValuePair<string, object?>("type", type));

    /// <summary>
    /// Records individual grounding violations (each ungrounded claim counts).
    /// </summary>
    public void RecordGroundingViolation(string claimType)
        => _groundingViolations.Add(1, new KeyValuePair<string, object?>("claim_type", claimType));

    /// <summary>
    /// Records content moderation events with category (hate_speech, sexual, scam, etc.).
    /// </summary>
    public void RecordModerationBlocked(string category, string stage)
        => _moderationBlocked.Add(1,
            new KeyValuePair<string, object?>("category", category),
            new KeyValuePair<string, object?>("stage", stage));

    /// <summary>
    /// Records prompt injection detection events with severity.
    /// </summary>
    public void RecordInjectionDetected(string severity, bool blocked)
        => _injectionDetected.Add(1,
            new KeyValuePair<string, object?>("severity", severity),
            new KeyValuePair<string, object?>("blocked", blocked));

    /// <summary>
    /// Records PII detection events with the type of PII found.
    /// </summary>
    public void RecordPiiDetected(string piiType)
        => _piiDetected.Add(1, new KeyValuePair<string, object?>("pii_type", piiType));

    /// <summary>
    /// Records a response quality score (0.0 = hallucinated/blocked, 1.0 = fully grounded + clean).
    /// Score formula: starts at 1.0, deducted for grounding violations, moderation issues.
    /// </summary>
    public void RecordResponseQualityScore(double score, string chatMode)
        => _responseQualityScore.Record(score,
            new KeyValuePair<string, object?>("chat_mode", chatMode));
}
