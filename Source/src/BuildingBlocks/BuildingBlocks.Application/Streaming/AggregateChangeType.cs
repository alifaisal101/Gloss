namespace BuildingBlocks.Application.Streaming;

public enum AggregateChangeType
{
    None,
    Created = 1,
    Updated = 2,
    Deleted = 3,
}