using Microsoft.EntityFrameworkCore;
using SupportAgent.Domain.Entities;

namespace SupportAgent.Infrastructure.Persistence;

public class SupportAgentDbContext : DbContext
{
    public SupportAgentDbContext(DbContextOptions<SupportAgentDbContext> options) : base(options) { }

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<SupportAgentConfig> SupportAgentConfigs => Set<SupportAgentConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.SessionId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.LastModule).HasMaxLength(50);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.DetectedModule).HasMaxLength(50);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupportAgentConfig>(entity =>
        {
            entity.ToTable("support_agent_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(128);

            // Seed default config
            entity.HasData(new SupportAgentConfig
            {
                Id = 1,
                ModelId = "claude-haiku-4-5-20251001",
                MaxTokens = 512,
                Temperature = 0.3f,
                MaxConversationHistory = 10,
                SessionTimeoutMinutes = 30,
                IsActive = true,
                UpdatedBy = "system",
                UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
