using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;
using System.Text.Json;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Details)
            .HasMaxLength(2000);

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(l => l.Timestamp)
            .IsRequired();

        builder.Property(l => l.ProviderResponse)
            .HasMaxLength(1000);

        builder.Property(l => l.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(l => l.Cost)
            .HasPrecision(18, 6);

        builder.Property(l => l.IpAddress)
            .HasMaxLength(45); // Support for IPv6

        builder.Property(l => l.UserAgent)
            .HasMaxLength(500);

        builder.HasOne(l => l.Notification)
            .WithMany()
            .HasForeignKey(l => l.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for better query performance
        builder.HasIndex(l => l.NotificationId);
        builder.HasIndex(l => l.Action);
        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => new { l.NotificationId, l.Timestamp });

        // Configure JSON conversion for Metadata
        builder.Property(l => l.Metadata)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)
            );
    }
}