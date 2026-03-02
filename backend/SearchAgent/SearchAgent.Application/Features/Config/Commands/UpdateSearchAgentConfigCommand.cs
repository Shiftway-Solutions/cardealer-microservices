using MediatR;
using SearchAgent.Application.DTOs;
using SearchAgent.Domain.Entities;

namespace SearchAgent.Application.Features.Config.Commands;

public record UpdateSearchAgentConfigCommand(
    UpdateSearchAgentConfigRequest ConfigUpdate,
    string UpdatedBy
) : IRequest<SearchAgentConfig>;
