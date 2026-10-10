namespace AutoLineCRM.SharedKernel.Events;

public interface IDomainEvent
{
    Guid EventID { get; }
    DateTime OccuredAt { get; }
}