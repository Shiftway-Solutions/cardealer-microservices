using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Infrastructure.Persistence.Configurations;

public class DealerAnalyticConfiguration : IEntityTypeConfiguration<DealerAnalytic>
{
    public void Configure(EntityTypeBuilder<DealerAnalytic> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DealerId)
            .IsRequired();

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.ConversionRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.TotalRevenue)
            .HasPrecision(15, 2);

        builder.Property(x => x.AverageVehiclePrice)
            .HasPrecision(15, 2);

        builder.Property(x => x.RevenuePerView)
            .HasPrecision(10, 2);

        builder.Property(x => x.AverageDaysOnMarket)
            .HasPrecision(5, 2);

        builder.Property(x => x.AverageViewDuration)
            .HasPrecision(8, 2);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Índices
        builder.HasIndex(x => new { x.DealerId, x.Date })
            .IsUnique()
            .HasDatabaseName("IX_DealerAnalytics_DealerId_Date");

        builder.HasIndex(x => x.Date)
            .HasDatabaseName("IX_DealerAnalytics_Date");

        builder.ToTable("dealer_analytics");
    }
}

public class ConversionFunnelConfiguration : IEntityTypeConfiguration<ConversionFunnel>
{
    public void Configure(EntityTypeBuilder<ConversionFunnel> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DealerId)
            .IsRequired();

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.ViewToContactRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.ContactToTestDriveRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.TestDriveToSaleRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.OverallConversionRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.AverageTimeToSale)
            .HasPrecision(8, 2);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Índices
        builder.HasIndex(x => new { x.DealerId, x.Date })
            .IsUnique()
            .HasDatabaseName("IX_ConversionFunnels_DealerId_Date");

        builder.ToTable("conversion_funnels");
    }
}

public class MarketBenchmarkConfiguration : IEntityTypeConfiguration<MarketBenchmark>
{
    public void Configure(EntityTypeBuilder<MarketBenchmark> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.VehicleCategory)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PriceRange)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.MarketAveragePrice)
            .HasPrecision(15, 2);

        builder.Property(x => x.MarketAverageDaysOnMarket)
            .HasPrecision(8, 2);

        builder.Property(x => x.MarketAverageViews)
            .HasPrecision(8, 2);

        builder.Property(x => x.MarketConversionRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.PricePercentile25)
            .HasPrecision(15, 2);

        builder.Property(x => x.PricePercentile50)
            .HasPrecision(15, 2);

        builder.Property(x => x.PricePercentile75)
            .HasPrecision(15, 2);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Índices
        builder.HasIndex(x => new { x.VehicleCategory, x.PriceRange, x.Date })
            .IsUnique()
            .HasDatabaseName("IX_MarketBenchmarks_Category_PriceRange_Date");

        builder.HasIndex(x => x.Date)
            .HasDatabaseName("IX_MarketBenchmarks_Date");

        builder.ToTable("market_benchmarks");
    }
}

public class DealerInsightConfiguration : IEntityTypeConfiguration<DealerInsight>
{
    public void Configure(EntityTypeBuilder<DealerInsight> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DealerId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ActionRecommendation)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.SupportingData)
            .HasColumnType("jsonb");

        builder.Property(x => x.PotentialImpact)
            .HasPrecision(5, 2);

        builder.Property(x => x.Confidence)
            .HasPrecision(5, 2);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Índices
        builder.HasIndex(x => x.DealerId)
            .HasDatabaseName("IX_DealerInsights_DealerId");

        builder.HasIndex(x => new { x.DealerId, x.IsRead })
            .HasDatabaseName("IX_DealerInsights_DealerId_IsRead");

        builder.HasIndex(x => new { x.DealerId, x.Priority })
            .HasDatabaseName("IX_DealerInsights_DealerId_Priority");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_DealerInsights_ExpiresAt");

        builder.ToTable("dealer_insights");
    }
}
