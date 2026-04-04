namespace BuildingBlocks.Application.EventSourcing;

public sealed record EventQuery
{
    public string? StreamId { get; init; }
    public string? EventType { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public long? FromGlobalPosition { get; init; }
    public int? Limit { get; init; }
}