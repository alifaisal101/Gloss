using System.Text.Json.Serialization;

namespace BuildingBlocks.Infrastructure.Api.Responses;

public sealed class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FieldError>? Errors { get; init; }
}