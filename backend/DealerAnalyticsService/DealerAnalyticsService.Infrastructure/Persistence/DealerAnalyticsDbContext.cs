using Microsoft.EntityFrameworkCore;
using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Infrastructure.Persistence.Configurations;

namespace DealerAnalyticsService.Infrastructure.Persistence;

public class DealerAnalyticsDbContext : DbContext
{
    public DealerAnalyticsDbContext(DbContextOptions<DealerAnalyticsDbContext> options) : base(options)
    {
    }

    // Existing entities
    public DbSet<DealerAnalytic> DealerAnalytics { get; set; }
    public DbSet<ConversionFunnel> ConversionFunnels { get; set; }
    public DbSet<MarketBenchmark> MarketBenchmarks { get; set; }
    public DbSet<DealerInsight> DealerInsights { get; set; }

    // New analytics entities
    public DbSet<DealerSnapshot> DealerSnapshots { get; set; }
    public DbSet<VehiclePerformance> VehiclePerformances { get; set; }
    public DbSet<LeadFunnelMetrics> LeadFunnelMetrics { get; set; }
    public DbSet<DealerBenchmark> DealerBenchmarks { get; set; }
    public DbSet<DealerAlert> DealerAlerts { get; set; }
    public DbSet<InventoryAging> InventoryAgings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Existing configurations
        modelBuilder.ApplyConfiguration(new DealerAnalyticConfiguration());
        modelBuilder.ApplyConfiguration(new ConversionFunnelConfiguration());
        modelBuilder.ApplyConfiguration(new MarketBenchmarkConfiguration());
        modelBuilder.ApplyConfiguration(new DealerInsightConfiguration());

        // New entity configurations
        modelBuilder.ApplyConfiguration(new DealerSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new VehiclePerformanceConfiguration());
        modelBuilder.ApplyConfiguration(new LeadFunnelMetricsConfiguration());
        modelBuilder.ApplyConfiguration(new DealerBenchmarkConfiguration());
        modelBuilder.ApplyConfiguration(new DealerAlertConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryAgingConfiguration());
    }
}
