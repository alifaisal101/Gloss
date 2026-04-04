using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Models;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.EfCore;

/// <summary>
/// <para>Generic EF Core-backed repository for the Cold-to-Hot strategy.</para>
/// <para>
/// DomainContext calls Add/Update/Remove → this tracks changes in DbContext.
/// Then DomainContext.CommitAsync → SaveChanges persists to Postgres.
/// Combined with CachedRepository decorator → you get the full Cold-to-Hot flow.
/// </para>
/// <para>
/// For modules that need custom queries, extend this class:
///   internal sealed class SubscriptionRepository(SubscriptionDbContext db, IDomainContext ctx)
///       : EfCoreRepository&lt;Subscription, Guid&gt;(db, ctx)
///   {
///       public Task&lt;Subscription?&gt; GetByStripeIdAsync(string stripeId, CancellationToken cancellationToken) =&gt; ...
///   }
/// </para>
/// </summary>
public class EfCoreRepository<T, TId>(DbContext dbContext, IDomainContext context)
    : BaseRepository<T, TId>(context)
    where T : AggregateRoot<TId>
    where TId : notnull
{
    protected DbContext DbContext { get; } = dbContext;

    protected override async Task<T?> FetchByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>()
            .FindAsync([id], cancellationToken)
            .ConfigureAwait(false);
    }

    protected override Task AddAsync(T aggregate, CancellationToken cancellationToken)
    {
        DbContext.Set<T>().Add(aggregate);
        return Task.CompletedTask;
    }

    protected override Task UpdateAsync(T aggregate, CancellationToken cancellationToken)
    {
        DbContext.Set<T>().Update(aggregate);
        return Task.CompletedTask;
    }

    protected override Task RemoveAsync(T aggregate, CancellationToken cancellationToken)
    {
        DbContext.Set<T>().Remove(aggregate);
        return Task.CompletedTask;
    }

    protected override Task AddRangeAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregates);
        DbContext.Set<T>().AddRange(aggregates);
        return Task.CompletedTask;
    }

    protected override Task UpdateRangeAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregates);
        DbContext.Set<T>().UpdateRange(aggregates);
        return Task.CompletedTask;
    }
}
