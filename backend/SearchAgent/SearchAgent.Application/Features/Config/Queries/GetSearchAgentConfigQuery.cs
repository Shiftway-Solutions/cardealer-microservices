using MediatR;
using SearchAgent.Application.DTOs;
using SearchAgent.Domain.Entities;

namespace SearchAgent.Application.Features.Config.Queries;

public record GetSearchAgentConfigQuery : IRequest<SearchAgentConfig>;
