namespace SearchAgent.Domain.Interfaces;

using SearchAgent.Domain.Entities;

public interface ISearchAgentConfigRepository
{
    Task<SearchAgentConfig> GetActiveConfigAsync(CancellationToken ct = default);
    Task UpdateConfigAsync(SearchAgentConfig config, CancellationToken ct = default);
}
