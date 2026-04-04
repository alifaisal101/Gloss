using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Strategies;

internal sealed class ConcreteTypeStrategy : IRepositoryRegistrationStrategy
{
    public void Register(IServiceCollection services, Type concreteType, Type baseType) =>
        services.AddScoped(concreteType);
}