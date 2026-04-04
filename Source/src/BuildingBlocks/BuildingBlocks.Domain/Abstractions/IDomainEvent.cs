namespace BuildingBlocks.Domain.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}