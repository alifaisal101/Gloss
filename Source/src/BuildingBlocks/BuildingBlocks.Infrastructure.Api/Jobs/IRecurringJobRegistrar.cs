namespace BuildingBlocks.Infrastructure.Api.Jobs;

/// <summary>
/// Implement in each module to register recurring Hangfire jobs.
///
/// Example:
///   internal sealed class CleanupJobRegistrar : IRecurringJobRegistrar
///   {
///       public void Register() =>
///           RecurringJob.AddOrUpdate&lt;CleanupService&gt;("cleanup-expired",
///               s => s.ExecuteAsync(CancellationToken.None), Cron.Hourly, new RecurringJobOptions
///               {
///                   TimeZone = TimeZoneInfo.Utc,
///                   MisfireHandling = MisfireHandlingMode.Ignorable
///               });
///   }
///
/// Register in module: services.AddScoped&lt;IRecurringJobRegistrar, CleanupJobRegistrar&gt;();
/// </summary>
public interface IRecurringJobRegistrar
{
    void Register();
}
