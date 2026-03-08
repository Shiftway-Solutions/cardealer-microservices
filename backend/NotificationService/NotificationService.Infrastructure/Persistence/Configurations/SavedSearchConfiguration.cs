using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("SavedSearches");

        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.UserId)
            .IsRequired();

        builder.Property(ss => ss.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ss => ss.CriteriaJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(ss => ss.NotificationFrequency)
            .HasMaxLength(20)
            .HasDefaultValue("daily");

        builder.HasIndex(ss => ss.UserId);
        builder.HasIndex(ss => new { ss.IsActive, ss.NotifyOnNewResults });
    }
}
