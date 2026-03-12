using Microsoft.EntityFrameworkCore;
using AIProcessingService.Domain.Entities;

namespace AIProcessingService.Infrastructure.Persistence;

public class AIProcessingDbContext : DbContext
{
    public AIProcessingDbContext(DbContextOptions<AIProcessingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ImageProcessingJob> ImageProcessingJobs => Set<ImageProcessingJob>();
    public DbSet<Spin360Job> Spin360Jobs => Set<Spin360Job>();
    public DbSet<BackgroundPreset> BackgroundPresets => Set<BackgroundPreset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ImageProcessingJob
        modelBuilder.Entity<ImageProcessingJob>(entity =>
        {
            entity.ToTable("image_processing_jobs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.OriginalImageUrl).HasColumnName("original_image_url").HasMaxLength(2048);
            entity.Property(e => e.ProcessedImageUrl).HasColumnName("processed_image_url").HasMaxLength(2048);
            entity.Property(e => e.MaskUrl).HasColumnName("mask_url").HasMaxLength(2048);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.MaxRetries).HasColumnName("max_retries");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ProcessingTimeMs).HasColumnName("processing_time_ms");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id").HasMaxLength(100);
            entity.Property(e => e.ModelVersion).HasColumnName("model_version").HasMaxLength(50);

            // JSON columns
            entity.Property(e => e.Options).HasColumnName("options")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<ProcessingOptions>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ProcessingOptions()
                );

            entity.Property(e => e.Result).HasColumnName("result")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<ProcessingResult>(v, (System.Text.Json.JsonSerializerOptions?)null)
                );

            // Indexes
            entity.HasIndex(e => e.VehicleId).HasDatabaseName("ix_image_processing_jobs_vehicle_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_image_processing_jobs_user_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_image_processing_jobs_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_image_processing_jobs_created_at");
        });

        // Spin360Job
        modelBuilder.Entity<Spin360Job>(entity =>
        {
            entity.ToTable("spin360_jobs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasConversion<string>();
            entity.Property(e => e.SourceVideoUrl).HasColumnName("source_video_url").HasMaxLength(2048);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(e => e.FrameCount).HasColumnName("frame_count");
            entity.Property(e => e.TotalFrames).HasColumnName("total_frames");
            entity.Property(e => e.ProcessedFrames).HasColumnName("processed_frames");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ProcessingTimeMs).HasColumnName("processing_time_ms");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id").HasMaxLength(100);

            // JSON columns
            entity.Property(e => e.SourceImageUrls).HasColumnName("source_image_urls")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );

            entity.Property(e => e.Options).HasColumnName("options")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Spin360Options>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Spin360Options()
                );

            entity.Property(e => e.Result).HasColumnName("result")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Spin360Result>(v, (System.Text.Json.JsonSerializerOptions?)null)
                );

            // Indexes
            entity.HasIndex(e => e.VehicleId).HasDatabaseName("ix_spin360_jobs_vehicle_id").IsUnique();
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_spin360_jobs_user_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_spin360_jobs_status");
        });

        // BackgroundPreset
        modelBuilder.Entity<BackgroundPreset>(entity =>
        {
            entity.ToTable("background_presets");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.BackgroundImageUrl).HasColumnName("background_image_url").HasMaxLength(2048);
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(2048);
            entity.Property(e => e.PreviewUrl).HasColumnName("preview_url").HasMaxLength(2048);
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.FloorColor).HasColumnName("floor_color").HasMaxLength(20);
            entity.Property(e => e.ShadowIntensity).HasColumnName("shadow_intensity");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.RequiresDealerMembership).HasColumnName("requires_dealer_membership");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Code).HasDatabaseName("ix_background_presets_code").IsUnique();
            entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_background_presets_is_active");
        });

        // Seed default backgrounds
        SeedBackgrounds(modelBuilder);
    }

    private static void SeedBackgrounds(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackgroundPreset>().HasData(
            new BackgroundPreset
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "white_studio",
                Name = "Blanco Infinito",
                Description = "Fondo blanco profesional estilo catálogo",
                Type = BackgroundType.Studio,
                FloorColor = "#FFFFFF",
                ShadowIntensity = 0.3f,
                IsPublic = true,
                RequiresDealerMembership = false,
                IsDefault = true,
                SortOrder = 1,
                IsActive = true
            },
            new BackgroundPreset
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = "gray_showroom",
                Name = "Showroom Gris",
                Description = "Fondo de showroom profesional en gris",
                Type = BackgroundType.Showroom,
                FloorColor = "#E5E5E5",
                ShadowIntensity = 0.4f,
                IsPublic = true,
                RequiresDealerMembership = true,
                IsDefault = false,
                SortOrder = 2,
                IsActive = true
            },
            new BackgroundPreset
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Code = "dark_studio",
                Name = "Estudio Oscuro",
                Description = "Fondo oscuro dramático para vehículos de lujo",
                Type = BackgroundType.Studio,
                FloorColor = "#1A1A1A",
                ShadowIntensity = 0.2f,
                IsPublic = true,
                RequiresDealerMembership = true,
                IsDefault = false,
                SortOrder = 3,
                IsActive = true
            },
            new BackgroundPreset
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Code = "outdoor_day",
                Name = "Exterior Día",
                Description = "Escena exterior con cielo azul",
                Type = BackgroundType.Outdoor,
                FloorColor = "#CCCCCC",
                ShadowIntensity = 0.5f,
                IsPublic = true,
                RequiresDealerMembership = true,
                IsDefault = false,
                SortOrder = 4,
                IsActive = true
            }
        );
    }
}
