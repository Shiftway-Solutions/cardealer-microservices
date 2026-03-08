using ContactService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContactService.Infrastructure.Persistence.Configurations;

public class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> builder)
    {
        builder.ToTable("ContactRequests");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Id)
            .ValueGeneratedNever();

        builder.Property(cr => cr.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cr => cr.BuyerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cr => cr.BuyerEmail)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(cr => cr.BuyerPhone)
            .HasMaxLength(20);

        builder.Property(cr => cr.Name)
            .HasMaxLength(100);

        builder.Property(cr => cr.Email)
            .HasMaxLength(254);

        builder.Property(cr => cr.Phone)
            .HasMaxLength(20);

        builder.Property(cr => cr.Message)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(cr => cr.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Open");

        builder.Property(cr => cr.CreatedAt)
            .IsRequired();

        // Indexes for query performance
        builder.HasIndex(cr => cr.BuyerId);
        builder.HasIndex(cr => cr.SellerId);
        builder.HasIndex(cr => cr.VehicleId);
        builder.HasIndex(cr => cr.DealerId);
        builder.HasIndex(cr => cr.Status);
        builder.HasIndex(cr => cr.CreatedAt);

        // Relationships
        builder.HasMany(cr => cr.Messages)
            .WithOne(cm => cm.ContactRequest)
            .HasForeignKey(cm => cm.ContactRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
