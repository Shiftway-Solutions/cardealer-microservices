namespace RecoAgent.Domain.Interfaces;

using RecoAgent.Domain.Entities;

public interface IRecoAgentConfigRepository
{
    Task<RecoAgentConfig> GetActiveConfigAsync(CancellationToken ct = default);
    Task UpdateConfigAsync(RecoAgentConfig config, CancellationToken ct = default);
}
