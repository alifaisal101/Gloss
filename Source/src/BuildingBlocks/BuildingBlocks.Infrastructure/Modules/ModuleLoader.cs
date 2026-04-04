using BuildingBlocks.Application.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Modules;

public static class ModuleLoader
{
    public static IServiceCollection AddModule<TModule>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TModule : IModule, new()
    {
        var module = new TModule();
        module.Register(services, configuration);
        Console.WriteLine($"[Module] Registered Services: {typeof(TModule).Name.Replace("Module", "", StringComparison.Ordinal)}");
        return services;
    }

    public static IEndpointRouteBuilder MapModule<TModule>(
        this IEndpointRouteBuilder endpoints)
        where TModule : IModule, new()
    {
        var module = new TModule();
        module.MapEndpoints(endpoints);

        Console.WriteLine($"[Module] Mapped Endpoints: {typeof(TModule).Name.Replace("Module", "", StringComparison.Ordinal)}");
        return endpoints;
    }
}