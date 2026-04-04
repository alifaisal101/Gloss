using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Api.Versioning;

public static class ApiVersioningConfiguration
{
    /// <summary>
    /// Adds URL-segment API versioning: /api/v1/..., /api/v2/...
    /// Default version: 1.0. Version required in URL.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        return services;
    }
}
