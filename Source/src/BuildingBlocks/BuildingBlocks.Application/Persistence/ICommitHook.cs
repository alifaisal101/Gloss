namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// <para>
/// Hook into the DomainContext commit pipeline.
/// Called after all repository persist calls (Add/Update/Remove tracked),
/// before domain events are published.
/// </para>
/// <para>
/// Use cases:
///   - EfCore: flush DbContext.SaveChangesAsync (Priority 0)
///   - Streaming: publish aggregate change events to Redis Stream (Priority 10)
///   - Cache: invalidate stale entries (Priority 20)
/// </para>
/// <para>Hooks are executed in ascending Priority order.</para>
/// </summary>
public interface ICommitHook
{
    int Priority { get; }
    Task FlushAsync(CancellationToken cancellationToken);
}
