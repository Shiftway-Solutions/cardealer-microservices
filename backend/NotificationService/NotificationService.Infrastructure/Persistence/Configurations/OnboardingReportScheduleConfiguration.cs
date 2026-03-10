using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class OnboardingReportScheduleConfiguration : IEntityTypeConfiguration<OnboardingReportSchedule>
{
    public void Configure(EntityTypeBuilder<OnboardingReportSchedule> builder)
    {
        builder.ToTable("onboarding_report_schedules");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(o => o.DealerId)
            .HasColumnName("dealer_id")
            .IsRequired();

        builder.Property(o => o.OwnerUserId)
            .HasColumnName("owner_user_id");

        builder.Property(o => o.ApprovedAt)
            .HasColumnName("approved_at")
            .IsRequired();

        builder.Property(o => o.DueAt)
            .HasColumnName("due_at")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("Scheduled")
            .IsRequired();

        builder.Property(o => o.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(o => o.MaxRetries)
            .HasColumnName("max_retries")
            .HasDefaultValue(3);

        builder.Property(o => o.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(o => o.SentAt)
            .HasColumnName("sent_at");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(o => o.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(255)
            .IsRequired();

        // Indexes
        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ix_onboarding_report_schedules_idempotency_key");

        builder.HasIndex(o => new { o.Status, o.DueAt })
            .HasDatabaseName("ix_onboarding_report_schedules_status_due_at");

        builder.HasIndex(o => o.DealerId)
            .HasDatabaseName("ix_onboarding_report_schedules_dealer_id");
    }
}
