using System.Diagnostics;
using BuildingBlocks.Infrastructure.Api.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Api.Middleware;

internal sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var requestId = httpContext.Items["RequestId"]?.ToString() ?? "unknown";
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        LogUnhandled(exception, exception.GetType().Name, requestId);

        var response = new ApiResponse
        {
            Status = 500,
            Error = new ApiError
            {
                Code = "Server.InternalError",
                Message = "An unexpected error occurred. Please try again later.",
            },
            TraceId = traceId,
        };

        httpContext.Response.StatusCode = 500;
        httpContext.Response.Headers["X-Request-Id"] = requestId;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Critical,
        Message = "Unhandled {ExceptionType} on request {RequestId}")]
    private partial void LogUnhandled(Exception ex, string exceptionType, string requestId);
}
