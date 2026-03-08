using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("PriceAlerts");

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.UserId)
            .IsRequired();

        builder.Property(pa => pa.VehicleId)
            .IsRequired();

        builder.Property(pa => pa.VehicleTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pa => pa.VehicleImageUrl)
            .HasMaxLength(500);

        builder.Property(pa => pa.CurrentPrice)
            .HasPrecision(18, 2);

        builder.Property(pa => pa.TargetPrice)
            .HasPrecision(18, 2);

        builder.Property(pa => pa.PriceDropPercentage)
            .HasPrecision(5, 2);

        builder.HasIndex(pa => pa.UserId);
        builder.HasIndex(pa => pa.VehicleId);
        builder.HasIndex(pa => new { pa.UserId, pa.IsActive });
        builder.HasIndex(pa => new { pa.VehicleId, pa.IsActive });
    }
}
