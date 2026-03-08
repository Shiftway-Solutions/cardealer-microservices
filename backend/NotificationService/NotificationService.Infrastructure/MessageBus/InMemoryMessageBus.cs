using System.Text.Json;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.MessageBus;

public class InMemoryMessageBus : IMessageBus
{
    private readonly Dictionary<string, List<Func<string, Task>>> _handlers = new();

    public Task PublishAsync<T>(T message, string queueName)
    {
        if (_handlers.ContainsKey(queueName))
        {
            var messageBody = JsonSerializer.Serialize(message);
            foreach (var handler in _handlers[queueName])
            {
                handler(messageBody).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        System.Diagnostics.Debug.WriteLine($"InMemoryMessageBus handler failed for queue '{queueName}': {t.Exception?.GetBaseException().Message}");
                    }
                }, System.Threading.Tasks.TaskScheduler.Default);
            }
        }
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(Func<T, Task> handler, string queueName)
    {
        if (!_handlers.ContainsKey(queueName))
        {
            _handlers[queueName] = new List<Func<string, Task>>();
        }

        _handlers[queueName].Add(async messageBody =>
        {
            var message = JsonSerializer.Deserialize<T>(messageBody);
            if (message != null)
            {
                await handler(message);
            }
        });

        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(true); // Siempre conectado en memoria
    }
}