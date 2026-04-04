using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Api.Middleware;

/// <summary>
/// Logs every HTTP request with method, path, status code, and duration.
/// Uses source-generated logging for zero-allocation at Info level.
/// Logs warnings for 4xx, errors for 5xx.
/// </summary>
internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context).ConfigureAwait(false);
            sw.Stop();

            var statusCode = context.Response.StatusCode;
            switch (statusCode)
            {
                case >= 500:
                    RequestLogs.LogServerError(logger, context.Request.Method, context.Request.Path, statusCode, sw.ElapsedMilliseconds);
                    break;
                case >= 400:
                    RequestLogs.LogClientError(logger, context.Request.Method, context.Request.Path, statusCode, sw.ElapsedMilliseconds);
                    break;
                default:
                    RequestLogs.LogRequest(logger, context.Request.Method, context.Request.Path, statusCode, sw.ElapsedMilliseconds);
                    break;
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            RequestLogs.LogUnhandledException(logger, ex, context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds);
            throw;
        }
    }
}