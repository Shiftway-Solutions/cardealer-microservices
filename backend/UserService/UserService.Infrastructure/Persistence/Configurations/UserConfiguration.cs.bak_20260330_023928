using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.EmailConfirmed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt);

            builder.Property(u => u.LastLoginAt);

            // AccountType enum
            builder.Property(u => u.AccountType)
                .IsRequired()
                .HasDefaultValue(AccountType.Buyer)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Índices
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            builder.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            builder.HasIndex(u => u.CreatedAt)
                .HasDatabaseName("IX_Users_CreatedAt");

            // Relación con UserRoles
            builder.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
