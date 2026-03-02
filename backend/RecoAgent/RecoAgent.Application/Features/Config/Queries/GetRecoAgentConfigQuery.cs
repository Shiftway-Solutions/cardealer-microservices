using MediatR;
using RecoAgent.Domain.Entities;

namespace RecoAgent.Application.Features.Config.Queries;

public record GetRecoAgentConfigQuery() : IRequest<RecoAgentConfig>;
