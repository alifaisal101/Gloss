using Gloss.Application.MergeRequests.GetMergeRequest;
using Gloss.Application.MergeRequests.ListAllMergeRequests;
using Gloss.Application.MergeRequests.ListMergeRequests;
using Gloss.Application.MergeRequests.PollAllRepositories;
using Gloss.Application.MergeRequests.PullMergeRequests;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Application.MergeRequests;

public static class MergeRequestApplicationServices
{
    public static IServiceCollection AddMergeRequestApplication(this IServiceCollection services)
    {
        services.AddScoped<PullMergeRequestsHandler>();
        services.AddScoped<ListMergeRequestsHandler>();
        services.AddScoped<ListAllMergeRequestsHandler>();
        services.AddScoped<GetMergeRequestHandler>();
        services.AddScoped<PollAllRepositoriesHandler>();
        return services;
    }
}
