using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;
using ChatbotService.Infrastructure.Persistence;
using System.Net.Http.Json;
using System.Text;

namespace ChatbotService.Infrastructure.Services;

public class ReportingSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "reports@okla.com.do";
    public string NotificationServiceUrl { get; set; } = "http://notificationservice:8080/api/notifications";
}

public class ReportingService : IReportingService
{
    private readonly ChatbotDbContext _context;
    private readonly IChatbotConfigurationRepository _configRepo;
    private readonly IInteractionUsageRepository _usageRepo;
    private readonly HttpClient _httpClient;
    private readonly ReportingSettings _settings;
    private readonly ILogger<ReportingService> _logger;
    private const decimal CostPerInteraction = 0.002m;
    private const int FreeInteractionsPerMonth = 180;

    public ReportingService(
        ChatbotDbContext context,
        IChatbotConfigurationRepository configRepo,
        IInteractionUsageRepository usageRepo,
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Options.IOptions<ReportingSettings> settings,
        ILogger<ReportingService> logger)
    {
        _context = context;
        _configRepo = configRepo;
        _usageRepo = usageRepo;
        _httpClient = httpClientFactory.CreateClient("NotificationService");
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CostAnalysisReport> GenerateCostReportAsync(Guid configurationId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var report = new CostAnalysisReport
        {
            ChatbotConfigurationId = configurationId,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        try
        {
            // Get usage data
            var usages = await _context.InteractionUsages
                .Where(u => u.ChatbotConfigurationId == configurationId && u.UsageDate >= startDate && u.UsageDate <= endDate)
                .ToListAsync(ct);

            report.TotalInteractions = usages.Sum(u => u.InteractionCount);
            report.TotalCost = usages.Sum(u => u.TotalCost);

            // Calculate free vs paid
            var daysInPeriod = (endDate - startDate).Days + 1;
            var monthsInPeriod = Math.Max(1, daysInPeriod / 30.0);
            var freeInteractionsForPeriod = (int)(FreeInteractionsPerMonth * monthsInPeriod);

            report.FreeInteractionsUsed = Math.Min(report.TotalInteractions, freeInteractionsForPeriod);
            report.PaidInteractions = Math.Max(0, report.TotalInteractions - freeInteractionsForPeriod);

            // Get quick response usage
            var quickResponseCount = await _context.QuickResponses
                .Where(q => q.ChatbotConfigurationId == configurationId)
                .SumAsync(q => q.UsageCount, ct);
            report.QuickResponseInteractions = quickResponseCount;
            report.LlmInteractions = report.TotalInteractions - quickResponseCount;
            report.CostSavingsFromQuickResponses = quickResponseCount * CostPerInteraction;

            // Calculate averages
            report.CostPerInteraction = report.TotalInteractions > 0
                ? report.TotalCost / report.TotalInteractions
                : 0;

            var dailyAvg = report.TotalInteractions / (double)daysInPeriod;
            report.ProjectedMonthlyCost = (decimal)(dailyAvg * 30) * CostPerInteraction;

            // Get previous period for comparison
            var prevStart = startDate.AddDays(-daysInPeriod);
            var prevEnd = startDate.AddDays(-1);
            var prevUsages = await _context.InteractionUsages
                .Where(u => u.ChatbotConfigurationId == configurationId && u.UsageDate >= prevStart && u.UsageDate <= prevEnd)
                .SumAsync(u => u.TotalCost, ct);
            report.PreviousPeriodCost = prevUsages;
            report.CostChangePercent = prevUsages > 0
                ? ((report.TotalCost - prevUsages) / prevUsages) * 100
                : 0;

            // Breakdown by category
            var messages = await _context.ChatMessages
                .Include(m => m.Session)
                .Where(m => m.Session!.ChatbotConfigurationId == configurationId && m.CreatedAt >= startDate && m.CreatedAt <= endDate)
                .ToListAsync(ct);

            var categoryGroups = messages
                .Where(m => !string.IsNullOrEmpty(m.IntentName))
                .GroupBy(m => GetCategoryFromIntent(m.IntentName!))
                .ToDictionary(g => g.Key, g => g.Count());

            report.InteractionsByCategory = categoryGroups;
            report.CostsByCategory = categoryGroups.ToDictionary(kv => kv.Key, kv => kv.Value * CostPerInteraction);

            // Generate recommendations
            GenerateRecommendations(report);

            _logger.LogInformation("Cost report generated: {TotalInteractions} interactions, ${TotalCost} cost",
                report.TotalInteractions, report.TotalCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate cost report for configuration {ConfigId}", configurationId);
        }

        return report;
    }

    public async Task<bool> SendReportByEmailAsync(CostAnalysisReport report, string recipientEmail, CancellationToken ct = default)
    {
        try
        {
            var htmlContent = GenerateReportHtml(report);

            var notification = new
            {
                Type = "Email",
                Recipient = recipientEmail,
                Subject = $"OKLA Chatbot - Reporte de Costos ({report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd yyyy})",
                Body = htmlContent,
                IsHtml = true
            };

            var response = await _httpClient.PostAsJsonAsync($"{_settings.NotificationServiceUrl}/send", notification, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send report email: {StatusCode}", response.StatusCode);
                return false;
            }

            _logger.LogInformation("Cost report sent to {Email}", recipientEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send report email to {Email}", recipientEmail);
            return false;
        }
    }

    public async Task<object> GenerateDashboardDataAsync(Guid configurationId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var weekStart = now.AddDays(-(int)now.DayOfWeek);

        var todayInteractions = await _usageRepo.GetGlobalTodayInteractionsAsync(configurationId, ct);
        var monthInteractions = await _usageRepo.GetGlobalMonthInteractionsAsync(configurationId, now.Year, now.Month, ct);

        var activeSessions = await _context.ChatSessions
            .CountAsync(s => s.ChatbotConfigurationId == configurationId && s.Status == Domain.Enums.SessionStatus.Active, ct);

        var todayLeads = await _context.ChatLeads
            .CountAsync(l => l.CreatedAt >= now.Date, ct);

        var weeklyData = await GetWeeklyTrendAsync(configurationId, ct);

        return new
        {
            Today = new { Interactions = todayInteractions, Leads = todayLeads, ActiveSessions = activeSessions },
            Month = new
            {
                Interactions = monthInteractions,
                FreeRemaining = Math.Max(0, FreeInteractionsPerMonth - monthInteractions),
                EstimatedCost = Math.Max(0, monthInteractions - FreeInteractionsPerMonth) * CostPerInteraction
            },
            WeeklyTrend = weeklyData,
            QuickStats = new
            {
                AvgSessionDuration = await GetAverageSessionDurationAsync(configurationId, ct),
                ConversionRate = await GetConversionRateAsync(configurationId, ct),
                FallbackRate = await GetFallbackRateAsync(configurationId, ct)
            }
        };
    }

    public async Task<object> GetUsageTrendsAsync(Guid configurationId, int days = 30, CancellationToken ct = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var usages = await _context.InteractionUsages
            .Where(u => u.ChatbotConfigurationId == configurationId && u.UsageDate >= startDate)
            .OrderBy(u => u.UsageDate)
            .Select(u => new { u.UsageDate, u.InteractionCount, u.TotalCost })
            .ToListAsync(ct);

        return new
        {
            Labels = usages.Select(u => u.UsageDate.ToString("MMM dd")).ToList(),
            Interactions = usages.Select(u => u.InteractionCount).ToList(),
            Costs = usages.Select(u => u.TotalCost).ToList()
        };
    }

    private async Task<List<int>> GetWeeklyTrendAsync(Guid configurationId, CancellationToken ct)
    {
        var result = new List<int>();
        var today = DateTime.UtcNow.Date;

        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var count = await _context.InteractionUsages
                .Where(u => u.ChatbotConfigurationId == configurationId && u.UsageDate == date)
                .SumAsync(u => u.InteractionCount, ct);
            result.Add(count);
        }

        return result;
    }

    private async Task<double> GetAverageSessionDurationAsync(Guid configurationId, CancellationToken ct)
    {
        var sessions = await _context.ChatSessions
            .Where(s => s.ChatbotConfigurationId == configurationId && s.EndedAt.HasValue)
            .Select(s => (s.EndedAt!.Value - s.CreatedAt).TotalMinutes)
            .ToListAsync(ct);

        return sessions.Any() ? sessions.Average() : 0;
    }

    private async Task<double> GetConversionRateAsync(Guid configurationId, CancellationToken ct)
    {
        var totalSessions = await _context.ChatSessions
            .CountAsync(s => s.ChatbotConfigurationId == configurationId, ct);
        var sessionsWithLeads = await _context.ChatLeads
            .Select(l => l.SessionId)
            .Distinct()
            .CountAsync(ct);

        return totalSessions > 0 ? (double)sessionsWithLeads / totalSessions * 100 : 0;
    }

    private async Task<double> GetFallbackRateAsync(Guid configurationId, CancellationToken ct)
    {
        var totalMessages = await _context.ChatMessages
            .Include(m => m.Session)
            .CountAsync(m => m.Session!.ChatbotConfigurationId == configurationId && m.Type == Domain.Enums.MessageType.UserText, ct);
        var fallbackMessages = await _context.ChatMessages
            .Include(m => m.Session)
            .CountAsync(m => m.Session!.ChatbotConfigurationId == configurationId && m.IntentCategory == Domain.Enums.IntentCategory.Fallback, ct);

        return totalMessages > 0 ? (double)fallbackMessages / totalMessages * 100 : 0;
    }

    private static string GetCategoryFromIntent(string intent)
    {
        if (intent.Contains("precio", StringComparison.OrdinalIgnoreCase)) return "Pricing";
        if (intent.Contains("financ", StringComparison.OrdinalIgnoreCase)) return "Financing";
        if (intent.Contains("busca", StringComparison.OrdinalIgnoreCase)) return "Search";
        if (intent.Contains("disponib", StringComparison.OrdinalIgnoreCase)) return "Availability";
        if (intent.Contains("test", StringComparison.OrdinalIgnoreCase)) return "TestDrive";
        return "General";
    }

    private static void GenerateRecommendations(CostAnalysisReport report)
    {
        if (report.FallbackRate() > 20)
        {
            report.Recommendations.Add(new CostRecommendation
            {
                Title = "Alto índice de fallback",
                Description = "Más del 20% de las consultas resultan en fallback. Considere agregar más intents o entrenar el modelo.",
                EstimatedSavings = report.TotalCost * 0.15m,
                Priority = "High",
                ActionRequired = "Revisar preguntas no respondidas y crear nuevos intents"
            });
        }

        if (report.QuickResponseInteractions < report.TotalInteractions * 0.3m)
        {
            report.Recommendations.Add(new CostRecommendation
            {
                Title = "Bajo uso de Quick Responses",
                Description = "Las respuestas rápidas ahorran costos. Agregue más respuestas rápidas para consultas frecuentes.",
                EstimatedSavings = report.TotalInteractions * 0.2m * CostPerInteraction,
                Priority = "Medium",
                ActionRequired = "Identificar consultas frecuentes y crear Quick Responses"
            });
        }

        if (report.CostChangePercent > 20)
        {
            report.Recommendations.Add(new CostRecommendation
            {
                Title = "Incremento significativo de costos",
                Description = $"Los costos aumentaron {report.CostChangePercent:F1}% respecto al período anterior.",
                EstimatedSavings = report.TotalCost * 0.1m,
                Priority = "High",
                ActionRequired = "Revisar límites de interacciones y patrones de uso"
            });
        }
    }

    private static string GenerateReportHtml(CostAnalysisReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
        sb.AppendLine($"<h1>Reporte de Costos del Chatbot</h1>");
        sb.AppendLine($"<p>Período: {report.PeriodStart:dd/MM/yyyy} - {report.PeriodEnd:dd/MM/yyyy}</p>");
        sb.AppendLine("<h2>Resumen</h2>");
        sb.AppendLine("<table border='1' cellpadding='8'>");
        sb.AppendLine($"<tr><td>Total Interacciones</td><td>{report.TotalInteractions:N0}</td></tr>");
        sb.AppendLine($"<tr><td>Interacciones Gratuitas</td><td>{report.FreeInteractionsUsed:N0}</td></tr>");
        sb.AppendLine($"<tr><td>Interacciones Pagadas</td><td>{report.PaidInteractions:N0}</td></tr>");
        sb.AppendLine($"<tr><td>Costo Total</td><td>${report.TotalCost:F2}</td></tr>");
        sb.AppendLine($"<tr><td>Ahorro por Quick Responses</td><td>${report.CostSavingsFromQuickResponses:F2}</td></tr>");
        sb.AppendLine($"<tr><td>Proyección Mensual</td><td>${report.ProjectedMonthlyCost:F2}</td></tr>");
        sb.AppendLine("</table>");

        if (report.Recommendations.Any())
        {
            sb.AppendLine("<h2>Recomendaciones</h2><ul>");
            foreach (var rec in report.Recommendations)
                sb.AppendLine($"<li><strong>{rec.Title}</strong>: {rec.Description}</li>");
            sb.AppendLine("</ul>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}

internal static class CostReportExtensions
{
    public static double FallbackRate(this CostAnalysisReport report)
    {
        if (!report.InteractionsByCategory.Any()) return 0;
        var fallbackCount = report.InteractionsByCategory.GetValueOrDefault("Fallback", 0);
        return report.TotalInteractions > 0 ? (double)fallbackCount / report.TotalInteractions * 100 : 0;
    }
}
