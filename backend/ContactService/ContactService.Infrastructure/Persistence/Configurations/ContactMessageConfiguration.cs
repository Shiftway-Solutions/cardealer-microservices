using ContactService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactService.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.Id)
            .ValueGeneratedNever();

        builder.Property(cm => cm.Message)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(cm => cm.IsFromBuyer)
            .IsRequired();

        builder.Property(cm => cm.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(cm => cm.SenderName)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(cm => cm.CreatedAt)
            .IsRequired();

        builder.Property(cm => cm.SentAt)
            .IsRequired();

        // Indexes for query performance
        builder.HasIndex(cm => cm.ContactRequestId);
        builder.HasIndex(cm => cm.SenderId);
        builder.HasIndex(cm => cm.DealerId);
        builder.HasIndex(cm => new { cm.IsRead, cm.IsFromBuyer });
    }
}
