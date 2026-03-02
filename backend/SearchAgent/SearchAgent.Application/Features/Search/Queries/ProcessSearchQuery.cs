using MediatR;
using SearchAgent.Application.DTOs;

namespace SearchAgent.Application.Features.Search.Queries;

/// <summary>
/// Query to process a natural language vehicle search via Claude AI.
/// Implements CQRS pattern via MediatR.
/// </summary>
public record ProcessSearchQuery(
    string Query,
    string? SessionId,
    int Page,
    int PageSize,
    string? UserId,
    string? IpAddress
) : IRequest<SearchAgentResultDto>;
