using Gloss.Application.Projection.GetProjection;
using Gloss.Application.Projection.UpdateProjection;
using Microsoft.Extensions.DependencyInjection;

namespace Gloss.Application.Projection;

public static class ProjectionApplicationServices
{
    public static IServiceCollection AddProjectionApplication(this IServiceCollection services)
    {
        services.AddScoped<UpdateProjectionHandler>();
        services.AddScoped<GetProjectionHandler>();
        return services;
    }
}
