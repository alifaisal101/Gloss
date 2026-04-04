using BuildingBlocks.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.EfCore;

public static class EfCorePersistenceConfiguration
{
    /// <summary>
    /// <para>Registers a module's DbContext with Postgres and wires up the EfCoreCommitHook.</para>
    /// <para>
    /// This gives you the Cold-to-Hot strategy:
    ///   Write: Handler → DomainContext.Save → EfCoreRepository tracks in DbContext
    ///          → CommitAsync → EfCoreCommitHook calls SaveChanges → data in Postgres
    ///   Read:  CachedRepository decorator (from BuildingBlocks.Infrastructure.Cache)
    ///          → FusionCache [L1 Memory → L2 Redis → Factory(Postgres)]
    /// </para>
    /// <para>
    /// Usage in a module's configuration:
    ///   services.AddModuleDbContext&lt;SubscriptionDbContext&gt;(configuration, "SubscriptionsDb");
    /// </para>
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
        where TDbContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<TDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString(connectionStringName),
                npgsql => npgsql.EnableRetryOnFailure(3)));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.AddScoped<ICommitHook, EfCoreCommitHook<TDbContext>>();
        return services;
    }
}