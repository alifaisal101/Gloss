namespace BuildingBlocks.Application.Cache;

/// <summary>
/// <para>
/// Implement in modules to provide a cache warming strategy.
/// The CacheWarmingService calls all registered sources before peak hours.
/// </para>
/// <para>
/// Example:
///   internal sealed class SubscriptionWarmingSource(ExampleDbContext db, IRepository&lt;Subscription, Guid&gt; repo)
///       : ICacheWarmingSource
///   {
///       public async Task WarmAsync(CancellationToken cancellationToken)
///       {
///           var recentIds = await db.Subscriptions.Where(s => s.ExpiresAtUtc > DateTime.UtcNow)
///               .Select(s => s.Id).Take(10000).ToListAsync(ct);
///           foreach (var id in recentIds)
///               await repo.GetByIdAsync(id, ct); // triggers CachedRepository → warms cache
///       }
///   }
/// </para>
/// </summary>
public interface ICacheWarmingSource
{
    Task WarmAsync(CancellationToken cancellationToken);
}
