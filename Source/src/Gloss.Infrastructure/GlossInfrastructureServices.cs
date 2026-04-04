using BuildingBlocks.Infrastructure.EfCore;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Persistence;
using Gloss.Application.MergeRequests;
using Gloss.Infrastructure.MergeRequests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Infrastructure;

public static class GlossInfrastructureServices
{
    public static IServiceCollection AddGlossInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddModuleDbContext<GlossDbContext>(configuration, "GlossDb");
        services.AddBuildingBlocksPersistence(typeof(GlossDbContext).Assembly);
        services.AddBuildingBlocksEvents();
        services.AddHttpClient<IGitClient, GitLabClient>();
        return services;
    }
}
