using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Infrastructure.Persistence.Context;

namespace BuildingBlocks.Infrastructure.Persistence;

internal interface IRepositoryWrapper
{
    Task PersistBatchAsync(
        IServiceProvider provider,
        IDomainContextTracker context,
        IEnumerable<object> aggregates,
        CancellationToken cancellationToken);

    Task PersistDeleteAsync(
        IServiceProvider provider,
        object aggregate,
        CancellationToken cancellationToken);

    Task PublishEventsAsync(
        IDomainEventPublisher publisher,
        object aggregate,
        CancellationToken cancellationToken);
}