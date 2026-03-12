using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;
using ChatbotService.Infrastructure.Persistence;
using System.Diagnostics;

namespace ChatbotService.Infrastructure.Services;

public class HealthMonitoringService : IHealthMonitoringService
{
    private readonly ChatbotDbContext _context;
    private readonly ILlmService _llmService;
    private readonly ILogger<HealthMonitoringService> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthMonitoringService(
        ChatbotDbContext context,
        ILlmService llmService,
        ILogger<HealthMonitoringService> logger)
    {
        _context = context;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<ChatbotHealthReport> GenerateHealthReportAsync(Guid configurationId, CancellationToken ct = default)
    {
        var report = new ChatbotHealthReport
        {
            ChatbotConfigurationId = configurationId,
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            // Database health
            var dbSw = Stopwatch.StartNew();
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(ct);
                dbSw.Stop();

                report.DatabaseStatus = new DatabaseHealthStatus
                {
                    IsConnected = canConnect,
                    ResponseTimeMs = dbSw.ElapsedMilliseconds,
                    TotalSessions = await _context.ChatSessions.CountAsync(ct),
                    TotalMessages = await _context.ChatMessages.CountAsync(ct),
                    ActiveSessions = await _context.ChatSessions.CountAsync(s => s.Status == Domain.Enums.SessionStatus.Active, ct)
                };
            }
            catch (Exception ex)
            {
                dbSw.Stop();
                report.DatabaseStatus = new DatabaseHealthStatus { IsConnected = false, ResponseTimeMs = dbSw.ElapsedMilliseconds };
                report.Alerts.Add(new HealthAlert { AlertType = "Database", Severity = "Critical", Message = $"Database connection failed: {ex.Message}" });
            }

            // LLM Server health
            var llmStatus = await _llmService.GetHealthStatusAsync(ct);
            report.LlmStatus = llmStatus;

            if (!llmStatus.IsConnected)
            {
                report.Alerts.Add(new HealthAlert { AlertType = "LLM", Severity = "Error", Message = "LLM server not connected" });
            }
            else if (llmStatus.FailedCallsLast24h > 10)
            {
                report.Alerts.Add(new HealthAlert { AlertType = "LLM", Severity = "Warning", Message = $"{llmStatus.FailedCallsLast24h} failed LLM calls in last 24h" });
            }

            if (!llmStatus.ModelLoaded)
            {
                report.Alerts.Add(new HealthAlert { AlertType = "LLM", Severity = "Critical", Message = "LLM model not loaded" });
            }

            // System metrics
            var process = Process.GetCurrentProcess();
            report.SystemMetrics = new SystemMetrics
            {
                MemoryUsageMb = process.WorkingSet64 / 1024.0 / 1024.0,
                Uptime = DateTime.UtcNow - _startTime
            };

            // Determine overall status
            if (!report.DatabaseStatus.IsConnected || report.Alerts.Any(a => a.Severity == "Critical"))
                report.OverallStatus = ChatbotHealthStatus.Unhealthy;
            else if (!report.LlmStatus.IsConnected || report.Alerts.Any(a => a.Severity == "Error"))
                report.OverallStatus = ChatbotHealthStatus.Degraded;
            else if (report.Alerts.Any(a => a.Severity == "Warning"))
                report.OverallStatus = ChatbotHealthStatus.Degraded;
            else
                report.OverallStatus = ChatbotHealthStatus.Healthy;

            _logger.LogInformation("Health report generated: {Status}", report.OverallStatus);
        }
        catch (Exception ex)
        {
            report.OverallStatus = ChatbotHealthStatus.Unknown;
            report.Alerts.Add(new HealthAlert { AlertType = "System", Severity = "Critical", Message = $"Health check failed: {ex.Message}" });
            _logger.LogError(ex, "Failed to generate health report");
        }

        return report;
    }

    public async Task<bool> CheckLlmHealthAsync(CancellationToken ct = default)
        => await _llmService.TestConnectivityAsync(ct);

    public async Task<bool> CheckDatabaseHealthAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(ct);
        }
        catch
        {
            return false;
        }
    }

    public async Task<SystemMetrics> GetSystemMetricsAsync(CancellationToken ct = default)
    {
        var process = Process.GetCurrentProcess();
        return await Task.FromResult(new SystemMetrics
        {
            MemoryUsageMb = process.WorkingSet64 / 1024.0 / 1024.0,
            Uptime = DateTime.UtcNow - _startTime,
            ActiveConnections = await _context.ChatSessions.CountAsync(s => s.Status == Domain.Enums.SessionStatus.Active, ct)
        });
    }

    public async Task<IEnumerable<HealthAlert>> GetActiveAlertsAsync(Guid configurationId, CancellationToken ct = default)
    {
        var report = await GenerateHealthReportAsync(configurationId, ct);
        return report.Alerts;
    }
}
