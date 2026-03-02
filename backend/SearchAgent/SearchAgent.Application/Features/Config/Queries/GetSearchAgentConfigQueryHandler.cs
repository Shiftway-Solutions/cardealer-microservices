using MediatR;
using SearchAgent.Domain.Entities;
using SearchAgent.Domain.Interfaces;

namespace SearchAgent.Application.Features.Config.Queries;

public class GetSearchAgentConfigQueryHandler : IRequestHandler<GetSearchAgentConfigQuery, SearchAgentConfig>
{
    private readonly ISearchAgentConfigRepository _configRepo;

    public GetSearchAgentConfigQueryHandler(ISearchAgentConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public Task<SearchAgentConfig> Handle(GetSearchAgentConfigQuery request, CancellationToken ct)
    {
        return _configRepo.GetActiveConfigAsync(ct);
    }
}
