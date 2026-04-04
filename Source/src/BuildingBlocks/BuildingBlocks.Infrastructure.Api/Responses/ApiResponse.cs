using System.Text.Json.Serialization;

namespace BuildingBlocks.Infrastructure.Api.Responses;

/// <summary>
/// <para>Standard API envelope. Every endpoint returns this shape.</para>
/// <para>
///   Success:
///   {
///     "status": 200,
///     "data": {...},
///     "traceId": "00-abc123...",
///     "requestId": "r-7f3a2b1c"
///   }
/// </para>
/// <para>
///   Error:
///   {
///     "status": 400,
///     "error": { "code": "Subscription.NotFound", "message": "الاشتراك غير موجود." },
///     "traceId": "00-abc123...",
///     "requestId": "r-7f3a2b1c"
///   }
/// </para>
/// <para>
/// Debugging workflow:
///   1. Client sees error → copies traceId or requestId
///   2. Search logs/Jaeger by traceId → full distributed trace
///   3. Search logs by requestId → all log lines for that request
/// </para>
/// </summary>
public sealed class ApiResponse<T>
{
    public int Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; init; }

    public string TraceId { get; init; } = default!;
    public string RequestId { get; init; } = default!;
}

public sealed class ApiResponse
{
    public int Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; init; }

    public string TraceId { get; init; } = default!;
    public string RequestId { get; init; } = default!;
}
