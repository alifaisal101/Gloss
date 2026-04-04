namespace BuildingBlocks.Application.Streaming;

/// <summary>
/// Generic change event for any aggregate. Stored in Redis Stream,
/// consumed by the persistence worker for cold-store backup.
/// Contains the full serialized state for upserts and the aggregate ID for deletes.
/// </summary>
public sealed class AggregateChangeEvent
{
    public required string AggregateType { get; init; }
    public required AggregateChangeType ChangeType { get; init; }
    public required string AggregateId { get; init; }
    public required string SerializedData { get; init; }
    public required DateTime OccurredAtUtc { get; init; }

    public string StreamName => $"{AggregateType}:stream";
}
