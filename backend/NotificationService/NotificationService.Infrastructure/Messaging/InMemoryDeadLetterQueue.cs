using System.Collections.Concurrent;
using NotificationService.Domain.Interfaces;
using NotificationService.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Messaging;

public class InMemoryDeadLetterQueue : IDeadLetterQueue
{
    private readonly ConcurrentDictionary<Guid, FailedEvent> _failedEvents = new();
    private readonly ILogger<InMemoryDeadLetterQueue> _logger;
    private readonly int _maxRetries;

    public InMemoryDeadLetterQueue(ILogger<InMemoryDeadLetterQueue> logger, int maxRetries = 5)
    {
        _logger = logger;
        _maxRetries = maxRetries;
    }

    public Task Enqueue(string eventType, string eventJson, string error)
    {
        var failedEvent = new FailedEvent
        {
            EventType = eventType,
            EventJson = eventJson,
            LastError = error,
            FailedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        failedEvent.ScheduleNextRetry();
        _failedEvents.TryAdd(failedEvent.Id, failedEvent);

        _logger.LogWarning("📬 NotificationService DLQ: {EventType} | NextRetry: {NextRetry}",
            eventType, failedEvent.NextRetryAt);

        return Task.CompletedTask;
    }

    public Task<List<FailedEvent>> GetEventsReadyForRetry()
    {
        var now = DateTime.UtcNow;
        var readyEvents = _failedEvents.Values
            .Where(e => e.NextRetryAt <= now && !e.HasExceededMaxRetries(_maxRetries))
            .ToList();

        return Task.FromResult(readyEvents);
    }

    public Task Remove(Guid eventId)
    {
        if (_failedEvents.TryRemove(eventId, out var removedEvent))
        {
            _logger.LogInformation("✅ NotificationService DLQ removed: {EventType}", removedEvent.EventType);
        }

        return Task.CompletedTask;
    }

    public Task MarkAsFailed(Guid eventId, string error)
    {
        if (_failedEvents.TryGetValue(eventId, out var failedEvent))
        {
            failedEvent.LastError = error;
            failedEvent.ScheduleNextRetry();

            if (failedEvent.HasExceededMaxRetries(_maxRetries))
            {
                _logger.LogError("❌ NotificationService DLQ max retries: {EventType}", failedEvent.EventType);
                // RELIABILITY: Remove exhausted events to prevent unbounded memory growth
                _failedEvents.TryRemove(eventId, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task<(int Total, int ReadyForRetry, int Exhausted)> GetStats()
    {
        var now = DateTime.UtcNow;
        var total = _failedEvents.Count;
        var readyForRetry = _failedEvents.Values.Count(e => e.NextRetryAt <= now && !e.HasExceededMaxRetries(_maxRetries));
        var exhausted = _failedEvents.Values.Count(e => e.HasExceededMaxRetries(_maxRetries));

        return Task.FromResult((total, readyForRetry, exhausted));
    }
}
