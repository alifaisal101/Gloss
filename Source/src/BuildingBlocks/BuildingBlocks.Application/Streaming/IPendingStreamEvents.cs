namespace BuildingBlocks.Application.Streaming;

/// <summary>
/// <para>
/// Scoped service that collects stream events during a unit of work.
/// HotRepository enqueues events here during Add/Update/Remove.
/// StreamCommitHook drains and publishes them during DomainContext.CommitAsync.
/// </para>
/// <para>
/// This ensures events are only published if the full commit succeeds.
/// If any repository operation fails, CommitAsync throws before the hook runs,
/// and no events leak to the stream.
/// </para>
/// </summary>
public interface IPendingStreamEvents
{
    void Enqueue(AggregateChangeEvent evt);
    IReadOnlyList<AggregateChangeEvent> DrainAll();
}