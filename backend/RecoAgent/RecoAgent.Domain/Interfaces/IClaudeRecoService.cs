namespace RecoAgent.Domain.Interfaces;

/// <summary>
/// Interface for the Claude Sonnet 4.5 service that generates personalized vehicle recommendations.
/// Returns raw JSON string to avoid circular dependency between Domain and Application layers.
/// The Application handler deserializes the response into RecoAgentResponse.
/// </summary>
public interface IClaudeRecoService
{
    /// <summary>
    /// Sends a user profile and vehicle candidates to Claude Sonnet 4.5
    /// and returns the raw JSON response string for deserialization by the caller.
    /// </summary>
    Task<string> GenerateRecommendationsAsync(
        string userProfileJson,
        string systemPrompt,
        float temperature,
        int maxTokens,
        CancellationToken ct = default);
}
