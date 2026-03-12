using Microsoft.EntityFrameworkCore;
using VehicleIntelligenceService.Domain.Entities;

namespace VehicleIntelligenceService.Infrastructure.Persistence;

public class VehicleIntelligenceDbContext : DbContext
{
    public VehicleIntelligenceDbContext(DbContextOptions<VehicleIntelligenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<PriceAnalysis> PriceAnalyses => Set<PriceAnalysis>();
    public DbSet<PriceRecommendation> PriceRecommendations => Set<PriceRecommendation>();
    public DbSet<DemandPrediction> DemandPredictions => Set<DemandPrediction>();
    public DbSet<MarketComparable> MarketComparables => Set<MarketComparable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PriceAnalysis>(entity =>
        {
            entity.ToTable("price_analyses");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VehicleId);
            entity.HasIndex(e => e.AnalysisDate);

            entity.Property(e => e.CurrentPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SuggestedPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SuggestedPriceMin).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SuggestedPriceMax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MarketAvgPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PriceVsMarket).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5,2)");
        });

        modelBuilder.Entity<PriceRecommendation>(entity =>
        {
            entity.ToTable("price_recommendations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PriceAnalysisId);

            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.SuggestedValue).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.PriceAnalysis)
                  .WithMany()
                  .HasForeignKey(e => e.PriceAnalysisId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DemandPrediction>(entity =>
        {
            entity.ToTable("demand_predictions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Make, e.Model, e.Year });
            entity.HasIndex(e => e.PredictionDate);

            entity.Property(e => e.CurrentDemand).HasConversion<string>();
            entity.Property(e => e.Trend).HasConversion<string>();
            entity.Property(e => e.PredictedDemand30Days).HasConversion<string>();
            entity.Property(e => e.PredictedDemand90Days).HasConversion<string>();
            entity.Property(e => e.BuyRecommendation).HasConversion<string>();
            entity.Property(e => e.DemandScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.TrendStrength).HasColumnType("decimal(3,2)");
            entity.Property(e => e.AvgDaysToSale).HasColumnType("decimal(5,1)");
        });

        modelBuilder.Entity<MarketComparable>(entity =>
        {
            entity.ToTable("market_comparables");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PriceAnalysisId);
            entity.HasIndex(e => new { e.Make, e.Model, e.Year });

            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SimilarityScore).HasColumnType("decimal(5,2)");

            entity.HasOne(e => e.PriceAnalysis)
                  .WithMany()
                  .HasForeignKey(e => e.PriceAnalysisId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
