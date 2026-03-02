using System.ComponentModel.DataAnnotations;

namespace SearchAgent.Application.DTOs;

/// <summary>
/// Request DTO for the AI-powered vehicle search endpoint.
/// </summary>
public class SearchAgentRequest
{
    /// <summary>
    /// Natural language query from the user (e.g., "busco toyota 2020 automática")
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Optional session ID for multi-turn search context
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Page number for paginated results (default: 1)
    /// </summary>
    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Results per page (default: 20, min: 8, max: 40)
    /// </summary>
    [Range(8, 40)]
    public int PageSize { get; set; } = 20;
}
