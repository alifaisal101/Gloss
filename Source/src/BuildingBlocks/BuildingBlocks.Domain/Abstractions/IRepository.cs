using BuildingBlocks.Domain.Models;

namespace BuildingBlocks.Domain.Abstractions;

public interface IRepository<T, in TId> where T : AggregateRoot<TId> where TId : notnull
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
}