using BuildingBlocks.Domain.Models;

namespace BuildingBlocks.Application.Persistence;

public interface IDomainContext
{
    void Save<T, TId>(T aggregate) where T : AggregateRoot<TId> where TId : notnull;
    void Remove<T, TId>(T aggregate) where T : AggregateRoot<TId> where TId : notnull;
    Task CommitAsync(CancellationToken cancellationToken = default);
}