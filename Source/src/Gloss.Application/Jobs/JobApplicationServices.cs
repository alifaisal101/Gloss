using Gloss.Application.Jobs.ListJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Application.Jobs;

public static class JobApplicationServices
{
    public static IServiceCollection AddJobApplication(this IServiceCollection services)
    {
        services.AddScoped<ListJobsHandler>();
        return services;
    }
}
