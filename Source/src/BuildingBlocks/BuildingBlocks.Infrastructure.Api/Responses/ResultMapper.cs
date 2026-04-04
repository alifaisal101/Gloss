using System.Diagnostics;
using System.Globalization;
using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Api.Responses;

/// <summary>
/// <para>Maps Result/VoidResult → IResult with standardized ApiResponse envelope.</para>
/// <para>
/// Error code conventions → HTTP status:
///   "*.NotFound"       → 404
///   "*.Conflict"       → 409
///   "*.Unauthorized"   → 401
///   "*.Forbidden"      → 403
///   "*.Validation.*"   → 422
///   everything else    → 400
/// </para>
/// <para>
/// Usage:
///   return result.ToCreated(ctx, id => $"/api/v1/items/{id}");
///   return result.ToNoContent(ctx);
///   return result.ToOk(ctx);
/// </para>
/// </summary>
public static class ResultMapper
{
    public static IResult ToOk<T>(this Result<T> result, HttpContext ctx)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(ctx);
        return result.IsSuccess
            ? Results.Ok(Envelope(200, result.Value, ctx))
            : ToError(result.Error, ctx);
    }

    public static IResult ToOk(this VoidResult result, HttpContext ctx)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(ctx);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse { Status = 200, TraceId = GetTraceId(ctx), RequestId = GetRequestId(ctx) })
            : ToError(result.Error, ctx);
    }

    /// <summary>Wraps any data (e.g. PagedResult) in the standard envelope.</summary>
    public static IResult ToOk<T>(this T data, HttpContext ctx) =>
        Results.Ok(Envelope(200, data, ctx));

    public static IResult ToCreated<T>(this Result<T> result, HttpContext ctx, string location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(ctx);
        return result.IsSuccess
            ? Results.Created(location, Envelope(201, result.Value, ctx))
            : ToError(result.Error, ctx);
    }

    public static IResult ToCreated<T>(this Result<T> result, HttpContext ctx, Func<T, string> locationFactory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(locationFactory);
        return result.IsSuccess
            ? Results.Created(locationFactory(result.Value), Envelope(201, result.Value, ctx))
            : ToError(result.Error, ctx);
    }

    public static IResult ToNoContent(this VoidResult result, HttpContext ctx)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(ctx);
        return result.IsSuccess
            ? Results.NoContent()
            : ToError(result.Error, ctx);
    }

    private static IResult ToError(DomainError error, HttpContext ctx)
    {
        var statusCode = MapErrorToStatusCode(error);
        var localizer = ctx.RequestServices.GetService(typeof(IDomainErrorLocalizer)) as IDomainErrorLocalizer;
        var culture = CultureInfo.CurrentUICulture;

        var message = localizer is not null
            ? localizer.Localize(error, culture)
            : error.FormatMessage(culture);

        var response = new ApiResponse
        {
            Status = statusCode,
            Error = new ApiError { Code = error.Code, Message = message },
            TraceId = GetTraceId(ctx),
            RequestId = GetRequestId(ctx),
        };

        return Results.Json(response, statusCode: statusCode);
    }

    private static int MapErrorToStatusCode(DomainError error) =>
        error.Code switch
        {
            var c when c.Contains("NotFound", StringComparison.OrdinalIgnoreCase) => 404,
            var c when c.Contains("Conflict", StringComparison.OrdinalIgnoreCase) => 409,
            var c when c.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) => 401,
            var c when c.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) => 403,
            var c when c.Contains("Validation", StringComparison.OrdinalIgnoreCase) => 422,
            _ => 400,
        };

    private static ApiResponse<T> Envelope<T>(int status, T data, HttpContext ctx) =>
        new() { Status = status, Data = data, TraceId = GetTraceId(ctx), RequestId = GetRequestId(ctx) };

    private static string GetTraceId(HttpContext ctx) =>
        Activity.Current?.Id ?? ctx.TraceIdentifier;

    private static string GetRequestId(HttpContext ctx) =>
        ctx.Items["RequestId"]?.ToString() ?? "unknown";
}