using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Strategies;

internal sealed class BaseTypeStrategy : IRepositoryRegistrationStrategy
{
    public void Register(IServiceCollection services, Type concreteType, Type baseType) =>
        services.AddScoped(baseType, sp => sp.GetRequiredService(concreteType));
}