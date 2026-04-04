using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Application.Events.Abstractions;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}