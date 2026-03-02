namespace SearchAgent.Domain.Interfaces;

using SearchAgent.Domain.Entities;

public interface ISearchQueryRepository
{
    Task SaveAsync(SearchQuery query, CancellationToken ct = default);
    Task<IEnumerable<SearchQuery>> GetRecentAsync(int count = 100, CancellationToken ct = default);
}
