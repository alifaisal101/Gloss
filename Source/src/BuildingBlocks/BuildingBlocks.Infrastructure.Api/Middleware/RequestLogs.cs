using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Api.Middleware;

internal static partial class RequestLogs
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms")]
    public static partial void LogRequest(ILogger logger, string method, PathString path, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms")]
    public static partial void LogClientError(ILogger logger, string method, PathString path, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error,
        Message = "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms")]
    public static partial void LogServerError(ILogger logger, string method, PathString path, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Critical,
        Message = "HTTP {Method} {Path} UNHANDLED EXCEPTION in {ElapsedMs}ms")]
    public static partial void LogUnhandledException(ILogger logger, Exception ex, string method, PathString path, long elapsedMs);
}