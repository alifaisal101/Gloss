using BuildingBlocks.Domain.Models.EventSourced;

namespace BuildingBlocks.Application.EventSourcing;

public interface IEventSourcedRepository<TAggregateRoot, in TId>
    where TAggregateRoot : EventSourcedAggregateRoot<TId>, new()
    where TId : notnull
{
    Task<TAggregateRoot?> FindAsync(TId id, CancellationToken cancellationToken = default);
    Task SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default);
}