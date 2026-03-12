using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class NotificationQueueConfiguration : IEntityTypeConfiguration<NotificationQueue>
{
    public void Configure(EntityTypeBuilder<NotificationQueue> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QueuedAt)
            .IsRequired();

        builder.Property(q => q.ProcessedAt);

        builder.Property(q => q.RetryCount)
            .IsRequired();

        builder.Property(q => q.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(q => q.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne(q => q.Notification)
            .WithMany()
            .HasForeignKey(q => q.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}