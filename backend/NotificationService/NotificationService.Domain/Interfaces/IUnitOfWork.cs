using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    INotificationRepository Notifications { get; }
    INotificationTemplateRepository NotificationTemplates { get; }
    INotificationQueueRepository NotificationQueues { get; }
    INotificationLogRepository NotificationLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}