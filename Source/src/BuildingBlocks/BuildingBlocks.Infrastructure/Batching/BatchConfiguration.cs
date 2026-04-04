using BuildingBlocks.Application.Batching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure.Batching;

public static class BatchConfiguration
{
    public static IServiceCollection AddBatching(this IServiceCollection services)
    {
        services.TryAddScoped<IBatchProcessor, BatchProcessor>();
        return services;
    }
}