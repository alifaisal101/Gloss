using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Infrastructure.EfCore;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Persistence;
using Gloss.Application.MergeRequests;
using Gloss.Application.Projection;
using Gloss.Application.Repositories;
using Gloss.Application.Reviews;
using Gloss.Domain.Projection;
using Gloss.Infrastructure.Events;
using Gloss.Infrastructure.MergeRequests;
using Gloss.Infrastructure.Projection;
using Gloss.Infrastructure.Repositories;
using Gloss.Infrastructure.Reviews;
using Gloss.Infrastructure.Reviews.Anthropic;
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
        services.AddScoped<IEventStore, GlossEventStore>();
        services.AddScoped<IReviewerProjectionRepository, ReviewerProjectionRepository>();
        services.AddScoped<IProjectionEngine, AnthropicProjectionEngine>();
        services.AddScoped<IRepoManager, RepoManager>();
        services.AddHttpClient<IGitClient, GitLabClient>();
        services.AddHttpClient<IClaudeApiClient, AnthropicApiClient>(client =>
            client.BaseAddress = new Uri(configuration["Anthropic:BaseUrl"]!));
        services.AddSingleton<IReviewFileSystem, RepoFileSystem>();
        services.AddScoped<IReviewProvider, AnthropicReviewProvider>();
        return services;
    }
}
