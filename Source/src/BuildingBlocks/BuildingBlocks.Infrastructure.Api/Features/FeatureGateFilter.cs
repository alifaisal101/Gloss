using System.Security.Claims;
using BuildingBlocks.Application.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Api.Features;

/// <summary>
/// <para>
/// Endpoint filter that gates access behind a feature flag.
/// Returns 404 when the feature is disabled.
/// </para>
/// <para>
///   group.MapGet("/v2/dashboard", handler)
///       .RequireFeature("BetaDashboard");
/// </para>
/// </summary>
public sealed class FeatureGateFilter(string featureName) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var featureService = context.HttpContext.RequestServices
            .GetRequiredService<IFeatureService>();

        var userId = context.HttpContext.User.FindFirst("sub")?.Value
            ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        bool enabled;
        if (!string.IsNullOrEmpty(userId))
            enabled = await featureService.IsEnabledForAsync(featureName, userId, context.HttpContext.RequestAborted).ConfigureAwait(false);
        else
            enabled = await featureService.IsEnabledAsync(featureName, context.HttpContext.RequestAborted).ConfigureAwait(false);

        if (!enabled)
        {
            return Results.Json(new
            {
                status = 404,
                error = new { code = "Feature.NotAvailable", message = "This feature is not available." },
                traceId = context.HttpContext.TraceIdentifier,
            }, statusCode: 404);
        }

        return await next(context).ConfigureAwait(false);
    }
}