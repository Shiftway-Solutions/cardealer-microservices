using Microsoft.EntityFrameworkCore;
using SupportAgent.Domain.Entities;
using SupportAgent.Domain.Interfaces;
using SupportAgent.Infrastructure.Persistence;

namespace SupportAgent.Infrastructure.Repositories;

public class SupportAgentConfigRepository : ISupportAgentConfigRepository
{
    private readonly SupportAgentDbContext _context;

    public SupportAgentConfigRepository(SupportAgentDbContext context)
    {
        _context = context;
    }

    public async Task<SupportAgentConfig> GetActiveConfigAsync(CancellationToken ct = default)
    {
        var config = await _context.SupportAgentConfigs
            .FirstOrDefaultAsync(c => c.IsActive, ct);

        return config ?? new SupportAgentConfig();
    }

    public async Task UpdateConfigAsync(SupportAgentConfig config, CancellationToken ct = default)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.SupportAgentConfigs.Update(config);
        await _context.SaveChangesAsync(ct);
    }
}
