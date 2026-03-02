using SupportAgent.Domain.Entities;

namespace SupportAgent.Domain.Interfaces;

public interface ISupportAgentConfigRepository
{
    Task<SupportAgentConfig> GetActiveConfigAsync(CancellationToken ct = default);
    Task UpdateConfigAsync(SupportAgentConfig config, CancellationToken ct = default);
}
