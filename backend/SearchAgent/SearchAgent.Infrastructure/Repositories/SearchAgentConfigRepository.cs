using Microsoft.EntityFrameworkCore;
using SearchAgent.Domain.Entities;
using SearchAgent.Domain.Interfaces;
using SearchAgent.Infrastructure.Persistence;

namespace SearchAgent.Infrastructure.Repositories;

public class SearchAgentConfigRepository : ISearchAgentConfigRepository
{
    private readonly SearchAgentDbContext _context;

    public SearchAgentConfigRepository(SearchAgentDbContext context)
    {
        _context = context;
    }

    public async Task<SearchAgentConfig> GetActiveConfigAsync(CancellationToken ct = default)
    {
        var config = await _context.SearchAgentConfigs
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        return config ?? new SearchAgentConfig();
    }

    public async Task UpdateConfigAsync(SearchAgentConfig config, CancellationToken ct = default)
    {
        _context.SearchAgentConfigs.Update(config);
        await _context.SaveChangesAsync(ct);
    }
}
