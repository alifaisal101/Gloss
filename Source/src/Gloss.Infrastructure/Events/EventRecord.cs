using System.Text.Json;

namespace Gloss.Infrastructure.Events;

public sealed class EventRecord
{
    public Guid Id { get; private set; }
    public string StreamId { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public long Position { get; private set; }
    public long GlobalPosition { get; init; }
    public JsonDocument Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }

    private EventRecord() { }

    public static EventRecord Create(string streamId, string eventType, long position, JsonDocument payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            StreamId = streamId,
            EventType = eventType,
            Position = position,
            Payload = payload,
            OccurredAt = DateTimeOffset.UtcNow,
        };
}
