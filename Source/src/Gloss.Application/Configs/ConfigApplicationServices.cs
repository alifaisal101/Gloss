using Gloss.Application.Configs.GetConfig;
using Gloss.Application.Configs.SaveConfig;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Application.Configs;

public static class ConfigApplicationServices
{
    public static IServiceCollection AddConfigApplication(this IServiceCollection services)
    {
        services.AddScoped<GetConfigHandler>();
        services.AddScoped<SaveConfigHandler>();
        return services;
    }
}
