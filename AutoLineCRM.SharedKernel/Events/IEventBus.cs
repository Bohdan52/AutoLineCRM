namespace AutoLineCRM.SharedKernel.Events;

public interface IEventBus
{
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent;
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IDomainEvent;
}