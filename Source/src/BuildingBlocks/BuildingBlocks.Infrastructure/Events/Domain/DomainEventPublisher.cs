using System.Collections.Concurrent;
using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Infrastructure.Events.Domain;

public sealed class DomainEventPublisher(IServiceProvider serviceProvider) : IDomainEventPublisher
{
    private static readonly ConcurrentDictionary<Type, IDomainEventWrapper> WrapperCache = new();

    public async Task Publish(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();

        var wrapper = WrapperCache.GetOrAdd(eventType, type =>
        {
            var wrapperType = typeof(DomainEventWrapper<>).MakeGenericType(type);
            return (IDomainEventWrapper)Activator.CreateInstance(wrapperType)!;
        });

        await wrapper.Handle(domainEvent, serviceProvider, cancellationToken).ConfigureAwait(false);
    }
}