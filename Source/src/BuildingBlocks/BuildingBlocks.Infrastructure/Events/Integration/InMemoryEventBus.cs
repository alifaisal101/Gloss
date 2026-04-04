using System.Threading.Channels;
using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Infrastructure.Events.Integration;

internal sealed class InMemoryEventBus(
    Channel<IIntegrationEvent> channel) : IEventBus
{
    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IIntegrationEvent =>
        await channel.Writer.WriteAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
}