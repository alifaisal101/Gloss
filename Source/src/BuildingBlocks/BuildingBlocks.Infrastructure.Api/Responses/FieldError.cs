namespace BuildingBlocks.Infrastructure.Api.Responses;

public sealed class FieldError
{
    public required string Field { get; init; }
    public required string Message { get; init; }
}