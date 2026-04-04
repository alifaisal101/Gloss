using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Events.Integration;

internal sealed class InMemoryEventProcessor(
    Channel<IIntegrationEvent> channel,
    IServiceProvider serviceProvider,
    ILogger<InMemoryEventProcessor> logger) : BackgroundService
{
    private static readonly ConcurrentDictionary<Type, IIntegrationEventWrapper> WrapperCache = new();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Background service must survive individual event processing failures.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var integrationEvent in channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await ProcessEvent(integrationEvent, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogProcessingError(ex, integrationEvent.EventId);
            }
        }
    }

    private async Task ProcessEvent(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var eventType = integrationEvent.GetType();

        var wrapper = WrapperCache.GetOrAdd(eventType, type =>
        {
            var wrapperType = typeof(IntegrationEventWrapper<>).MakeGenericType(type);
            return (IIntegrationEventWrapper)Activator.CreateInstance(wrapperType)!;
        });

        var scope = serviceProvider.CreateAsyncScope();
        try
        {
            await wrapper.Handle(integrationEvent, scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }
}