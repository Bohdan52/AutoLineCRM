namespace AutoLineCRM.SharedKernel.Events;

public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();
    private readonly Action<Exception>? _onError;

    public InMemoryEventBus(Action<Exception>? onError = null) => _onError = onError;

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(TEvent)] = list;
            }
            list.Add(handler);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        Delegate[] snapshot;
        lock (_lock)
        {
            snapshot = _handlers.TryGetValue(typeof(TEvent), out var list)
                ? list.ToArray() : Array.Empty<Delegate>();
        }

        foreach (var handler in snapshot)
        {
            try { await ((Func<TEvent, CancellationToken, Task>)handler)(@event, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { _onError?.Invoke(ex); }
        }
    }
}