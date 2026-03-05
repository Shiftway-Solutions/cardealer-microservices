using Microsoft.EntityFrameworkCore;
using ChatbotService.Domain.Entities;

namespace ChatbotService.Infrastructure.Persistence;

public class ChatbotDbContext : DbContext
{
    public ChatbotDbContext(DbContextOptions<ChatbotDbContext> options) : base(options)
    {
    }

    // Chat entities
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatLead> ChatLeads => Set<ChatLead>();

    // Configuration entities
    public DbSet<ChatbotConfiguration> ChatbotConfigurations => Set<ChatbotConfiguration>();
    public DbSet<InteractionUsage> InteractionUsages => Set<InteractionUsage>();
    public DbSet<MonthlyUsageSummary> MonthlyUsageSummaries => Set<MonthlyUsageSummary>();

    // Maintenance entities
    public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();
    public DbSet<MaintenanceTaskLog> MaintenanceTaskLogs => Set<MaintenanceTaskLog>();
    public DbSet<ChatbotIntent> ChatbotIntents => Set<ChatbotIntent>();
    public DbSet<UnansweredQuestion> UnansweredQuestions => Set<UnansweredQuestion>();
    public DbSet<ChatbotVehicle> ChatbotVehicles => Set<ChatbotVehicle>();
    public DbSet<QuickResponse> QuickResponses => Set<QuickResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.UserPhone).HasMaxLength(50);
            entity.Property(e => e.Channel).HasMaxLength(50);
            entity.Property(e => e.ChannelUserId).HasMaxLength(100);
            entity.Property(e => e.ContextData).HasColumnType("jsonb");
            entity.Property(e => e.CurrentVehicleName).HasMaxLength(200);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.Language).HasMaxLength(10);

            // Dual-mode chat fields
            entity.Property(e => e.ChatMode)
                .HasConversion<int>()
                .HasDefaultValue(ChatbotService.Domain.Enums.ChatMode.General);
            entity.Property(e => e.HandoffStatus)
                .HasConversion<int>()
                .HasDefaultValue(ChatbotService.Domain.Enums.HandoffStatus.BotActive);
            entity.HasIndex(e => e.ChatMode);
            entity.HasIndex(e => e.VehicleId);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.HandoffStatus);

            // Handoff agent fields
            entity.Property(e => e.HandoffAgentName).HasMaxLength(200);
            entity.Property(e => e.HandoffReason).HasMaxLength(500);

            // Computed properties should be ignored by EF
            entity.Ignore(e => e.IsBotActive);
            entity.Ignore(e => e.IsWhatsApp);

            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Session)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lead)
                .WithOne(e => e.Session)
                .HasForeignKey<ChatLead>(e => e.SessionId);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IntentCategory);

            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.MediaUrl).HasMaxLength(500);
            entity.Property(e => e.MediaType).HasMaxLength(50);
            entity.Property(e => e.IntentName).HasMaxLength(200);
            entity.Property(e => e.LlmIntentId).HasMaxLength(100);
            entity.Property(e => e.IntentParameters).HasColumnType("jsonb");
            entity.Property(e => e.QuickReplies).HasColumnType("jsonb");
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4);
            entity.Property(e => e.SentimentScore).HasPrecision(5, 4);
            entity.Property(e => e.InteractionCost).HasPrecision(10, 6);
        });

        // ChatLead
        modelBuilder.Entity<ChatLead>(entity =>
        {
            entity.ToTable("chat_leads");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Temperature);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.WhatsApp).HasMaxLength(50);
            entity.Property(e => e.PreferredContactMethod).HasMaxLength(50);
            entity.Property(e => e.PreferredContactTime).HasMaxLength(100);
            entity.Property(e => e.InterestedVehicleName).HasMaxLength(200);
            entity.Property(e => e.BudgetMin).HasPrecision(18, 2);
            entity.Property(e => e.BudgetMax).HasPrecision(18, 2);
            entity.Property(e => e.SaleAmount).HasPrecision(18, 2);
            entity.Property(e => e.AssignedToUserName).HasMaxLength(200);
        });

        // ChatbotConfiguration
        modelBuilder.Entity<ChatbotConfiguration>(entity =>
        {
            entity.ToTable("chatbot_configurations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LlmProjectId).HasMaxLength(100);
            entity.Property(e => e.LlmModelId).HasMaxLength(100);
            entity.Property(e => e.LanguageCode).HasMaxLength(10);
            entity.Property(e => e.SystemPromptText).HasColumnType("text");
            entity.Property(e => e.CostPerInteraction).HasPrecision(10, 6);
            entity.Property(e => e.LimitReachedMessage).HasMaxLength(500);
            entity.Property(e => e.BotName).HasMaxLength(100);
            entity.Property(e => e.BotAvatarUrl).HasMaxLength(500);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(1000);
            entity.Property(e => e.OfflineMessage).HasMaxLength(500);
            entity.Property(e => e.QuickRepliesJson).HasColumnType("jsonb");
            entity.Property(e => e.BusinessHoursJson).HasColumnType("jsonb");
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.WhatsAppBusinessPhoneId).HasMaxLength(100);
            entity.Property(e => e.WhatsAppBusinessAccountId).HasMaxLength(100);
            entity.Property(e => e.FacebookPageId).HasMaxLength(100);
            entity.Property(e => e.WebhookUrl).HasMaxLength(500);
            entity.Property(e => e.WebhookSecret).HasMaxLength(200);
            entity.Property(e => e.CrmIntegrationType).HasMaxLength(50);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
        });

        // InteractionUsage
        modelBuilder.Entity<InteractionUsage>(entity =>
        {
            entity.ToTable("interaction_usages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatbotConfigurationId, e.Year, e.Month, e.Day, e.UserId })
                .HasDatabaseName("IX_interaction_usage_config_date_user");

            entity.Property(e => e.TotalCost).HasPrecision(10, 6);

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MonthlyUsageSummary
        modelBuilder.Entity<MonthlyUsageSummary>(entity =>
        {
            entity.ToTable("monthly_usage_summaries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatbotConfigurationId, e.Year, e.Month })
                .IsUnique()
                .HasDatabaseName("IX_monthly_usage_config_date");

            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.AvgInteractionsPerSession).HasPrecision(10, 2);
            entity.Property(e => e.AvgSessionDurationMinutes).HasPrecision(10, 2);
            entity.Property(e => e.ConversionRate).HasPrecision(5, 4);
            entity.Property(e => e.AvgConfidenceScore).HasPrecision(5, 4);
            entity.Property(e => e.UsagePercentage).HasPrecision(5, 2);
            entity.Property(e => e.TopIntentsJson).HasColumnType("jsonb");

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MaintenanceTask
        modelBuilder.Entity<MaintenanceTask>(entity =>
        {
            entity.ToTable("maintenance_tasks");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatbotConfigurationId);
            entity.HasIndex(e => e.TaskType);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.NextRunAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CronExpression).HasMaxLength(50);
            entity.Property(e => e.TaskConfigJson).HasColumnType("jsonb");
            entity.Property(e => e.NotificationEmails).HasMaxLength(500);
            entity.Property(e => e.AvgExecutionTimeMs).HasPrecision(10, 2);

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Logs)
                .WithOne(e => e.Task)
                .HasForeignKey(e => e.MaintenanceTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MaintenanceTaskLog
        modelBuilder.Entity<MaintenanceTaskLog>(entity =>
        {
            entity.ToTable("maintenance_task_logs");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MaintenanceTaskId);
            entity.HasIndex(e => e.StartedAt);

            entity.Property(e => e.ResultSummary).HasMaxLength(500);
            entity.Property(e => e.ResultDetails).HasColumnType("jsonb");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
        });

        // ChatbotIntent
        modelBuilder.Entity<ChatbotIntent>(entity =>
        {
            entity.ToTable("chatbot_intents");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatbotConfigurationId);
            entity.HasIndex(e => e.IntentId);
            entity.HasIndex(e => e.Category);

            entity.Property(e => e.IntentId).HasMaxLength(100);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TrainingPhrasesJson).HasColumnType("jsonb");
            entity.Property(e => e.ResponsesJson).HasColumnType("jsonb");
            entity.Property(e => e.ParametersJson).HasColumnType("jsonb");
            entity.Property(e => e.InputContextsJson).HasColumnType("jsonb");
            entity.Property(e => e.OutputContextsJson).HasColumnType("jsonb");
            entity.Property(e => e.SuggestedPhrasesJson).HasColumnType("jsonb");
            entity.Property(e => e.AvgConfidenceScore).HasPrecision(5, 4);

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UnansweredQuestion
        modelBuilder.Entity<UnansweredQuestion>(entity =>
        {
            entity.ToTable("unanswered_questions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatbotConfigurationId);
            entity.HasIndex(e => e.NormalizedQuestion);
            entity.HasIndex(e => e.OccurrenceCount);
            entity.HasIndex(e => e.IsProcessed);

            entity.Property(e => e.Question).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.NormalizedQuestion).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.AttemptedIntentName).HasMaxLength(200);
            entity.Property(e => e.AttemptedConfidence).HasPrecision(5, 4);
            entity.Property(e => e.SuggestedIntentName).HasMaxLength(200);
            entity.Property(e => e.SuggestedResponse).HasMaxLength(1000);
            entity.Property(e => e.ProcessedBy).HasMaxLength(200);

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatbotVehicle
        modelBuilder.Entity<ChatbotVehicle>(entity =>
        {
            entity.ToTable("chatbot_vehicles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatbotConfigurationId);
            entity.HasIndex(e => e.VehicleId);
            entity.HasIndex(e => e.IsAvailable);
            entity.HasIndex(e => e.IsFeatured);

            entity.Property(e => e.Make).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Trim).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.BodyType).HasMaxLength(50);
            entity.Property(e => e.FuelType).HasMaxLength(50);
            entity.Property(e => e.Transmission).HasMaxLength(50);
            entity.Property(e => e.EngineSize).HasMaxLength(50);
            entity.Property(e => e.SearchableText).HasMaxLength(1000);
            entity.Property(e => e.TagsJson).HasColumnType("jsonb");
            entity.Property(e => e.MainImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImagesJson).HasColumnType("jsonb");

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuickResponse
        modelBuilder.Entity<QuickResponse>(entity =>
        {
            entity.ToTable("quick_responses");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatbotConfigurationId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TriggersJson).HasColumnType("jsonb");
            entity.Property(e => e.Response).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.QuickRepliesJson).HasColumnType("jsonb");

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ChatbotConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
