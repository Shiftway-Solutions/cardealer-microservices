using Microsoft.EntityFrameworkCore;
using ComparisonService.Domain.Entities;

namespace ComparisonService.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<VehicleComparison> VehicleComparisons => Set<VehicleComparison>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VehicleComparison>(entity =>
        {
            entity.ToTable("vehicle_comparisons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.ShareToken).HasMaxLength(50);
            entity.Property(e => e.IsPublic).IsRequired().HasDefaultValue(false);

            // Store VehicleIds as JSON for better query support
            entity.Property(e => e.VehicleIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>()
                )
                .HasColumnType("jsonb");

            // Indexes for performance
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_vehicle_comparisons_user_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_vehicle_comparisons_created_at");
            entity.HasIndex(e => e.ShareToken)
                .HasDatabaseName("IX_vehicle_comparisons_share_token")
                .IsUnique()
                .HasFilter("share_token IS NOT NULL");

            // Compound index for user's recent comparisons
            entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                .HasDatabaseName("IX_vehicle_comparisons_user_created");
        });
    }
}
