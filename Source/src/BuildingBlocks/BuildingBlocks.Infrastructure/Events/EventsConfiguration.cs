using System.Reflection;
using System.Threading.Channels;
using BuildingBlocks.Application.Events.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Infrastructure.Events.Domain;
using BuildingBlocks.Infrastructure.Events.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Events;

public static class EventsConfiguration
{
    public static IServiceCollection AddBuildingBlocksEvents(
        this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        ArgumentNullException.ThrowIfNull(assembliesToScan);

        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        if (assembliesToScan.Length > 0)
        {
            RegisterHandlers(services, typeof(IDomainEventHandler<>), assembliesToScan);
        }

        return services;
    }

    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton(Channel.CreateUnbounded<IIntegrationEvent>());
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddHostedService<InMemoryEventProcessor>();

        return services;
    }

    public static IServiceCollection RegisterModuleHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        RegisterHandlers(services, typeof(IIntegrationEventHandler<>), [assembly]);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Type interfaceType, Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
                .Select(i => new { Interface = i, Implementation = t }))
            .ToList();

        foreach (var handler in handlerTypes)
            services.AddScoped(handler.Interface, handler.Implementation);
    }
}