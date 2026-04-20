using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.Projection.GetProjection;
using Gloss.Application.Projection.UpdateProjection;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.Projection;

public static class ProjectionEndpoints
{
    public static IEndpointRouteBuilder MapProjectionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projection/update", async (
            [FromServices] UpdateProjectionHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        }).WithTags("Projection");

        app.MapGet("/api/projection", async (
            [FromServices] GetProjectionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("Projection");

        return app;
    }
}
