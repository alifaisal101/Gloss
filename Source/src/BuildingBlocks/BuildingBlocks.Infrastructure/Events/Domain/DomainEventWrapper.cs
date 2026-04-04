using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Events.Domain;

internal sealed class DomainEventWrapper<TEvent> : IDomainEventWrapper where TEvent : IDomainEvent
{
    public async Task Handle(
        IDomainEvent domainEvent,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers) await handler.Handle((TEvent)domainEvent, cancellationToken).ConfigureAwait(false);
    }
}