using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models;
using BuildingBlocks.Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class RepositoryWrapper<T, TId> : IRepositoryWrapper
    where T : AggregateRoot<TId>
    where TId : notnull
{
    public async Task PersistBatchAsync(
        IServiceProvider provider,
        IDomainContextTracker context,
        IEnumerable<object> aggregates,
        CancellationToken cancellationToken)
    {
        var repo = ResolveRepository(provider);
        var typedAggregates = aggregates.Cast<T>().ToList();

        var inserts = new List<T>();
        var updates = new List<T>();

        foreach (var agg in typedAggregates)
        {
            if (context.IsAttached(agg))
            {
                updates.Add(agg);
            }
            else
            {
                inserts.Add(agg);
                context.Attach<T, TId>(agg);
            }
        }

        if (inserts.Count > 0) await repo.AddRangeInternalAsync(inserts, cancellationToken).ConfigureAwait(false);

        if (updates.Count > 0) await repo.UpdateRangeInternalAsync(updates, cancellationToken).ConfigureAwait(false);
    }

    public async Task PersistDeleteAsync(
        IServiceProvider provider,
        object aggregate,
        CancellationToken cancellationToken)
    {
        var repo = ResolveRepository(provider);
        var typedAggregate = (T)aggregate;
        await repo.RemoveInternalAsync(typedAggregate, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishEventsAsync(
        IDomainEventPublisher publisher,
        object aggregate,
        CancellationToken cancellationToken)
    {
        var typedAggregate = (T)aggregate;

        if (typedAggregate.DomainEvents.Count != 0)
        {
            var events = typedAggregate.DomainEvents.ToArray();
            typedAggregate.ClearDomainEvents();
            foreach (var domainEvent in events)
                await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private static BaseRepository<T, TId> ResolveRepository(IServiceProvider provider)
    {
        var repoInterface = provider.GetService<IRepository<T, TId>>() ?? provider.GetService<BaseRepository<T, TId>>();
        if (repoInterface is BaseRepository<T, TId> repo) return repo;
        throw new InvalidOperationException(
            $"The registered repository for {typeof(T).Name} must inherit from BaseRepository to be used with DomainContext.");
    }
}