using Gloss.Application.MergeRequests.AddDraftComment;
using Gloss.Application.MergeRequests.DeleteDraftComment;
using Gloss.Application.MergeRequests.DeleteMergeRequest;
using Gloss.Application.MergeRequests.EditDraftComment;
using Gloss.Application.MergeRequests.GetMergeRequest;
using Gloss.Application.MergeRequests.IgnoreMergeRequest;
using Gloss.Application.MergeRequests.ListAllMergeRequests;
using Gloss.Application.MergeRequests.ListIgnoredMergeRequests;
using Gloss.Application.MergeRequests.UnignoreMergeRequest;
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
        services.AddScoped<AddDraftCommentHandler>();
        services.AddScoped<EditDraftCommentHandler>();
        services.AddScoped<DeleteDraftCommentHandler>();
        services.AddScoped<DeleteMergeRequestHandler>();
        services.AddScoped<IgnoreMergeRequestHandler>();
        services.AddScoped<ListIgnoredMergeRequestsHandler>();
        services.AddScoped<UnignoreMergeRequestHandler>();
        return services;
    }
}
