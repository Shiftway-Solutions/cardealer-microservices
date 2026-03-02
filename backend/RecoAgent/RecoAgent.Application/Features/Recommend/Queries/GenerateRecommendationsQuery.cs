using MediatR;
using RecoAgent.Application.DTOs;

namespace RecoAgent.Application.Features.Recommend.Queries;

/// <summary>
/// Query to generate vehicle recommendations for a user profile.
/// </summary>
public record GenerateRecommendationsQuery(
    RecoAgentRequest Request,
    string? UserId,
    string? IpAddress
) : IRequest<RecoAgentResultDto>;
