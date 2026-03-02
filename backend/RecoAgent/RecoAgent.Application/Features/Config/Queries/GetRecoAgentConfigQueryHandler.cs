using MediatR;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Application.Features.Config.Queries;

public class GetRecoAgentConfigQueryHandler : IRequestHandler<GetRecoAgentConfigQuery, RecoAgentConfig>
{
    private readonly IRecoAgentConfigRepository _configRepo;

    public GetRecoAgentConfigQueryHandler(IRecoAgentConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public Task<RecoAgentConfig> Handle(GetRecoAgentConfigQuery request, CancellationToken ct)
    {
        return _configRepo.GetActiveConfigAsync(ct);
    }
}
