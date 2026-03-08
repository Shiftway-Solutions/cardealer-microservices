using MediatR;
using Microsoft.Extensions.Logging;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.DTOs;

namespace ChatbotService.Application.Features.Maintenance.Commands;

/// <summary>
/// Handler para ejecutar tarea de mantenimiento
/// </summary>
public class RunMaintenanceTaskCommandHandler : IRequestHandler<RunMaintenanceTaskCommand, MaintenanceTaskResultDto>
{
    private readonly IMaintenanceTaskRepository _taskRepository;
    private readonly IInventorySyncService _inventoryService;
    private readonly IAutoLearningService _autoLearningService;
    private readonly IHealthMonitoringService _healthService;
    private readonly IReportingService _reportingService;
    private readonly ILlmService _llmService;
    private readonly ILlmResponseCacheService _llmCacheService;
    private readonly ILogger<RunMaintenanceTaskCommandHandler> _logger;

    public RunMaintenanceTaskCommandHandler(
        IMaintenanceTaskRepository taskRepository,
        IInventorySyncService inventoryService,
        IAutoLearningService autoLearningService,
        IHealthMonitoringService healthService,
        IReportingService reportingService,
        ILlmService llmService,
        ILlmResponseCacheService llmCacheService,
        ILogger<RunMaintenanceTaskCommandHandler> logger)
    {
        _taskRepository = taskRepository;
        _inventoryService = inventoryService;
        _autoLearningService = autoLearningService;
        _healthService = healthService;
        _reportingService = reportingService;
        _llmService = llmService;
        _llmCacheService = llmCacheService;
        _logger = logger;
    }

    public async Task<MaintenanceTaskResultDto> Handle(RunMaintenanceTaskCommand request, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var task = await _taskRepository.GetByIdAsync(request.TaskId, ct);
            if (task == null)
            {
                return new MaintenanceTaskResultDto
                {
                    Success = false,
                    Message = "Task not found",
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow,
                    ExecutionTimeMs = 0
                };
            }

            _logger.LogInformation("Executing maintenance task: {TaskType}", task.TaskType);

            var result = await ExecuteTaskAsync(task, ct);

            // Log execution
            var log = new MaintenanceTaskLog
            {
                Id = Guid.NewGuid(),
                MaintenanceTaskId = task.Id,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow,
                Success = result.Success,
                ResultSummary = result.Message,
                ErrorMessage = result.ErrorMessage
            };
            await _taskRepository.LogExecutionAsync(log, ct);

            // Update task
            task.LastRunAt = DateTime.UtcNow;
            task.LastRunSuccess = result.Success;
            task.LastRunResult = result.Message;
            task.LastRunError = result.ErrorMessage;
            task.TotalExecutions++;
            if (result.Success) task.SuccessfulExecutions++;
            else task.FailedExecutions++;
            await _taskRepository.UpdateAsync(task, ct);

            _logger.LogInformation("Maintenance task {TaskType} completed: {Success}", task.TaskType, result.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maintenance task {TaskId} failed", request.TaskId);
            return new MaintenanceTaskResultDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private async Task<MaintenanceTaskResultDto> ExecuteTaskAsync(MaintenanceTask task, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        string? message = null;
        string? errorMessage = null;
        bool success = false;

        try
        {
            switch (task.TaskType)
            {
                case MaintenanceTaskType.InventorySync:
                    var syncResult = await _inventoryService.SyncVehiclesAsync(task.ChatbotConfigurationId, ct);
                    success = syncResult.Success;
                    message = $"Synced {syncResult.TotalVehiclesProcessed} vehicles: {syncResult.NewVehiclesAdded} added, {syncResult.VehiclesUpdated} updated, {syncResult.VehiclesRemoved} removed";

                    // P1 FIX: Invalidate LLM response cache after inventory sync.
                    // Cached responses may reference old prices, sold vehicles, or stale inventory counts.
                    if (syncResult.Success && (syncResult.NewVehiclesAdded > 0 || syncResult.VehiclesUpdated > 0 || syncResult.VehiclesRemoved > 0))
                    {
                        try
                        {
                            await _llmCacheService.InvalidateAllAsync(ct);
                            _logger.LogInformation("LLM cache invalidated after inventory sync ({Added} added, {Updated} updated, {Removed} removed)",
                                syncResult.NewVehiclesAdded, syncResult.VehiclesUpdated, syncResult.VehiclesRemoved);
                        }
                        catch (Exception cacheEx)
                        {
                            _logger.LogWarning(cacheEx, "Failed to invalidate LLM cache after inventory sync");
                        }
                    }
                    break;

                case MaintenanceTaskType.HealthCheck:
                    var health = await _healthService.GenerateHealthReportAsync(task.ChatbotConfigurationId, ct);
                    success = health.OverallStatus != Domain.Models.ChatbotHealthStatus.Unhealthy;
                    message = $"Health status: {health.OverallStatus}";
                    break;

                case MaintenanceTaskType.AutoLearning:
                    var learning = await _autoLearningService.AnalyzeAndSuggestAsync(task.ChatbotConfigurationId, ct);
                    success = true;
                    message = $"Analyzed {learning.FallbacksAnalyzed} fallbacks, suggested {learning.SuggestedIntents.Count} intents";
                    break;

                case MaintenanceTaskType.DailyReport:
                case MaintenanceTaskType.WeeklyReport:
                case MaintenanceTaskType.MonthlyReport:
                    var (start, end) = GetReportPeriod(task.TaskType);
                    var report = await _reportingService.GenerateCostReportAsync(task.ChatbotConfigurationId, start, end, ct);
                    success = true;
                    message = $"Report generated: {report.TotalInteractions} interactions, ${report.TotalCost:F2} cost";
                    break;

                default:
                    success = false;
                    errorMessage = $"Unknown task type: {task.TaskType}";
                    break;
            }
        }
        catch (Exception ex)
        {
            success = false;
            errorMessage = ex.Message;
        }

        var completedAt = DateTime.UtcNow;
        return new MaintenanceTaskResultDto
        {
            Success = success,
            Message = message,
            ErrorMessage = errorMessage,
            StartedAt = startTime,
            CompletedAt = completedAt,
            ExecutionTimeMs = (int)(completedAt - startTime).TotalMilliseconds
        };
    }

    private static (DateTime start, DateTime end) GetReportPeriod(MaintenanceTaskType type)
    {
        var now = DateTime.UtcNow;
        return type switch
        {
            MaintenanceTaskType.DailyReport => (now.Date.AddDays(-1), now.Date),
            MaintenanceTaskType.WeeklyReport => (now.Date.AddDays(-7), now.Date),
            MaintenanceTaskType.MonthlyReport => (new DateTime(now.Year, now.Month, 1).AddMonths(-1), new DateTime(now.Year, now.Month, 1)),
            _ => (now.Date.AddDays(-1), now.Date)
        };
    }
}

/// <summary>
/// Handler para sincronizar inventario
/// </summary>
public class SyncInventoryCommandHandler : IRequestHandler<SyncInventoryCommand, InventorySyncResultDto>
{
    private readonly IInventorySyncService _inventoryService;
    private readonly ILogger<SyncInventoryCommandHandler> _logger;

    public SyncInventoryCommandHandler(
        IInventorySyncService inventoryService,
        ILogger<SyncInventoryCommandHandler> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<InventorySyncResultDto> Handle(SyncInventoryCommand request, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting inventory sync for configuration {ConfigId}", request.ConfigurationId);

        try
        {
            var result = await _inventoryService.SyncVehiclesAsync(request.ConfigurationId, ct);
            var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("Inventory sync completed: {Total} vehicles processed", result.TotalVehiclesProcessed);

            return new InventorySyncResultDto(
                Success: result.Success,
                TotalVehicles: result.TotalVehiclesProcessed,
                AddedVehicles: result.NewVehiclesAdded,
                UpdatedVehicles: result.VehiclesUpdated,
                RemovedVehicles: result.VehiclesRemoved,
                FailedVehicles: 0,
                Errors: new List<string>(),
                ExecutionTimeMs: executionTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory sync failed for configuration {ConfigId}", request.ConfigurationId);
            return new InventorySyncResultDto(
                Success: false,
                TotalVehicles: 0,
                AddedVehicles: 0,
                UpdatedVehicles: 0,
                RemovedVehicles: 0,
                FailedVehicles: 0,
                Errors: new List<string> { ex.Message },
                ExecutionTimeMs: (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            );
        }
    }
}

/// <summary>
/// Handler para ejecutar auto-learning
/// </summary>
public class RunAutoLearningCommandHandler : IRequestHandler<RunAutoLearningCommand, AutoLearningResultDto>
{
    private readonly IAutoLearningService _autoLearningService;
    private readonly ILogger<RunAutoLearningCommandHandler> _logger;

    public RunAutoLearningCommandHandler(
        IAutoLearningService autoLearningService,
        ILogger<RunAutoLearningCommandHandler> logger)
    {
        _autoLearningService = autoLearningService;
        _logger = logger;
    }

    public async Task<AutoLearningResultDto> Handle(RunAutoLearningCommand request, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting auto-learning for configuration {ConfigId}", request.ConfigurationId);

        try
        {
            var result = await _autoLearningService.AnalyzeAndSuggestAsync(request.ConfigurationId, ct);
            var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("Auto-learning completed: {Analyzed} fallbacks, {Suggestions} suggestions",
                result.FallbacksAnalyzed, result.SuggestedIntents.Count);

            return new AutoLearningResultDto(
                ConversationsAnalyzed: result.FallbacksAnalyzed,
                NewUnansweredQuestions: result.SuggestedIntents.Count,
                ExistingQuestionsUpdated: 0,
                SuggestionsGenerated: result.SuggestedIntents.Count,
                ExecutionTimeMs: executionTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-learning failed for configuration {ConfigId}", request.ConfigurationId);
            return new AutoLearningResultDto(
                ConversationsAnalyzed: 0,
                NewUnansweredQuestions: 0,
                ExistingQuestionsUpdated: 0,
                SuggestionsGenerated: 0,
                ExecutionTimeMs: (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            );
        }
    }
}

/// <summary>
/// Handler para generar reportes
/// </summary>
public class GenerateReportCommandHandler : IRequestHandler<GenerateReportCommand, GenerateReportResultDto>
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<GenerateReportCommandHandler> _logger;

    public GenerateReportCommandHandler(
        IReportingService reportingService,
        ILogger<GenerateReportCommandHandler> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    public async Task<GenerateReportResultDto> Handle(GenerateReportCommand request, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Generating {ReportType} report for configuration {ConfigId}",
            request.ReportType, request.ConfigurationId);

        try
        {
            var (start, end) = GetReportPeriod(request.ReportType, request.Date);
            var report = await _reportingService.GenerateCostReportAsync(request.ConfigurationId, start, end, ct);

            bool emailSent = false;
            if (request.SendByEmail && request.Recipients?.Any() == true)
            {
                emailSent = await _reportingService.SendReportByEmailAsync(
                    report,
                    request.Recipients.First(),
                    ct);
            }

            var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new GenerateReportResultDto(
                Success: true,
                ReportType: request.ReportType.ToString(),
                ReportContent: GenerateReportContent(report),
                ReportUrl: null,
                EmailSent: emailSent,
                ExecutionTimeMs: executionTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report generation failed for configuration {ConfigId}", request.ConfigurationId);
            return new GenerateReportResultDto(
                Success: false,
                ReportType: request.ReportType.ToString(),
                ReportContent: null,
                ReportUrl: null,
                EmailSent: false,
                ExecutionTimeMs: (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            );
        }
    }

    private static (DateTime start, DateTime end) GetReportPeriod(ReportType type, DateTime? date)
    {
        var referenceDate = date ?? DateTime.UtcNow;
        return type switch
        {
            ReportType.Daily => (referenceDate.Date.AddDays(-1), referenceDate.Date),
            ReportType.Weekly => (referenceDate.Date.AddDays(-7), referenceDate.Date),
            ReportType.Monthly => (new DateTime(referenceDate.Year, referenceDate.Month, 1).AddMonths(-1), new DateTime(referenceDate.Year, referenceDate.Month, 1)),
            _ => (referenceDate.Date.AddDays(-1), referenceDate.Date)
        };
    }

    private static string GenerateReportContent(Domain.Models.CostAnalysisReport report)
    {
        return $@"
## Chatbot Cost Report

**Period:** {report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}

### Summary
- **Total Interactions:** {report.TotalInteractions:N0}
- **LLM Calls:** {report.LlmInteractions:N0}
- **Quick Response Hits:** {report.QuickResponseInteractions:N0}
- **Total Cost:** ${report.TotalCost:F2}
- **Savings:** ${report.CostSavingsFromQuickResponses:F2}

### Cost Breakdown
- Free Tier Used: {report.FreeInteractionsUsed}/180
- Billable Interactions: {report.PaidInteractions:N0}
- Cost per Interaction: ${report.CostPerInteraction:F4}
";
    }
}
