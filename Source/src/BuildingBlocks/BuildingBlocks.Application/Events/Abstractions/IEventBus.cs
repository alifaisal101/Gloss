using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Application.Events.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IIntegrationEvent;
}