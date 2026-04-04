using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.MergeRequests.ListMergeRequests;
using Gloss.Application.MergeRequests.PullMergeRequests;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.MergeRequests;

public static class MergeRequestEndpoints
{
    public static IEndpointRouteBuilder MapMergeRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/repositories/{repositoryId:guid}").WithTags("MergeRequests");

        group.MapGet("/merge-requests", async (
            Guid repositoryId,
            [FromServices] ListMergeRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/pull-reviews", async (
            Guid repositoryId,
            [FromServices] PullMergeRequestsHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        });

        return app;
    }
}
