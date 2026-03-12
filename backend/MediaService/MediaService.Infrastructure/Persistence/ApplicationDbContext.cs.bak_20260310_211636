using MediaService.Domain.Entities;
using MediaService.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CarDealer.Shared.MultiTenancy;

namespace MediaService.Infrastructure.Persistence
{
    public class ApplicationDbContext : MultiTenantDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantContext tenantContext)
            : base(options, tenantContext)
        {
        }

        // DbSets para todas las entidades
        public DbSet<MediaAsset> MediaAssets { get; set; }
        public DbSet<MediaVariant> MediaVariants { get; set; }
        public DbSet<ImageMedia> ImageMedia { get; set; }
        public DbSet<VideoMedia> VideoMedia { get; set; }
        public DbSet<DocumentMedia> DocumentMedia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplicar configuraciones desde el assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Configuraciones específicas
            modelBuilder.Entity<MediaAsset>(entity =>
            {
                entity.ToTable("media_assets");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DealerId); // Multi-tenant index
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ContentType);

                // Relación con variantes
                entity.HasMany(e => e.Variants)
                      .WithOne(v => v.MediaAsset)
                      .HasForeignKey(v => v.MediaAssetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Discriminador para herencia TPH
                entity.HasDiscriminator<string>("MediaType")
                      .HasValue<ImageMedia>("Image")
                      .HasValue<VideoMedia>("Video")
                      .HasValue<DocumentMedia>("Document")
                      .HasValue<MediaAsset>("Other");
            });

            modelBuilder.Entity<MediaVariant>(entity =>
            {
                entity.ToTable("media_variants");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.MediaAssetId, e.Name }).IsUnique();
                entity.HasIndex(e => e.Width);
                entity.HasIndex(e => e.Height);
                entity.HasIndex(e => e.Quality);
            });

            // TPH: Todas las entidades de herencia en la misma tabla (media_assets)
            // No configurar ToTable para tipos derivados en TPH
            modelBuilder.Entity<ImageMedia>(entity =>
            {
                entity.HasBaseType<MediaAsset>();
            });

            modelBuilder.Entity<VideoMedia>(entity =>
            {
                entity.HasBaseType<MediaAsset>();
            });

            modelBuilder.Entity<DocumentMedia>(entity =>
            {
                entity.HasBaseType<MediaAsset>();
            });
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Domain.Common.EntityBase && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (Domain.Common.EntityBase)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    var createdAtProperty = entity.GetType().GetProperty("CreatedAt");
                    if (createdAtProperty != null && createdAtProperty.CanWrite)
                    {
                        createdAtProperty.SetValue(entity, DateTime.UtcNow);
                    }
                }

                entity.MarkAsUpdated();
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}