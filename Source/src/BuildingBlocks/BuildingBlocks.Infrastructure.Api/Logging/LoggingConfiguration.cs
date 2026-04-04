using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Api.Logging;

public static class LoggingConfiguration
{
    /// <summary>
    /// Configures enterprise-grade logging:
    ///
    ///   - Silences noisy EF Core query logs (keeps errors)
    ///   - Silences FusionCache info (keeps warnings/errors)
    ///   - Silences ASP.NET routing noise
    ///   - Keeps all Warning+ from everything
    ///   - Our building blocks log at Information (request timing, cache ops, stream batches)
    ///
    /// Source-generated [LoggerMessage] is enforced across all building blocks
    /// for zero-allocation logging at hot paths.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            // --- Silence the noise (keep errors) ---
            builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            builder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
            builder.AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.Warning);
            builder.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Warning);
            builder.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Warning);

            builder.AddFilter("ZiggyCreatures.Caching.Fusion", LogLevel.Warning);
            builder.AddFilter("ZiggyCreatures.Caching.Fusion.FusionCache", LogLevel.Warning);

            builder.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
            builder.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
            builder.AddFilter("Microsoft.AspNetCore.StaticFiles", LogLevel.Warning);
            builder.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Warning);

            builder.AddFilter("StackExchange.Redis", LogLevel.Warning);
            builder.AddFilter("Npgsql", LogLevel.Warning);

            // --- Our code at Information ---
            builder.AddFilter("BuildingBlocks", LogLevel.Information);
            builder.AddFilter("ExampleModule", LogLevel.Information);
        });

        return services;
    }
}
