using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Events.Integration;

internal sealed class IntegrationEventWrapper<TIntegrationEvent> : IIntegrationEventWrapper
    where TIntegrationEvent : IIntegrationEvent
{
    public async Task Handle(
        IIntegrationEvent integrationEvent,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>();
        foreach (var handler in handlers)
            await handler.Handle((TIntegrationEvent)integrationEvent, cancellationToken).ConfigureAwait(false);
    }
}