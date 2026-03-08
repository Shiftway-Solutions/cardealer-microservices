using Microsoft.EntityFrameworkCore;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Infrastructure.Persistence;

namespace ChatbotService.Infrastructure.Persistence.Repositories;

public class ChatSessionRepository : IChatSessionRepository
{
    private readonly ChatbotDbContext _context;

    public ChatSessionRepository(ChatbotDbContext context) => _context = context;

    public async Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<ChatSession?> GetByTokenAsync(string sessionToken, CancellationToken ct = default)
        => await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionToken == sessionToken, ct);

    public async Task<ChatSession?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.ChatSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SessionStatus.Active, ct);

    public async Task<ChatSession?> GetByChannelUserIdAsync(string channel, string channelUserId, CancellationToken ct = default)
        => await _context.ChatSessions
            .Where(s => s.Channel == channel && s.ChannelUserId == channelUserId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.ChatSessions.Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(CancellationToken ct = default)
        => await _context.ChatSessions.Where(s => s.Status == SessionStatus.Active).ToListAsync(ct);

    public async Task<IEnumerable<ChatSession>> GetExpiredSessionsAsync(int timeoutMinutes, CancellationToken ct = default)
        => await _context.ChatSessions.Where(s => s.Status == SessionStatus.Active && s.LastActivityAt < DateTime.UtcNow.AddMinutes(-timeoutMinutes)).ToListAsync(ct);

    public async Task<ChatSession> CreateAsync(ChatSession session, CancellationToken ct = default)
    {
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateAsync(ChatSession session, CancellationToken ct = default)
    {
        _context.ChatSessions.Update(session);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetTodaySessionCountAsync(Guid configurationId, CancellationToken ct = default)
        => await _context.ChatSessions.CountAsync(s => s.ChatbotConfigurationId == configurationId && s.CreatedAt >= DateTime.UtcNow.Date, ct);

    public async Task<int> GetMonthSessionCountAsync(Guid configurationId, int year, int month, CancellationToken ct = default)
        => await _context.ChatSessions.CountAsync(s => s.ChatbotConfigurationId == configurationId && s.CreatedAt.Year == year && s.CreatedAt.Month == month, ct);
}

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly ChatbotDbContext _context;

    public ChatMessageRepository(ChatbotDbContext context) => _context = context;

    public async Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
        => await _context.ChatMessages.Where(m => m.SessionId == sessionId).OrderBy(m => m.CreatedAt).ToListAsync(ct);

    public async Task<ChatMessage> CreateAsync(ChatMessage message, CancellationToken ct = default)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(ct);
        return message;
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(Guid configurationId, int count, CancellationToken ct = default)
        => await _context.ChatMessages.Include(m => m.Session).Where(m => m.Session!.ChatbotConfigurationId == configurationId).OrderByDescending(m => m.CreatedAt).Take(count).ToListAsync(ct);

    public async Task<IEnumerable<ChatMessage>> GetFallbackMessagesAsync(Guid configurationId, DateTime since, CancellationToken ct = default)
        => await _context.ChatMessages.Include(m => m.Session).Where(m => m.Session!.ChatbotConfigurationId == configurationId && m.IntentCategory == IntentCategory.Fallback && m.CreatedAt >= since).ToListAsync(ct);

    public async Task<IEnumerable<ChatMessage>> GetBySessionTokenAsync(string sessionToken, CancellationToken ct = default)
    {
        var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionToken == sessionToken, ct);
        if (session == null) return Enumerable.Empty<ChatMessage>();
        return await _context.ChatMessages.Where(m => m.SessionId == session.Id).OrderBy(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task<int> GetLlmCallsCountAsync(Guid configurationId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.ChatMessages
            .Include(m => m.Session)
            .CountAsync(m => m.Session!.ChatbotConfigurationId == configurationId 
                && m.CreatedAt >= from 
                && m.CreatedAt < to 
                && m.ConsumedInteraction, ct);

    public async Task<IEnumerable<ChatMessage>> GetRecentBySessionTokenAsync(string sessionToken, int maxMessages, CancellationToken ct = default)
    {
        var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionToken == sessionToken, ct);
        if (session == null) return Enumerable.Empty<ChatMessage>();
        return await _context.ChatMessages
            .Where(m => m.SessionId == session.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxMessages)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetLlmCallsCountByUserAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.ChatMessages
            .Include(m => m.Session)
            .CountAsync(m => m.Session!.UserId == userId
                && m.CreatedAt >= from
                && m.CreatedAt < to
                && m.ConsumedInteraction, ct);
}

public class ChatLeadRepository : IChatLeadRepository
{
    private readonly ChatbotDbContext _context;

    public ChatLeadRepository(ChatbotDbContext context) => _context = context;

    public async Task<ChatLead?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ChatLeads.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<ChatLead?> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
        => await _context.ChatLeads.FirstOrDefaultAsync(l => l.SessionId == sessionId, ct);

    public async Task<IEnumerable<ChatLead>> GetByStatusAsync(LeadStatus status, int page, int pageSize, CancellationToken ct = default)
        => await _context.ChatLeads.Where(l => l.Status == status).OrderByDescending(l => l.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public async Task<IEnumerable<ChatLead>> GetUnassignedLeadsAsync(CancellationToken ct = default)
        => await _context.ChatLeads.Where(l => l.AssignedToUserId == null && l.Status == LeadStatus.New).ToListAsync(ct);

    public async Task<ChatLead> CreateAsync(ChatLead lead, CancellationToken ct = default)
    {
        _context.ChatLeads.Add(lead);
        await _context.SaveChangesAsync(ct);
        return lead;
    }

    public async Task UpdateAsync(ChatLead lead, CancellationToken ct = default)
    {
        _context.ChatLeads.Update(lead);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetTodayLeadCountAsync(Guid configurationId, CancellationToken ct = default)
        => await _context.ChatLeads.Include(l => l.Session).CountAsync(l => l.Session!.ChatbotConfigurationId == configurationId && l.CreatedAt >= DateTime.UtcNow.Date, ct);

    public async Task<int> GetMonthLeadCountAsync(Guid configurationId, int year, int month, CancellationToken ct = default)
        => await _context.ChatLeads.Include(l => l.Session).CountAsync(l => l.Session!.ChatbotConfigurationId == configurationId && l.CreatedAt.Year == year && l.CreatedAt.Month == month, ct);
}

public class ChatbotConfigurationRepository : IChatbotConfigurationRepository
{
    private readonly ChatbotDbContext _context;

    public ChatbotConfigurationRepository(ChatbotDbContext context) => _context = context;

    public async Task<ChatbotConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ChatbotConfigurations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<ChatbotConfiguration?> GetByDealerIdAsync(Guid dealerId, CancellationToken ct = default)
        => await _context.ChatbotConfigurations.FirstOrDefaultAsync(c => c.DealerId == dealerId && c.IsEnabled, ct);

    public async Task<ChatbotConfiguration?> GetGlobalConfigurationAsync(CancellationToken ct = default)
        => await _context.ChatbotConfigurations.FirstOrDefaultAsync(c => c.DealerId == null && c.IsEnabled, ct);

    public async Task<IEnumerable<ChatbotConfiguration>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.ChatbotConfigurations.Where(c => c.IsEnabled).ToListAsync(ct);

    public async Task<ChatbotConfiguration> CreateAsync(ChatbotConfiguration config, CancellationToken ct = default)
    {
        _context.ChatbotConfigurations.Add(config);
        await _context.SaveChangesAsync(ct);
        return config;
    }

    public async Task UpdateAsync(ChatbotConfiguration config, CancellationToken ct = default)
    {
        _context.ChatbotConfigurations.Update(config);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ChatbotConfiguration?> GetDefaultAsync(CancellationToken ct = default)
        => await _context.ChatbotConfigurations.FirstOrDefaultAsync(c => c.IsEnabled, ct);
}

public class InteractionUsageRepository : IInteractionUsageRepository
{
    private readonly ChatbotDbContext _context;

    public InteractionUsageRepository(ChatbotDbContext context) => _context = context;

    public async Task<InteractionUsage?> GetTodayUsageAsync(Guid configurationId, Guid? userId, CancellationToken ct = default)
        => await _context.InteractionUsages.FirstOrDefaultAsync(u => u.ChatbotConfigurationId == configurationId && u.UserId == userId && u.UsageDate == DateTime.UtcNow.Date, ct);

    public async Task<InteractionUsage?> GetMonthUsageAsync(Guid configurationId, Guid? userId, int year, int month, CancellationToken ct = default)
        => await _context.InteractionUsages.FirstOrDefaultAsync(u => u.ChatbotConfigurationId == configurationId && u.UserId == userId && u.UsageDate.Year == year && u.UsageDate.Month == month, ct);

    public async Task<int> GetGlobalTodayInteractionsAsync(Guid configurationId, CancellationToken ct = default)
        => await _context.InteractionUsages.Where(u => u.ChatbotConfigurationId == configurationId && u.UsageDate == DateTime.UtcNow.Date).SumAsync(u => u.InteractionCount, ct);

    public async Task<int> GetGlobalMonthInteractionsAsync(Guid configurationId, int year, int month, CancellationToken ct = default)
        => await _context.InteractionUsages.Where(u => u.ChatbotConfigurationId == configurationId && u.UsageDate.Year == year && u.UsageDate.Month == month).SumAsync(u => u.InteractionCount, ct);

    public async Task<InteractionUsage> IncrementUsageAsync(Guid configurationId, Guid? userId, string? sessionToken, decimal cost, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var usage = await _context.InteractionUsages.FirstOrDefaultAsync(u => u.ChatbotConfigurationId == configurationId && u.UserId == userId && u.UsageDate == today, ct);
        if (usage == null)
        {
            usage = new InteractionUsage { Id = Guid.NewGuid(), ChatbotConfigurationId = configurationId, UserId = userId, UsageDate = today };
            _context.InteractionUsages.Add(usage);
        }
        usage.InteractionCount++;
        usage.TotalCost += cost;
        usage.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return usage;
    }

    public async Task<MonthlyUsageSummary?> GetMonthlySummaryAsync(Guid configurationId, int year, int month, CancellationToken ct = default)
        => await _context.MonthlyUsageSummaries.FirstOrDefaultAsync(s => s.ChatbotConfigurationId == configurationId && s.Year == year && s.Month == month, ct);

    public async Task<MonthlyUsageSummary> CreateOrUpdateMonthlySummaryAsync(MonthlyUsageSummary summary, CancellationToken ct = default)
    {
        var existing = await GetMonthlySummaryAsync(summary.ChatbotConfigurationId, summary.Year, summary.Month, ct);
        if (existing == null)
        {
            _context.MonthlyUsageSummaries.Add(summary);
        }
        else
        {
            existing.TotalInteractions = summary.TotalInteractions;
            existing.TotalCost = summary.TotalCost;
            _context.MonthlyUsageSummaries.Update(existing);
        }
        await _context.SaveChangesAsync(ct);
        return summary;
    }
}

public class MaintenanceTaskRepository : IMaintenanceTaskRepository
{
    private readonly ChatbotDbContext _context;

    public MaintenanceTaskRepository(ChatbotDbContext context) => _context = context;

    public async Task<MaintenanceTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IEnumerable<MaintenanceTask>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default)
        => await _context.MaintenanceTasks.Where(t => t.ChatbotConfigurationId == configurationId).ToListAsync(ct);

    public async Task<IEnumerable<MaintenanceTask>> GetDueTasksAsync(CancellationToken ct = default)
        => await _context.MaintenanceTasks.Where(t => t.IsEnabled && t.NextRunAt <= DateTime.UtcNow).ToListAsync(ct);

    public async Task<IEnumerable<MaintenanceTask>> GetByTypeAsync(MaintenanceTaskType type, CancellationToken ct = default)
        => await _context.MaintenanceTasks.Where(t => t.TaskType == type).ToListAsync(ct);

    public async Task<MaintenanceTask> CreateAsync(MaintenanceTask task, CancellationToken ct = default)
    {
        _context.MaintenanceTasks.Add(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    public async Task UpdateAsync(MaintenanceTask task, CancellationToken ct = default)
    {
        _context.MaintenanceTasks.Update(task);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<MaintenanceTaskLog> LogExecutionAsync(MaintenanceTaskLog log, CancellationToken ct = default)
    {
        _context.MaintenanceTaskLogs.Add(log);
        await _context.SaveChangesAsync(ct);
        return log;
    }

    public async Task<IEnumerable<MaintenanceTaskLog>> GetLogsAsync(Guid taskId, int limit, CancellationToken ct = default)
        => await _context.MaintenanceTaskLogs.Where(l => l.MaintenanceTaskId == taskId).OrderByDescending(l => l.StartedAt).Take(limit).ToListAsync(ct);
}

public class QuickResponseRepository : IQuickResponseRepository
{
    private readonly ChatbotDbContext _context;

    public QuickResponseRepository(ChatbotDbContext context) => _context = context;

    public async Task<QuickResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.QuickResponses.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<QuickResponse>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default)
        => await _context.QuickResponses.Where(r => r.ChatbotConfigurationId == configurationId && r.IsActive).OrderByDescending(r => r.Priority).ToListAsync(ct);

    public async Task<IEnumerable<QuickResponse>> GetByCategoryAsync(Guid configurationId, string category, CancellationToken ct = default)
        => await _context.QuickResponses.Where(r => r.ChatbotConfigurationId == configurationId && r.Category == category && r.IsActive).ToListAsync(ct);

    public async Task<QuickResponse?> FindMatchingAsync(Guid configurationId, string userMessage, CancellationToken ct = default)
    {
        var responses = await GetByConfigurationIdAsync(configurationId, ct);
        var msg = userMessage.ToLowerInvariant();
        return responses.FirstOrDefault(r => r.GetTriggers().Any(t => msg.Contains(t.ToLowerInvariant())));
    }

    public async Task<QuickResponse> CreateAsync(QuickResponse response, CancellationToken ct = default)
    {
        _context.QuickResponses.Add(response);
        await _context.SaveChangesAsync(ct);
        return response;
    }

    public async Task UpdateAsync(QuickResponse response, CancellationToken ct = default)
    {
        _context.QuickResponses.Update(response);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await GetByIdAsync(id, ct);
        if (response != null) { _context.QuickResponses.Remove(response); await _context.SaveChangesAsync(ct); }
    }

    public async Task IncrementUsageCountAsync(Guid id, CancellationToken ct = default)
    {
        var response = await GetByIdAsync(id, ct);
        if (response != null) { response.UsageCount++; response.LastUsedAt = DateTime.UtcNow; await _context.SaveChangesAsync(ct); }
    }
}

public class ChatbotVehicleRepository : IChatbotVehicleRepository
{
    private readonly ChatbotDbContext _context;

    public ChatbotVehicleRepository(ChatbotDbContext context) => _context = context;

    public async Task<ChatbotVehicle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ChatbotVehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ChatbotVehicle?> GetByVehicleIdAsync(Guid configurationId, Guid vehicleId, CancellationToken ct = default)
        => await _context.ChatbotVehicles.FirstOrDefaultAsync(v => v.ChatbotConfigurationId == configurationId && v.VehicleId == vehicleId, ct);

    public async Task<IEnumerable<ChatbotVehicle>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken ct = default)
        => await _context.ChatbotVehicles.Where(v => v.ChatbotConfigurationId == configurationId && v.IsAvailable).ToListAsync(ct);

    public async Task<IEnumerable<ChatbotVehicle>> SearchAsync(Guid configurationId, string searchText, int limit = 5, CancellationToken ct = default)
    {
        var search = searchText.ToLowerInvariant();
        return await _context.ChatbotVehicles.Where(v => v.ChatbotConfigurationId == configurationId && v.IsAvailable && (v.Make.ToLower().Contains(search) || v.Model.ToLower().Contains(search))).Take(limit).ToListAsync(ct);
    }

    public async Task<IEnumerable<ChatbotVehicle>> GetFeaturedAsync(Guid configurationId, int limit = 5, CancellationToken ct = default)
        => await _context.ChatbotVehicles.Where(v => v.ChatbotConfigurationId == configurationId && v.IsFeatured && v.IsAvailable).Take(limit).ToListAsync(ct);

    public async Task<IEnumerable<ChatbotVehicle>> GetByPriceRangeAsync(Guid configurationId, decimal minPrice, decimal maxPrice, int limit = 10, CancellationToken ct = default)
        => await _context.ChatbotVehicles.Where(v => v.ChatbotConfigurationId == configurationId && v.IsAvailable && v.Price >= minPrice && v.Price <= maxPrice).Take(limit).ToListAsync(ct);

    public async Task<ChatbotVehicle> CreateAsync(ChatbotVehicle vehicle, CancellationToken ct = default)
    {
        _context.ChatbotVehicles.Add(vehicle);
        await _context.SaveChangesAsync(ct);
        return vehicle;
    }

    public async Task UpdateAsync(ChatbotVehicle vehicle, CancellationToken ct = default)
    {
        _context.ChatbotVehicles.Update(vehicle);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> SyncFromInventoryAsync(Guid configurationId, IEnumerable<ChatbotVehicle> vehicles, CancellationToken ct = default)
    {
        var existingIds = await _context.ChatbotVehicles.Where(v => v.ChatbotConfigurationId == configurationId).Select(v => v.VehicleId).ToListAsync(ct);
        var count = 0;
        foreach (var vehicle in vehicles)
        {
            vehicle.ChatbotConfigurationId = configurationId;
            if (existingIds.Contains(vehicle.VehicleId))
                _context.ChatbotVehicles.Update(vehicle);
            else
                _context.ChatbotVehicles.Add(vehicle);
            count++;
        }
        await _context.SaveChangesAsync(ct);
        return count;
    }

    public async Task IncrementViewCountAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await GetByIdAsync(id, ct);
        if (vehicle != null) { vehicle.ViewCount++; await _context.SaveChangesAsync(ct); }
    }

    public async Task IncrementInquiryCountAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await GetByIdAsync(id, ct);
        if (vehicle != null) { vehicle.InquiryCount++; await _context.SaveChangesAsync(ct); }
    }
}

public class UnansweredQuestionRepository : IUnansweredQuestionRepository
{
    private readonly ChatbotDbContext _context;

    public UnansweredQuestionRepository(ChatbotDbContext context) => _context = context;

    public async Task<UnansweredQuestion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.UnansweredQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task<UnansweredQuestion?> GetByQuestionAsync(Guid configurationId, string normalizedQuestion, CancellationToken ct = default)
        => await _context.UnansweredQuestions.FirstOrDefaultAsync(q => q.ChatbotConfigurationId == configurationId && q.NormalizedQuestion == normalizedQuestion, ct);

    public async Task<IEnumerable<UnansweredQuestion>> GetUnprocessedAsync(Guid configurationId, int limit, CancellationToken ct = default)
        => await _context.UnansweredQuestions.Where(q => q.ChatbotConfigurationId == configurationId && !q.IsProcessed).OrderByDescending(q => q.OccurrenceCount).Take(limit).ToListAsync(ct);

    public async Task<IEnumerable<UnansweredQuestion>> GetMostFrequentAsync(Guid configurationId, int limit, CancellationToken ct = default)
        => await _context.UnansweredQuestions.Where(q => q.ChatbotConfigurationId == configurationId).OrderByDescending(q => q.OccurrenceCount).Take(limit).ToListAsync(ct);

    public async Task<UnansweredQuestion> CreateOrIncrementAsync(Guid configurationId, string question, string? attemptedIntent, decimal? confidence, CancellationToken ct = default)
    {
        var normalized = question.ToLowerInvariant().Trim();
        var existing = await GetByQuestionAsync(configurationId, normalized, ct);
        if (existing != null)
        {
            existing.OccurrenceCount++;
            existing.LastAskedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return existing;
        }
        var uq = new UnansweredQuestion
        {
            Id = Guid.NewGuid(),
            ChatbotConfigurationId = configurationId,
            OriginalQuestion = question,
            NormalizedQuestion = normalized,
            AttemptedIntentName = attemptedIntent,
            AttemptedConfidence = confidence,
            OccurrenceCount = 1,
            FirstAskedAt = DateTime.UtcNow,
            LastAskedAt = DateTime.UtcNow
        };
        _context.UnansweredQuestions.Add(uq);
        await _context.SaveChangesAsync(ct);
        return uq;
    }

    public async Task UpdateAsync(UnansweredQuestion question, CancellationToken ct = default)
    {
        _context.UnansweredQuestions.Update(question);
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkAsProcessedAsync(Guid id, Guid? addedToIntentId, string processedBy, CancellationToken ct = default)
    {
        var question = await GetByIdAsync(id, ct);
        if (question != null)
        {
            question.IsProcessed = true;
            question.ProcessedAt = DateTime.UtcNow;
            question.ProcessedBy = processedBy;
            question.AddedToIntentId = addedToIntentId;
            await _context.SaveChangesAsync(ct);
        }
    }
}
