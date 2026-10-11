namespace AutoLineCRM.SharedKernel.Events;

public record ContractSignedEvent(Guid EventId, DateTime OccurredAt, int ContractId, int RequestId, int ClientId) : IDomainEvent
{
    public ContractSignedEvent(int contractId, int requestId, int clientId) : this(Guid.NewGuid(), DateTime.UtcNow, contractId, requestId, clientId) {}
}