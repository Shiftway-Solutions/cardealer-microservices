using Microsoft.EntityFrameworkCore;
using RecoAgent.Domain.Entities;

namespace RecoAgent.Infrastructure.Persistence;

public class RecoAgentDbContext : DbContext
{
    public RecoAgentDbContext(DbContextOptions<RecoAgentDbContext> options) : base(options)
    {
    }

    public DbSet<RecommendationLog> RecommendationLogs { get; set; } = null!;
    public DbSet<RecoAgentConfig> RecoAgentConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // RecommendationLog
        modelBuilder.Entity<RecommendationLog>(entity =>
        {
            entity.ToTable("recommendation_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.Mode).HasMaxLength(20);
            entity.Property(e => e.ProfileJson).HasColumnType("jsonb");
            entity.Property(e => e.DetectedStage).HasMaxLength(30);
            entity.Property(e => e.RecommendationsJson).HasColumnType("jsonb");
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
        });

        // RecoAgentConfig — singleton config row
        modelBuilder.Entity<RecoAgentConfig>(entity =>
        {
            entity.ToTable("reco_agent_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SponsoredPositions).HasMaxLength(50);
            entity.Property(e => e.SponsoredLabel).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            // Seed default configuration for RecoAgent
            entity.HasData(new RecoAgentConfig
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                IsEnabled = true,
                Model = "claude-sonnet-4-5-20251022",
                Temperature = 0.5f,
                MaxTokens = 2048,
                MinRecommendations = 8,
                MaxRecommendations = 12,
                SponsoredAffinityThreshold = 0.50f,
                SponsoredPositions = "2,6,11",
                SponsoredLabel = "Destacado",
                MaxSameBrandPercent = 0.40f,
                BatchRefreshIntervalHours = 4,
                CacheTtlSeconds = 14400, // 4 hours for batch
                RealTimeCacheTtlSeconds = 900  // 15 min for real-time
            });
        });
    }
}
