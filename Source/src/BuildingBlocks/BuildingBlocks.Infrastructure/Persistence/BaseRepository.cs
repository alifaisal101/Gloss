using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models;
using BuildingBlocks.Infrastructure.Persistence.Context;

namespace BuildingBlocks.Infrastructure.Persistence;

public abstract class BaseRepository<T, TId>(IDomainContext context) : IRepository<T, TId>
    where T : AggregateRoot<TId>
    where TId : notnull
{
    protected IDomainContext DomainContext { get; } = context;

    protected abstract Task<T?> FetchByIdAsync(TId id, CancellationToken cancellationToken = default);
    protected abstract Task AddAsync(T aggregate, CancellationToken cancellationToken);
    protected abstract Task UpdateAsync(T aggregate, CancellationToken cancellationToken);
    protected abstract Task RemoveAsync(T aggregate, CancellationToken cancellationToken);

    public async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await FetchByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is not null && DomainContext is IDomainContextTracker tracker) tracker.Attach<T, TId>(entity);
        return entity;
    }

    protected virtual async Task AddRangeAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregates);
        foreach (var aggregate in aggregates) await AddAsync(aggregate, cancellationToken).ConfigureAwait(false);
    }

    protected virtual async Task UpdateRangeAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregates);
        foreach (var aggregate in aggregates) await UpdateAsync(aggregate, cancellationToken).ConfigureAwait(false);
    }

    internal async Task AddRangeInternalAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken)
        => await AddRangeAsync(aggregates, cancellationToken).ConfigureAwait(false);

    internal async Task UpdateRangeInternalAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken)
        => await UpdateRangeAsync(aggregates, cancellationToken).ConfigureAwait(false);

    internal async Task AddInternalAsync(T aggregate, CancellationToken cancellationToken)
        => await AddAsync(aggregate, cancellationToken).ConfigureAwait(false);

    internal async Task RemoveInternalAsync(T aggregate, CancellationToken cancellationToken)
        => await RemoveAsync(aggregate, cancellationToken).ConfigureAwait(false);

    internal async Task UpdateInternalAsync(T aggregate, CancellationToken cancellationToken)
        => await UpdateAsync(aggregate, cancellationToken).ConfigureAwait(false);
}