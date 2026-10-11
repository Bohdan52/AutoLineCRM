namespace AutoLineCRM.SharedKernel.Events;

public record PaymentProcessedEvent(Guid EventId, DateTime OccurredAt, int PaymentId, int RequestId, decimal Amount, bool Success) :IDomainEvent
{
    public PaymentProcessedEvent(int paymentId, int requestId, decimal amount, bool success):this(Guid.NewGuid(), DateTime.UtcNow, paymentId, requestId, amount, success) {}
}