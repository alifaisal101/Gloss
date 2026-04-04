using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Application.Events.Abstractions;

public interface IDomainEventPublisher
{
    Task Publish(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}