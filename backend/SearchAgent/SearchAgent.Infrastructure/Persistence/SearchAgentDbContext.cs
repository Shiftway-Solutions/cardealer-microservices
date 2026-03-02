using Microsoft.EntityFrameworkCore;
using SearchAgent.Domain.Entities;

namespace SearchAgent.Infrastructure.Persistence;

public class SearchAgentDbContext : DbContext
{
    public SearchAgentDbContext(DbContextOptions<SearchAgentDbContext> options) : base(options)
    {
    }

    public DbSet<SearchQuery> SearchQueries { get; set; } = null!;
    public DbSet<SearchAgentConfig> SearchAgentConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SearchQuery
        modelBuilder.Entity<SearchQuery>(entity =>
        {
            entity.ToTable("search_queries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalQuery).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReformulatedQuery).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.FiltersJson).HasColumnType("jsonb");
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
        });

        // SearchAgentConfig — singleton config row
        modelBuilder.Entity<SearchAgentConfig>(entity =>
        {
            entity.ToTable("search_agent_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SponsoredPositions).HasMaxLength(50);
            entity.Property(e => e.SponsoredLabel).HasMaxLength(50);
            entity.Property(e => e.SystemPromptOverride).HasColumnType("text");
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            // Seed default configuration
            entity.HasData(new SearchAgentConfig
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                IsEnabled = true,
                Model = "claude-haiku-4-5-20251001",
                Temperature = 0.2f,
                MaxTokens = 1024,
                MinResultsPerPage = 8,
                MaxResultsPerPage = 40,
                SponsoredAffinityThreshold = 0.45f,
                MaxSponsoredPercentage = 0.25f,
                SponsoredPositions = "1,5,10",
                SponsoredLabel = "Patrocinado",
                PriceRelaxPercent = 25,
                YearRelaxRange = 2,
                MaxRelaxationLevel = 5,
                EnableCache = true,
                CacheTtlSeconds = 3600,
                SemanticCacheThreshold = 0.95f,
                MaxQueriesPerMinutePerIp = 60,
                AiSearchTrafficPercent = 100
            });
        });
    }
}
