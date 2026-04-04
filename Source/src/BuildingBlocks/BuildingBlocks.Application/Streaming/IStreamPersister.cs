namespace BuildingBlocks.Application.Streaming;

/// <summary>
/// Handles bulk persistence of stream events to the cold store (Postgres).
/// Register one per hot aggregate type. The generic persistence worker
/// resolves the correct persister by AggregateType.
/// </summary>
public interface IStreamPersister
{
    string AggregateType { get; }
    Task PersistBatchAsync(IReadOnlyList<AggregateChangeEvent> events, CancellationToken cancellationToken);
}