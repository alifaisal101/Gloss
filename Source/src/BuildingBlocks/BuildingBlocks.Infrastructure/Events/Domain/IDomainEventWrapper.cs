using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Infrastructure.Events.Domain;

internal interface IDomainEventWrapper
{
    Task Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}