using BuildingBlocks.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.EfCore;

/// <summary>
/// <para>Flushes DbContext.SaveChangesAsync as part of the DomainContext commit pipeline.</para>
/// <para>
/// The flow:
///   1. DomainContext calls EfCoreRepository.Add/Update/Remove → EF change tracker
///   2. DomainContext calls ICommitHook.FlushAsync → THIS calls SaveChangesAsync
///   3. DomainContext publishes domain events (data is now durable in Postgres)
/// </para>
/// <para>
/// Register one per module DbContext:
///   services.AddScoped&lt;ICommitHook, EfCoreCommitHook&lt;SubscriptionDbContext&gt;&gt;();
///   (or use AddModuleDbContext which does this automatically)
/// </para>
/// </summary>
internal sealed class EfCoreCommitHook<TDbContext>(TDbContext dbContext) : ICommitHook
    where TDbContext : DbContext
{
    public int Priority => 0;
    public async Task FlushAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}