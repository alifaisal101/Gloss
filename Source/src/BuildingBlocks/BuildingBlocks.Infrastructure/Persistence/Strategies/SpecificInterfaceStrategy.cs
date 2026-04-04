using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Strategies;

internal sealed class SpecificInterfaceStrategy : IRepositoryRegistrationStrategy
{
    public void Register(IServiceCollection services, Type concreteType, Type baseType)
    {
        var interfaceType = concreteType.GetInterfaces()
            .FirstOrDefault(i => i != typeof(IDisposable) &&
                                 i.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRepository<,>)));

        if (interfaceType is not null)
            services.AddScoped(interfaceType, sp => sp.GetRequiredService(concreteType));
    }
}