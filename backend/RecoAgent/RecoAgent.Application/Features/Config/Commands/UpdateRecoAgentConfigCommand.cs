using MediatR;
using RecoAgent.Application.DTOs;
using RecoAgent.Domain.Entities;

namespace RecoAgent.Application.Features.Config.Commands;

public record UpdateRecoAgentConfigCommand(
    UpdateRecoAgentConfigRequest ConfigUpdate,
    string UpdatedBy
) : IRequest<RecoAgentConfig>;
