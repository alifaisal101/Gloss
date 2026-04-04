using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Api.Middleware;

/// <summary>
/// Enriches every request with two tracking IDs:
///
///   X-Correlation-Id  — end-to-end trace (OpenTelemetry Activity.Id or propagated from caller).
///                        Use this to trace across services.
///
///   X-Request-Id      — unique per-request. Short, human-readable.
///                        Use this to find a specific request in logs.
///
/// Both are pushed into the logging scope so every log line within a request
/// includes them automatically in structured output:
///
///   { "CorrelationId": "00-abc123...", "RequestId": "r-7f3a2b1c", ... }
///
/// Workflow for debugging:
///   1. User reports error → gets traceId from ApiResponse
///   2. Search logs by traceId → find the exact request
///   3. CorrelationId traces across service boundaries
/// </summary>
internal sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string RequestIdHeader = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString("N");

        var requestId = $"r-{Guid.NewGuid().ToString("N")[..8]}";

        context.Items["CorrelationId"] = correlationId;
        context.Items["RequestId"] = requestId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationHeader] = correlationId;
            context.Response.Headers[RequestIdHeader] = requestId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>(StringComparer.Ordinal)
               {
                   ["CorrelationId"] = correlationId,
                   ["RequestId"] = requestId,
               }))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
