using System.Text.Json;

namespace BuildingBlocks.Application.EventSourcing;

public sealed record StoredEvent
{
    public required Guid Id { get; init; }
    public required string StreamId { get; init; }
    public required string EventType { get; init; }
    public required long Position { get; init; }
    public required long GlobalPosition { get; init; }
    public required JsonDocument Payload { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}
