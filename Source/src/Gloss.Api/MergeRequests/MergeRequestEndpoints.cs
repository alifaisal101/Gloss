using BuildingBlocks.Infrastructure.Api.Responses;
using Gloss.Application.MergeRequests.GetMergeRequest;
using Gloss.Application.MergeRequests.ListAllMergeRequests;
using Gloss.Application.MergeRequests.ListMergeRequests;
using Gloss.Application.MergeRequests.PullMergeRequests;
using Gloss.Application.Reviews.ReviewMergeRequest;
using Microsoft.AspNetCore.Mvc;

namespace Gloss.Api.MergeRequests;

public static class MergeRequestEndpoints
{
    public static IEndpointRouteBuilder MapMergeRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/merge-requests", async (
            [FromServices] ListAllMergeRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).WithTags("MergeRequests");

        app.MapGet("/api/merge-requests/{mrId:guid}", async (
            Guid mrId,
            [FromServices] GetMergeRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("MergeRequests");

        app.MapPost("/api/merge-requests/{mrId:guid}/review", async (
            Guid mrId,
            [FromServices] ReviewMergeRequestHandler handler,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(mrId, cancellationToken).ConfigureAwait(false);
            return result.ToOk(ctx);
        }).WithTags("MergeRequests");

        var repoGroup = app.MapGroup("/api/repositories/{repositoryId:guid}").WithTags("MergeRequests");

        repoGroup.MapGet("/merge-requests", async (
            Guid repositoryId,
            [FromServices] ListMergeRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(repositoryId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        repoGroup.MapPost("/pull-reviews", async (
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
