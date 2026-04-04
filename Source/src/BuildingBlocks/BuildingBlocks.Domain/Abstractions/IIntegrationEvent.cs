namespace BuildingBlocks.Domain.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}