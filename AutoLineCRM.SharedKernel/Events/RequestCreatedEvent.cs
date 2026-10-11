namespace AutoLineCRM.SharedKernel.Events;

public record RequestCreatedEvent(Guid EventId, DateTime OccurredAt, int RequestId, int ClientId) : IDomainEvent 
{
    public RequestCreatedEvent(int requestId, int clientId, int carId): this(Guid.NewGuid(), DateTime.UtcNow, requestId, clientId) { }  
}