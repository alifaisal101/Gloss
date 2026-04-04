using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Strategies;

internal sealed class GenericInterfaceStrategy : IRepositoryRegistrationStrategy
{
    public void Register(IServiceCollection services, Type concreteType, Type baseType)
    {
        var genericInterface = baseType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<,>));

        if (genericInterface is not null)
            services.AddScoped(genericInterface, sp => sp.GetRequiredService(concreteType));
    }
}