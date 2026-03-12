using Cronos;
using MediatR;
using ChatbotService.Application.Features.Maintenance.Commands;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;

namespace ChatbotService.Api.Services;

public class MaintenanceWorkerService : BackgroundService
{
    private readonly ILogger<MaintenanceWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<MaintenanceTaskType, CronExpression> _schedules;

    public MaintenanceWorkerService(
        ILogger<MaintenanceWorkerService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _schedules = new Dictionary<MaintenanceTaskType, CronExpression>();

        InitializeSchedules();
    }

    private void InitializeSchedules()
    {
        var scheduleConfig = new Dictionary<MaintenanceTaskType, string>
        {
            { MaintenanceTaskType.InventorySync, _configuration["Maintenance:InventorySyncCron"] ?? "0 */4 * * *" },
            { MaintenanceTaskType.DailyReport, _configuration["Maintenance:DailyReportCron"] ?? "0 6 * * *" },
            { MaintenanceTaskType.HealthCheck, _configuration["Maintenance:HealthCheckCron"] ?? "*/15 * * * *" },
            { MaintenanceTaskType.AutoLearning, _configuration["Maintenance:AutoLearningCron"] ?? "0 2 * * 0" },
            { MaintenanceTaskType.SessionCleanup, _configuration["Maintenance:SessionCleanupCron"] ?? "0 3 * * *" }
        };

        foreach (var kvp in scheduleConfig)
        {
            try
            {
                _schedules[kvp.Key] = CronExpression.Parse(kvp.Value);
                _logger.LogInformation("Scheduled {TaskType} with cron: {Cron}", kvp.Key, kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid cron expression for {TaskType}: {Cron}", kvp.Key, kvp.Value);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MaintenanceWorkerService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRunDueTasksAsync(stoppingToken);

                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in maintenance worker loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("MaintenanceWorkerService stopped");
    }

    private async Task CheckAndRunDueTasksAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var maintenanceRepo = scope.ServiceProvider.GetRequiredService<IMaintenanceTaskRepository>();

        // Get all due tasks from database
        var dueTasks = await maintenanceRepo.GetDueTasksAsync(ct);

        foreach (var task in dueTasks)
        {
            try
            {
                _logger.LogInformation("Running maintenance task: {TaskName} ({TaskType})",
                    task.Name, task.TaskType);

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new RunMaintenanceTaskCommand(task.Id, false, "Automated run");

                await mediator.Send(command, ct);

                // Calculate next run time and update task
                if (_schedules.TryGetValue(task.TaskType, out var cron))
                {
                    var nextRun = cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
                    if (nextRun.HasValue)
                    {
                        task.NextRunAt = nextRun.Value;
                        await maintenanceRepo.UpdateAsync(task, ct);
                    }
                }

                _logger.LogInformation("Completed maintenance task: {TaskName}", task.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running maintenance task: {TaskName}", task.Name);
            }
        }
    }
}
