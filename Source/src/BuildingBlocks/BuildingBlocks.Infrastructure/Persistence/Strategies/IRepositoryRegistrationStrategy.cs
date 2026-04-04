using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Strategies;

internal interface IRepositoryRegistrationStrategy
{
    void Register(IServiceCollection services, Type concreteType, Type baseType);
}