using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Infrastructure.Events.Integration;

internal interface IIntegrationEventWrapper
{
    Task Handle(
        IIntegrationEvent integrationEvent,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}