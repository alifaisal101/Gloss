using Gloss.Application.Repositories.ListRepositories;
using Gloss.Application.Repositories.UpdateRepository;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Application.Repositories;

public static class RepositoryApplicationServices
{
    public static IServiceCollection AddRepositoryApplication(this IServiceCollection services)
    {
        services.AddScoped<ListRepositoriesHandler>();
        services.AddScoped<UpdateRepositoryHandler>();
        return services;
    }
}
