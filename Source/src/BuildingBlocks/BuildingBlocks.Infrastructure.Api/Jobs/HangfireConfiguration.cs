using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Api.Jobs;

public static class HangfireConfiguration
{
    /// <summary>
    /// <para>Registers Hangfire with PostgreSQL storage.</para>
    /// <para>
    /// Modules register jobs via IRecurringJobRegistrar:
    ///   public class MyJobRegistrar : IRecurringJobRegistrar
    ///   {
    ///       public void Register() =>
    ///           RecurringJob.AddOrUpdate&lt;MyService&gt;("clean-expired",
    ///               s => s.CleanExpiredAsync(CancellationToken.None), Cron.Hourly);
    ///   }
    /// </para>
    /// <para>
    /// Fire-and-forget from handlers:
    ///   BackgroundJob.Enqueue&lt;IEmailService&gt;(s => s.SendAsync(email, CancellationToken.None));
    /// </para>
    /// </summary>
    public static IServiceCollection AddBuildingBlocksHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var config = configuration.GetSection(HangfireConfig.SectionName).Get<HangfireConfig>()
            ?? new HangfireConfig();

        if (!config.Enabled) return services;

        var connectionString = configuration.GetConnectionString(config.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{config.ConnectionStringName}' not found. " +
                "Add it to ConnectionStrings section or disable Hangfire.");

        services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = config.SchemaName,
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(5),
                    InvisibilityTimeout = TimeSpan.FromMinutes(30),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                }));

        services.AddHangfireServer(options =>
        {
            options.Queues = [.. config.Queues];
            if (config.WorkerCount.HasValue)
                options.WorkerCount = config.WorkerCount.Value;
        });

        return services;
    }

    /// <summary>
    /// Maps Hangfire dashboard + discovers and registers all recurring jobs.
    /// Call after app.Build().
    /// </summary>
    public static IApplicationBuilder UseBuildingBlocksHangfire(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        var config = configuration.GetSection(HangfireConfig.SectionName).Get<HangfireConfig>()
            ?? new HangfireConfig();

        if (!config.Enabled) return app;

        if (!string.IsNullOrEmpty(config.DashboardPath))
        {
            app.UseHangfireDashboard(config.DashboardPath, new DashboardOptions
            {
                DashboardTitle = "Background Jobs",
                DisplayStorageConnectionString = false,
                StatsPollingInterval = 5000,
            });
        }

        using var scope = app.ApplicationServices.CreateScope();
        var registrars = scope.ServiceProvider.GetServices<IRecurringJobRegistrar>();
        foreach (var registrar in registrars) registrar.Register();

        return app;
    }
}
