using System.Reflection;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Context;
using BuildingBlocks.Infrastructure.Persistence.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class PersistenceConfiguration
{
    private static readonly IRepositoryRegistrationStrategy[] Strategies =
    [
        new ConcreteTypeStrategy(),
        new BaseTypeStrategy(),
        new SpecificInterfaceStrategy(),
        new GenericInterfaceStrategy(),
    ];

    public static IServiceCollection AddBuildingBlocksPersistence(
        this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        ArgumentNullException.ThrowIfNull(assembliesToScan);

        services.TryAddScoped<IDomainContext, DomainContext>();
        if (assembliesToScan.Length > 0) RegisterRepositories(services, assembliesToScan);
        return services;
    }

    private static void RegisterRepositories(IServiceCollection services, Assembly[] assemblies)
    {
        var repositoryTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t => new { Type = t, Base = GetBaseRepositoryType(t) })
            .Where(x => x.Base is not null);

        foreach (var repo in repositoryTypes)
            foreach (var strategy in Strategies) strategy.Register(services, repo.Type, repo.Base!);
    }

    private static Type? GetBaseRepositoryType(Type type)
    {
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(BaseRepository<,>))
                return baseType;

            baseType = baseType.BaseType;
        }
        return null;
    }
}