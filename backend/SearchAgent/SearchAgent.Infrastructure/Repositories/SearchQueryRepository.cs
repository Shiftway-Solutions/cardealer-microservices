using SearchAgent.Domain.Entities;
using SearchAgent.Domain.Interfaces;
using SearchAgent.Infrastructure.Persistence;

namespace SearchAgent.Infrastructure.Repositories;

public class SearchQueryRepository : ISearchQueryRepository
{
    private readonly SearchAgentDbContext _context;

    public SearchQueryRepository(SearchAgentDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(SearchQuery query, CancellationToken ct = default)
    {
        _context.SearchQueries.Add(query);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<SearchQuery>> GetRecentAsync(int count = 100, CancellationToken ct = default)
    {
        return await Task.FromResult(
            _context.SearchQueries
                .OrderByDescending(q => q.CreatedAt)
                .Take(count)
                .AsEnumerable());
    }
}
