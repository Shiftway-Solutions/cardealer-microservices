namespace SearchAgent.Domain.Interfaces;

using SearchAgent.Application.DTOs;

/// <summary>
/// Interface for the Claude AI service that processes natural language vehicle queries.
/// </summary>
public interface IClaudeSearchService
{
    /// <summary>
    /// Sends a user query to Claude Haiku 4.5 and returns structured search filters.
    /// </summary>
    Task<SearchAgentResponse> ProcessQueryAsync(string userQuery, string systemPrompt, float temperature, int maxTokens, CancellationToken ct = default);
}
