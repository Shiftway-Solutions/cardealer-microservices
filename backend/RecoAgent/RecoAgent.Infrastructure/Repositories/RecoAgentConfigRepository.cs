using Microsoft.EntityFrameworkCore;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;
using RecoAgent.Infrastructure.Persistence;

namespace RecoAgent.Infrastructure.Repositories;

public class RecoAgentConfigRepository : IRecoAgentConfigRepository
{
    private readonly RecoAgentDbContext _context;

    public RecoAgentConfigRepository(RecoAgentDbContext context)
    {
        _context = context;
    }

    public async Task<RecoAgentConfig> GetActiveConfigAsync(CancellationToken ct = default)
    {
        var config = await _context.RecoAgentConfigs
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        return config ?? new RecoAgentConfig();
    }

    public async Task UpdateConfigAsync(RecoAgentConfig config, CancellationToken ct = default)
    {
        _context.RecoAgentConfigs.Update(config);
        await _context.SaveChangesAsync(ct);
    }
}
