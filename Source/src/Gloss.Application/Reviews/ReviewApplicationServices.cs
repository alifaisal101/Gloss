using Gloss.Application.Reviews.PublishMergeRequest;
using Gloss.Application.Reviews.ReviewMergeRequest;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Application.Reviews;

public static class ReviewApplicationServices
{
    public static IServiceCollection AddReviewApplication(this IServiceCollection services)
    {
        services.AddScoped<ReviewMergeRequestHandler>();
        services.AddScoped<PublishMergeRequestHandler>();
        return services;
    }
}
