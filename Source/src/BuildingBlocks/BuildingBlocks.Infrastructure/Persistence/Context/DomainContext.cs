using System.Collections.Concurrent;
using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Context;

internal sealed class DomainContext(
    IServiceProvider serviceProvider,
    IDomainEventPublisher publisher) : IDomainContext, IDomainContextTracker
{
    private readonly HashSet<object> _attached = [];
    private readonly HashSet<object> _toSave = [];
    private readonly HashSet<object> _toDelete = [];

    private static readonly ConcurrentDictionary<Type, IRepositoryWrapper> WrapperCache = new();
    private readonly ICommitHook[] _hooks = [..serviceProvider.GetServices<ICommitHook>().OrderBy(h => h.Priority),];

    public void Save<T, TId>(T aggregate) where T : AggregateRoot<TId> where TId : notnull
    {
        _toDelete.Remove(aggregate);
        _toSave.Add(aggregate);
    }

    public void Remove<T, TId>(T aggregate) where T : AggregateRoot<TId> where TId : notnull
    {
        _toSave.Remove(aggregate);
        _toDelete.Add(aggregate);
        _attached.Remove(aggregate);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        foreach (var aggregate in _toDelete)
        {
            var wrapper = GetWrapper(aggregate.GetType());
            await wrapper.PersistDeleteAsync(serviceProvider, aggregate, cancellationToken).ConfigureAwait(false);
        }

        var groups = _toSave.GroupBy(x => x.GetType());
        foreach (var group in groups)
        {
            var wrapper = GetWrapper(group.Key);
            await wrapper.PersistBatchAsync(serviceProvider, this, group, cancellationToken).ConfigureAwait(false);
        }

        foreach (var hook in _hooks) await hook.FlushAsync(cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in _toDelete)
        {
            var wrapper = GetWrapper(aggregate.GetType());
            await wrapper.PublishEventsAsync(publisher, aggregate, cancellationToken).ConfigureAwait(false);
        }

        foreach (var aggregate in _toSave)
        {
            var wrapper = GetWrapper(aggregate.GetType());
            await wrapper.PublishEventsAsync(publisher, aggregate, cancellationToken).ConfigureAwait(false);
        }

        _toSave.Clear();
        _toDelete.Clear();
        _attached.Clear();
    }

    void IDomainContextTracker.Attach<T, TId>(T aggregate) => _attached.Add(aggregate);

    bool IDomainContextTracker.IsAttached(object aggregate) => _attached.Contains(aggregate);

    private static IRepositoryWrapper GetWrapper(Type aggregateType) =>
        WrapperCache.GetOrAdd(aggregateType, t =>
        {
            var baseType = t.BaseType!;
            while (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(AggregateRoot<>))
                baseType = baseType.BaseType!;

            var idType = baseType.GetGenericArguments()[0];
            var wrapperType = typeof(RepositoryWrapper<,>).MakeGenericType(t, idType);

            return (IRepositoryWrapper)Activator.CreateInstance(wrapperType)!;
        });
}