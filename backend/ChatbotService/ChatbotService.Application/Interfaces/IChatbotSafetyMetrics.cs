namespace ChatbotService.Application.Interfaces;

/// <summary>
/// Interface for recording safety and quality metrics in the chatbot pipeline.
/// Implemented by ChatbotMetrics in the Infrastructure layer.
/// 
/// Metrics exposed:
/// - Hallucination detection events
/// - Grounding violations (ungrounded claims)
/// - Content moderation blocks
/// - Prompt injection attempts
/// - PII detection events
/// - Response quality scores (0.0-1.0)
/// </summary>
public interface IChatbotSafetyMetrics
{
    /// <summary>
    /// Records a hallucination event with type classification.
    /// Types: price_mismatch, ungrounded_phrase, fabricated_vehicle, url_fabrication, cached_response, fresh_response
    /// </summary>
    void RecordHallucinationDetected(string type);

    /// <summary>
    /// Records individual grounding violations (each ungrounded claim counts).
    /// </summary>
    void RecordGroundingViolation(string claimType);

    /// <summary>
    /// Records content moderation events with category and pipeline stage.
    /// Categories: hate_speech, sexual, scam, violence, off_topic, profanity, discrimination, etc.
    /// Stages: pre_llm, post_llm, cached_output
    /// </summary>
    void RecordModerationBlocked(string category, string stage);

    /// <summary>
    /// Records prompt injection detection events with severity and whether it was blocked.
    /// Severities: low, medium, high
    /// </summary>
    void RecordInjectionDetected(string severity, bool blocked);

    /// <summary>
    /// Records PII detection events with the type of PII found.
    /// Types: cedula, credit_card, phone, email, rnc, etc.
    /// </summary>
    void RecordPiiDetected(string piiType);

    /// <summary>
    /// Records a response quality score (0.0 = hallucinated/blocked, 1.0 = fully grounded + clean).
    /// </summary>
    void RecordResponseQualityScore(double score, string chatMode);
}
